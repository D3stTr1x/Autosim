using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DriftPoints : MonoBehaviour
{
    [SerializeField] private CarController _carController;
    [SerializeField] private Rigidbody _rb; 

    [SerializeField] private float _minSpeedForDrift = 5f;
    [SerializeField] private float _pointsMultiplier = 1f;

    private float _currentDriftPoints = 0f;
    private float _totalDriftPoints = 0f;
    private float _driftTimer = 0f;
    private bool _wasDrifting = false;
    private bool _isStopped = false;

    [SerializeField] private TextMeshProUGUI _driftPointsText;
    [SerializeField] private TextMeshProUGUI _driftPointsTextLabel;
    [SerializeField] private TextMeshProUGUI _totalPointsText;
    [SerializeField] private RawImage _background;

    public float _record = 0f;
    
    // --- ПАМЯТЬ НАПРАВЛЕНИЯ ТРАССЫ ---
    private Vector3 _currentTrackForward; 

    void Start()
    {
        if (_carController == null) _carController = GetComponent<CarController>();
        if (_rb == null) _rb = GetComponent<Rigidbody>();

        // На старте трасса идет вперед по ходу капота
        _currentTrackForward = transform.forward; 
    }

    // Считываем направление дороги по синим стрелочкам чекпоинтов
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("CheckPoint") || other.CompareTag("Finish"))
        {
            _currentTrackForward = other.transform.forward;
        }
    }

    void Update()
    {
        if (_isStopped) return;
        bool isDriftingFlag = _carController._isDrift;
        float currentSpeed = _carController._speed;

        float slipAngle = GetSlipAngle();
        bool isMovingForward = slipAngle < 85f; // Защита от езды багажником вперед
        
        float trackDirectionDot = Vector3.Dot(_rb.linearVelocity.normalized, _currentTrackForward);
        bool isMovingCorrectWay = trackDirectionDot > -0.3f; // Защита от фарма в обратную сторону

        bool canDrift = currentSpeed > _minSpeedForDrift && isDriftingFlag && isMovingForward && isMovingCorrectWay;

        if (canDrift)
        {
            if (!_wasDrifting) StartDrift();
            UpdateDriftPoints(currentSpeed, slipAngle);
            _wasDrifting = true;
        }
        else
        {
            if (_wasDrifting) EndDrift();
            _wasDrifting = false;
        }

        UpdateUI();
    }

    private float GetSlipAngle()
    {
        if (_rb.linearVelocity.magnitude < 0.5f) return 0f;
        Vector3 velocityDirection = _rb.linearVelocity.normalized;
        Vector3 forwardDirection = transform.forward;
        return Vector3.Angle(forwardDirection, velocityDirection);
    }

    private void StartDrift()
    {
        _driftTimer = 0f;
        _currentDriftPoints = 0f;
    }

    private void UpdateDriftPoints(float speed, float slipAngle)
    {
        _driftTimer += Time.deltaTime;
        float angleFactor = Mathf.Max(slipAngle / 10f, 0.3f); 
        float timeMultiplier = 1f + _driftTimer * 0.05f; 
        float pointsThisFrame = speed * angleFactor * timeMultiplier * _pointsMultiplier * Time.deltaTime;

        _currentDriftPoints += pointsThisFrame;
        SaveRecord(_currentDriftPoints);
    }

    private void EndDrift()
    {
        if (_currentDriftPoints > 0) _totalDriftPoints += _currentDriftPoints;
        _currentDriftPoints = 0f;
        _driftTimer = 0f;
    }

    private void SaveRecord(float points)
    {
        if (_record < points) _record = points;
    }

    private void UpdateUI()
    {
        if (_driftPointsText != null)
        {
            if (_currentDriftPoints > 0)
            {
                _driftPointsText.text = $"{_currentDriftPoints:F0}";
                _driftPointsText.enabled = true;
                _driftPointsTextLabel.enabled = true;
                _background.enabled = true;
            }
            else
            {
                _driftPointsText.enabled = false;
                _driftPointsTextLabel.enabled = false;
                _background.enabled = false;
            }
        }

        if (_totalPointsText != null) _totalPointsText.text = $"{_totalDriftPoints:F0}";
    }

    public float GetCurrentDriftPoints() => _currentDriftPoints;
    public float GetTotalDriftPoints() => _totalDriftPoints;
    public float GetDriftTime() => _driftTimer;
    public bool IsDrifting() => _wasDrifting;
    public float GetRecord() => _record;
    public void ResetTotalPoints() { _totalDriftPoints = 0; }

    // --- МЕТОД ДЛЯ МЕНЕДЖЕРА ГОНКИ (ПРОВЕРКА WRONG WAY) ---
    public bool IsDrivingWrongWay()
    {
        // Если скорость ниже 3 м/с (игрок врезался и сдает задом), мы не штрафуем
        if (_rb.linearVelocity.magnitude < 3f) return false; 
        
        float trackDirectionDot = Vector3.Dot(_rb.linearVelocity.normalized, _currentTrackForward);
        
        // Если вектор скорости сильно противоположен вектору трассы
        return trackDirectionDot < -0.5f; 
    }

    public void FinalizeDrift()
{
    _isStopped = true; // Блокируем Update навсегда для этого заезда
    
    if (_wasDrifting)
    {
        EndDrift(); // Принудительно завершаем текущий занос и прибавляем очки к общему счету
        _wasDrifting = false;
    }
}
}