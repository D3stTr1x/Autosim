using UnityEngine;

[RequireComponent(typeof(Rigidbody))]

public class CarController : MonoBehaviour
{
    [SerializeField] private Transform _centreOfMass;
    [SerializeField] private Wheel[] _wheels;
    [SerializeField] private GameObject _lights;
    [SerializeField] private GameObject _lightsBackMove;

    [SerializeField] private GameObject _driftSmoke;

    // ПЕРЕМЕННЫЕ ДЛЯ ТЕЛЕМЕТРИИ 
    public float currentSteeringAngle { get; private set; }
    public float currentSlipAngle { get; private set; }

    private Rigidbody _rb;
    [SerializeField] private int _motorForce;
    [SerializeField] private int _brakeForce;
    [SerializeField] private float _engineBrakeForce;
    [SerializeField] private float _maxSpeedForvard;
    [SerializeField] private float _maxSpeedRevers;

    [SerializeField]  private float _brakeInput;
    private float _verticalInput;
    private float _horizontalInput;

    [SerializeField] public bool _isDrift = false;
    [SerializeField] public float _speed;

    [SerializeField] private AnimationCurve _sterlingCurve;

    private float smallAngleTimer = 0f;
    private const float REQUIRED_SMALL_ANGLE_TIME = 2f;

    private JointSpring[] initialSprings;
    private WheelFrictionCurve[] initialForwardFrictions;
    private WheelFrictionCurve[] initialSidewaysFrictions;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.centerOfMass = _centreOfMass.position;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        initialSprings = new JointSpring[_wheels.Length];
        initialForwardFrictions = new WheelFrictionCurve[_wheels.Length];
        initialSidewaysFrictions = new WheelFrictionCurve[_wheels.Length];

        for (int i = 0; i < _wheels.Length; i++)
        {
            initialSprings[i] = _wheels[i].WheelCollider.suspensionSpring;
            initialForwardFrictions[i] = _wheels[i].WheelCollider.forwardFriction;
            initialSidewaysFrictions[i] = _wheels[i].WheelCollider.sidewaysFriction;
        }
    }

    private void Update()
    {
        Move();
        Brake();
        Steerling();
        CheckInput();
    }

    private void FixedUpdate()
    {
        foreach (Wheel wheel in _wheels)
        {
            if (wheel.IsForwardWheels)
            {
                float slipAngel = Vector3.Angle(transform.forward, _rb.linearVelocity - transform.forward);

                if (_isDrift == false && ((_speed > 8f && slipAngel > 20) || (_speed > 5f && Input.GetKey(KeyCode.Space))))
                {
                    _isDrift = true;
                    _driftSmoke.SetActive(true);
                }
                if (_isDrift == true && (_speed < 5f))
                {
                    _isDrift = false;
                    _driftSmoke.SetActive(false);
                }
                CheckAngelTime();
            }
        }
       
    }

    private void CheckAngelTime()
    {
        foreach (Wheel wheel in _wheels)
        {
            if (wheel.IsForwardWheels)
            {
                float slipAngel = Vector3.Angle(transform.forward, _rb.linearVelocity - transform.forward);

                if (_isDrift == true)
                {
                    if (Mathf.Abs(slipAngel) < 5f)
                    {
                        smallAngleTimer += Time.deltaTime; 

                        if (smallAngleTimer >= REQUIRED_SMALL_ANGLE_TIME)
                        {
                            _isDrift = false;
                            _driftSmoke.SetActive(false);
                        }
                    }
                    else
                    {
                        smallAngleTimer = 0f;
                    }
                }
            }
        }
    }

    private void Move()
    {
        _speed = _rb.linearVelocity.magnitude;

        foreach (Wheel wheel in _wheels)
        {
            if (!wheel.IsForwardWheels) 
            {
                float motorTorque = 0f;
                float speedLimit = 0f;
                float currentMaxSpeed = 0f;

                if (_verticalInput > 0)
                {
                    currentMaxSpeed = _maxSpeedForvard;
                    speedLimit = Mathf.Clamp01(1f - (_speed / currentMaxSpeed));
                    motorTorque = _motorForce * _verticalInput * speedLimit;
                }
                else if (_verticalInput < 0)
                {
                    currentMaxSpeed = _maxSpeedRevers;
                    speedLimit = Mathf.Clamp01(1f - (_speed / currentMaxSpeed));
                    motorTorque = _motorForce * _verticalInput * speedLimit;
                }
                else 
                {
                    if(_speed > 0.5f)
                    {
                        float brakeDirection = -Mathf.Sign(Vector3.Dot(_rb.linearVelocity, transform.forward));
                        motorTorque = brakeDirection * _engineBrakeForce;

                        float currentSpeed = _rb.linearVelocity.magnitude;
                        Vector3 currentDirection = _rb.linearVelocity.normalized;
                        Vector3 torqueDirection = transform.forward * motorTorque;

                        if (Vector3.Dot(torqueDirection, currentDirection) > 0)
                        {
                            motorTorque = 0;
                        }
                    }
                    else
                    {
                        _rb.linearVelocity = Vector3.zero;
                        motorTorque = 0;
                    }
                }

                if ((_verticalInput > 0 && _speed >= currentMaxSpeed) || (_verticalInput < 0 && _speed >= currentMaxSpeed))
                {
                    motorTorque = 0;
                }
                wheel.WheelCollider.motorTorque = motorTorque;
            }
             
            wheel.UpdateMeshPosition();
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

        float movingDirectional = Vector3.Dot(transform.forward, _rb.linearVelocity);

        if ((movingDirectional < -0.5f && _verticalInput > 0) || (movingDirectional > 0.5f && _verticalInput < 0))   
            _brakeInput = Mathf.Abs(_verticalInput);
        else  
            _brakeInput = 0;

    }

    private void Brake() 
    {
        foreach (Wheel wheel in _wheels)
        {
            if (wheel.IsForwardWheels)
            {
                wheel.WheelCollider.brakeTorque = _brakeForce * _brakeInput * 0.7f;
            }
            else
            {
                wheel.WheelCollider.brakeTorque = _brakeForce * _brakeInput * 0.3f;
            }
        }
            
    }

    private void Steerling()
    {
        float steeringAngle = _horizontalInput * _sterlingCurve.Evaluate(_speed);
        float slipAngel = Vector3.Angle(transform.forward, _rb.linearVelocity - transform.forward);

        if (slipAngel < 120 && slipAngel > 10)
            steeringAngle += Vector3.SignedAngle(transform.forward, _rb.linearVelocity, Vector3.up);

        steeringAngle = Mathf.Clamp(steeringAngle, -48, 48);
        // Телеметрия
        currentSteeringAngle = steeringAngle;
        currentSlipAngle = slipAngel;
        //
        foreach (Wheel wheel in _wheels)
        {
            if (wheel.IsForwardWheels)
            {
                wheel.WheelCollider.steerAngle = steeringAngle;
            }
        }

        ApplyDriftSettings();
    }


    private void ApplyDriftSettings()
    {
        for (int i = 0; i < _wheels.Length; i++)
        {
            Wheel wheel = _wheels[i];
            

            if ((_isDrift == false) && (_speed > 5f) && (!wheel.IsForwardWheels))
            { 
                WheelFrictionCurve forwardFrictionSettings = wheel.WheelCollider.forwardFriction;

                forwardFrictionSettings.extremumSlip = 0.6f;
                forwardFrictionSettings.extremumValue = 1f;
                forwardFrictionSettings.asymptoteSlip = 0.8f;
                forwardFrictionSettings.asymptoteValue = 0.5f;
                forwardFrictionSettings.stiffness = 1f;

                wheel.WheelCollider.forwardFriction = forwardFrictionSettings;
            }
            else if (_isDrift==true)
            {
                JointSpring spring = wheel.WheelCollider.suspensionSpring;
                WheelFrictionCurve forward = wheel.WheelCollider.forwardFriction;
                WheelFrictionCurve sideways = wheel.WheelCollider.sidewaysFriction;

                spring.spring = 45000f;
                spring.damper = 6000f;
                spring.targetPosition = 0.5f;
                wheel.WheelCollider.suspensionSpring = spring;

                if (wheel.IsForwardWheels)
                {
                    forward.extremumSlip = 0.4f;
                    forward.extremumValue = 1.0f;
                    forward.asymptoteSlip = 0.8f;
                    forward.asymptoteValue = 0.7f;
                    forward.stiffness = 1.8f;         

                    sideways.extremumSlip = 0.5f;
                    sideways.extremumValue = 0.5f;
                    sideways.asymptoteSlip = 0.5f;
                    sideways.asymptoteValue = 0.75f;
                    sideways.stiffness = 1.6f;
                }
                else
                {
                    forward.extremumSlip = 0.35f;
                    forward.extremumValue = 1.0f;
                    forward.asymptoteSlip = 0.7f;
                    forward.asymptoteValue = 0.75f;
                    forward.stiffness = 1.7f;

                    sideways.extremumSlip = 0.2f;
                    sideways.extremumValue = 0.3f;
                    sideways.asymptoteSlip = 0.45f;
                    sideways.asymptoteValue = 0.6f;
                    sideways.stiffness = 1.4f;

                    if (Input.GetKey(KeyCode.Space))
                    {
                        sideways.extremumSlip = 0.1f;
                        sideways.extremumValue = 0.1f;
                    }
                }

                wheel.WheelCollider.forwardFriction = forward;
                wheel.WheelCollider.sidewaysFriction = sideways;
            }
            else
            {
                wheel.WheelCollider.suspensionSpring = initialSprings[i];
                wheel.WheelCollider.forwardFriction = initialForwardFrictions[i];
                wheel.WheelCollider.sidewaysFriction = initialSidewaysFrictions[i];
            }
        }
    }
}

[System.Serializable]
public struct Wheel
{
    public Transform WheelMesh;
    public WheelCollider WheelCollider;
    public bool IsForwardWheels;

    public void UpdateMeshPosition()
    {
        Vector3 position;
        Quaternion rotation;

        WheelCollider.GetWorldPose(out position, out rotation);
        
        WheelMesh.position = position;
        WheelMesh.rotation = rotation;
    }
}