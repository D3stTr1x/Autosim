//using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NascarController : MonoBehaviour
{
    [SerializeField] private Transform _centreOfMass;

    [SerializeField] private GameObject _lights;
    [SerializeField] private GameObject _lightsBackMove;
    [SerializeField] private float _reverseBlinkInterval = 0.25f; // Время (в секундах) на один цвет при мигании

    [SerializeField] private Wheel[] _wheels;
    private Rigidbody _rb;

    [SerializeField] private float _motorForce = 3000f;  // Высокая мощность ~900 Нм / 2 колеса
    [SerializeField] private float _engineBrakeForce = 1500f;
    [SerializeField] private float _brakeForce = 4000f;

    [SerializeField] private float _maxSpeedForward = 70f;  // ~250+ км/ч
    [SerializeField] private float _maxSpeedReverse = 10f;

    [SerializeField] private float _downforceMultiplier = 0.002f;  // Коэффициент downforce    
    private float _brakeInput;
    private float _verticalInput;
    private float _horizontalInput;

    // --- ПЕРЕМЕННЫЕ ДЛЯ ТЕЛЕМЕТРИИ ---
    public float currentSteeringAngle { get; private set; }
    public float currentSlipAngle { get; private set; }

    private float _speed;
    [SerializeField] private AnimationCurve _steeringCurve;  // Кривая руля по скорости (max 30° low speed -> min 12° high)

    // NASCAR base: жёсткие пружины, высокий grip
    private JointSpring[] initialSprings;
    private WheelFrictionCurve[] initialForwardFrictions;
    private WheelFrictionCurve[] initialSidewaysFrictions;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.centerOfMass = _centreOfMass ? _centreOfMass.localPosition : new Vector3(0, -0.7f, 0);  // Низкий CoM

        initialSprings = new JointSpring[_wheels.Length];
        initialForwardFrictions = new WheelFrictionCurve[_wheels.Length];
        initialSidewaysFrictions = new WheelFrictionCurve[_wheels.Length];

        for (int i = 0; i < _wheels.Length; i++)
        {
            var wc = _wheels[i].WheelCollider;
            initialSprings[i] = wc.suspensionSpring;
            initialForwardFrictions[i] = wc.forwardFriction;
            initialSidewaysFrictions[i] = wc.sidewaysFriction;
        }
    }

    private void Update()
    {
        CheckInput();
        Move();
        Brake();
        Steer();
    }

    private void FixedUpdate()
    {
        _speed = _rb.linearVelocity.magnitude;
        ApplyDownforce();
        ApplyDynamicGrip();  // Динамические настройки для поворотов/load/downforce

        foreach (Wheel wheel in _wheels)
        {
            wheel.UpdateMeshPosition();
        }
    }

    private void Move()
    {
        float motorTorque = 0f;
        float speedLimit = 0f;
        float currentMaxSpeed = 0f;

        if (_verticalInput > 0)  // Forward
        {
            currentMaxSpeed = _maxSpeedForward;
            speedLimit = Mathf.Clamp01(1f - (_speed / currentMaxSpeed));
            motorTorque = _motorForce * _verticalInput * speedLimit;
        }
        else if (_verticalInput < 0)  // Reverse
        {
            currentMaxSpeed = _maxSpeedReverse;
            speedLimit = Mathf.Clamp01(1f - (_speed / currentMaxSpeed));
            motorTorque = _motorForce * _verticalInput * speedLimit;
        }
        else  // Engine brake
        {
            if (_speed > 0.5f)
            {
                float brakeDirection = -Mathf.Sign(Vector3.Dot(_rb.linearVelocity, transform.forward));
                motorTorque = brakeDirection * _engineBrakeForce;

                // Safety check
                Vector3 torqueDir = transform.forward * motorTorque;
                if (Vector3.Dot(torqueDir.normalized, _rb.linearVelocity.normalized) > 0)
                    motorTorque = 0;
            }
            else
            {
                _rb.linearVelocity = Vector3.zero;
                motorTorque = 0;
            }
        }
        // Apply to rear wheels (RWD)
        foreach (Wheel wheel in _wheels)
        {
            if (!wheel.IsForwardWheels)
            {
                if ((_verticalInput > 0 && _speed >= _maxSpeedForward) || (_verticalInput < 0 && _speed >= _maxSpeedReverse))
                    wheel.WheelCollider.motorTorque = 0;
                else
                    wheel.WheelCollider.motorTorque = motorTorque;
            }
        }

       bool isReversingInput = _verticalInput < 0;

        if (isReversingInput && IsMovingForward)
        {
            _lightsBackMove.gameObject.SetActive(false);
            _lights.gameObject.SetActive(true);
        }
        else if (isReversingInput && !IsMovingForward)
        {
            _lights.gameObject.SetActive(false);
            _lightsBackMove.gameObject.SetActive(true);
        }
        else
        {
            _lights.gameObject.SetActive(false);
            _lightsBackMove.gameObject.SetActive(false);
        }
    }

    public bool IsMovingForward 
    {
        get 
        {
            return Vector3.Dot(transform.forward, _rb.linearVelocity) > 0.5f;
        }
    }

    private void CheckInput()
    {
        _verticalInput = Input.GetAxis("Vertical");
        _horizontalInput = Input.GetAxis("Horizontal");

        float movingDirectional = Vector3.Dot(transform.forward, _rb.linearVelocity.normalized);
        if ((movingDirectional < -0.5f && _verticalInput > 0) || (movingDirectional > 0.5f && _verticalInput < 0))
            _brakeInput = Mathf.Abs(_verticalInput);
        else
            _brakeInput = 0;
    }

    private void Brake()
    {
        float frontBrake = _brakeForce * _brakeInput * 0.7f;
        float rearBrake = _brakeForce * _brakeInput * 0.3f;  // Меньше сзади для стабильности

        foreach (Wheel wheel in _wheels)
        {
            wheel.WheelCollider.brakeTorque = wheel.IsForwardWheels ? frontBrake : rearBrake;
        }
    }

    private void Steer()
    {
        float baseSteeringAngle = _horizontalInput * _steeringCurve.Evaluate(_speed);

        // Countersteer assist (NASCAR-style stability)
        Vector3 velForward = Vector3.ProjectOnPlane(_rb.linearVelocity, Vector3.up);
        float slipAngle = Vector3.SignedAngle(transform.forward, velForward, Vector3.up);
        if (Mathf.Abs(slipAngle) > 5f && Mathf.Abs(slipAngle) < 120f)
            baseSteeringAngle += slipAngle * 0.3f;

        float steeringAngle = Mathf.Clamp(baseSteeringAngle, -30f, 30f);
        // ТЕЛЕМЕТРИЯ
        currentSteeringAngle = steeringAngle;
        currentSlipAngle = slipAngle;
        //
        foreach (Wheel wheel in _wheels)
        {
            if (wheel.IsForwardWheels)
                wheel.WheelCollider.steerAngle = steeringAngle;
        }
    }

    private void ApplyDownforce()
    {
        float downforce = _speed * _speed * _downforceMultiplier; // без * mass — иначе перебор
        _rb.AddForce(-transform.up * downforce * 1000f, ForceMode.Force); // 1000 — подгонка под реальные 3000–5000 lbs на 200 mph
    }

    private void ApplyDynamicGrip()
    {
        float downforceLoad = _speed * _speed * _downforceMultiplier;
        float totalLoadFactor = 1f + downforceLoad * 2f;
        float gripMultiplier = Mathf.Sqrt(totalLoadFactor); // +40-50% на 200+ км/ч

        for (int i = 0; i < _wheels.Length; i++)
        {
            var wc = _wheels[i].WheelCollider;
            WheelFrictionCurve sideways = initialSidewaysFrictions[i];
            JointSpring suspSpring = initialSprings[i];

            if (_wheels[i].IsForwardWheels)
            {
                // Перед: tight, peaky
                sideways.extremumSlip *= gripMultiplier;
                sideways.extremumValue = initialSidewaysFrictions[i].extremumValue;
                sideways.asymptoteSlip *= gripMultiplier;
                sideways.asymptoteValue *= 0.9f; // Падает после пика
                sideways.stiffness *= 1.05f;
            }
            else
            {
                float rearMult = gripMultiplier * 1.05f;
                sideways.extremumSlip *= rearMult;
                sideways.extremumValue *= 1.1f; // Зад чуть лучше пика
                sideways.asymptoteSlip *= rearMult;
                sideways.asymptoteValue *= 1.05f; // Держит slide дольше
                sideways.stiffness *= 1.1f;
            }
            wc.sidewaysFriction = sideways;

            if (_speed > 30f)
            {
                suspSpring.targetPosition = 0.1f;
            }
            wc.suspensionSpring = suspSpring;

            WheelFrictionCurve forward = initialForwardFrictions[i]; // Нужно сохранить initialForward в Start!
            forward.extremumSlip *= gripMultiplier * 0.95f; // Слабее, чем sideways
            forward.asymptoteSlip *= gripMultiplier * 0.95f;
            forward.extremumValue *= 1.05f; // Лёгкий буст под load
            wc.forwardFriction = forward;
        }
    }
}