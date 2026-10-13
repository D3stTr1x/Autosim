using UnityEngine;

public class Drifting : MonoBehaviour
{
    // Используем новое имя структуры CarWheel
    [SerializeField] private CarWheel[] _wheels; 

    private JointSpring[] initialSprings;
    private WheelFrictionCurve[] initialForwardFrictions;
    private WheelFrictionCurve[] initialSidewaysFrictions;

    void Start()
    {
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

    // Update is called once per frame
    void Update()
    {
        ApplyDriftSettings(); 
        foreach (CarWheel wheel in _wheels)
        {
            wheel.UpdateMeshPosition();
        }
    }

    // Переименованный метод
    public void ApplyDriftSettings() 
    {
        for (int i = 0; i < _wheels.Length; i++)
        {
            CarWheel wheel = _wheels[i];

            if (Input.GetKey(KeyCode.Space))
            {
                JointSpring springSettings = wheel.WheelCollider.suspensionSpring;
                WheelFrictionCurve forwardFrictionSettings = wheel.WheelCollider.forwardFriction;
                WheelFrictionCurve sidewaysFrictionSettings = wheel.WheelCollider.sidewaysFriction;

                springSettings.spring = 35000f;
                springSettings.damper = 4500f;
                springSettings.targetPosition = 0.5f;

                forwardFrictionSettings.extremumSlip = 0.4f;
                forwardFrictionSettings.extremumValue = 1f;
                forwardFrictionSettings.asymptoteSlip = 0.8f;
                forwardFrictionSettings.asymptoteValue = 0.5f;
                forwardFrictionSettings.stiffness = 1f;
                if (!wheel.IsForwardWheels)
                {
                    

                    sidewaysFrictionSettings.extremumSlip = 0.1f;
                    sidewaysFrictionSettings.extremumValue = 0.45f;
                    sidewaysFrictionSettings.asymptoteSlip = 0.25f;
                    sidewaysFrictionSettings.asymptoteValue = 0.75f;                }
                else
                {
                    sidewaysFrictionSettings.extremumSlip = 0.2f;
                    sidewaysFrictionSettings.extremumValue = 1f;
                    sidewaysFrictionSettings.asymptoteSlip = 0.5f;
                    sidewaysFrictionSettings.asymptoteValue = 0.75f;
                    
                    
                    
                }
                sidewaysFrictionSettings.stiffness = 1f;
                wheel.WheelCollider.suspensionSpring = springSettings;
                wheel.WheelCollider.forwardFriction = forwardFrictionSettings;
                wheel.WheelCollider.sidewaysFriction = sidewaysFrictionSettings;
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
public struct CarWheel // Структура переименована
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