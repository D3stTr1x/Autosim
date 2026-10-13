using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class CheckPointCount : MonoBehaviour
{
    [SerializeField] private DriftPoints _driftPoints;
    private float record;
    private bool _canSwitch = false;

    private string scene;

    [SerializeField] private TextMeshProUGUI _needPointsText;
    [SerializeField] private TextMeshProUGUI _totalPointsText;
    [SerializeField] private TextMeshProUGUI _levelCompleteText;
    [SerializeField] private TextMeshProUGUI _countdownText;
    [SerializeField] private TextMeshProUGUI _slesh;



    [SerializeField] private TextMeshProUGUI _labelRec;
    [SerializeField] private TextMeshProUGUI _labelDel;

    [SerializeField] private int _currentNeedPoints = 200;
    private int _currentTotalPoints = 0;
    private bool _isLevelCompleted = false;
    private Coroutine _countdownCoroutine;

    void Start()
    {
        _driftPoints = GetComponent<DriftPoints>();
        _currentTotalPoints = 0;

        UpdateUI();
        // Скрываем тексты завершения уровня
        if (_levelCompleteText != null)
            _levelCompleteText.gameObject.SetActive(false);
        if (_countdownText != null)
            _countdownText.gameObject.SetActive(false);
    }

    void Update()
    {
        // Обновляем текущие очки из системы дрифта
        _currentTotalPoints = Mathf.FloorToInt(_driftPoints.GetTotalDriftPoints());
        record = _driftPoints.GetRecord();
        bool isDrifting = _driftPoints.IsDrifting();
        scene = SceneManager.GetActiveScene().name;

        // Проверяем условия завершения уровня
        if (!isDrifting && !_isLevelCompleted && _currentTotalPoints >= _currentNeedPoints)
        {
            CompleteLevel();
        }

        UpdateUI();
    }

    private void CompleteLevel()
    {
        _isLevelCompleted = true;

        // Показываем сообщение о завершении уровня
        if (_levelCompleteText != null)
        {
            if (scene == "DriftScene1")
            {
                _levelCompleteText.text = "Уровень пройден!";
                _levelCompleteText.gameObject.SetActive(true);
            }
            else if (scene == "DriftScene2")
            {
                _levelCompleteText.text = "Поздравляю с победой!";
                _levelCompleteText.gameObject.SetActive(true);

                _labelRec.text = "Рекорд:";
                _labelDel.gameObject.SetActive(false);
                _totalPointsText.gameObject.SetActive(false);
                _slesh.gameObject.SetActive(false);
                _needPointsText.text = Convert.ToString(record);
                _canSwitch = true;
                _countdownText.text = "Расслабьтесь и получайте удовольствие";
                _countdownText.gameObject.SetActive(true);

                StartCoroutine(DisableText());
            }
            else
            {

            }
        }



        // Запускаем отсчет до следующего уровня
        if (_countdownCoroutine != null)
            if (scene == "DriftScene1")
            {
                StopCoroutine(_countdownCoroutine);
            }
            else if (scene == "DriftScene2")
            {

            }
        if (scene == "DriftScene1") _countdownCoroutine = StartCoroutine(CountdownToNextLevel());
    }

    private IEnumerator DisableText()
    {
        yield return new WaitForSeconds(3f);

        _levelCompleteText.gameObject.SetActive(false);
        _countdownText.gameObject.SetActive(false);
    }
    private IEnumerator CountdownToNextLevel()
    {
        if (_countdownText != null)
            _countdownText.gameObject.SetActive(true);

        // 5-секундный отсчет
        for (int i = 5; i > 0; i--)
        {
            if (_countdownText != null)
                _countdownText.text = $"Следующий уровень через: {i}";

            yield return new WaitForSeconds(1f);
        }

        // Переход к следующему уровню
        NextLevel();
    }

    private void NextLevel()
    {
        // Увеличиваем требуемые очки
        _currentNeedPoints += 5000;

        // Обнуляем текущие очки
        _currentTotalPoints = 0;
        _driftPoints.ResetTotalPoints();

        _isLevelCompleted = false;

        if (_levelCompleteText != null)
            _levelCompleteText.gameObject.SetActive(false);
        if (_countdownText != null)
            _countdownText.gameObject.SetActive(false);

        if (scene == "DriftScene1")
        {
            SceneTransition.SwitchToScene("DriftScene2");
        }
    }
    private void UpdateUI()
    {
        if ((_needPointsText != null) && (Convert.ToDouble(_totalPointsText) < Convert.ToDouble(_needPointsText)))
            _needPointsText.text = $"{_currentNeedPoints}";
        else if ((_canSwitch) && (Convert.ToDouble(_totalPointsText) >= Convert.ToDouble(_needPointsText))) 
            _needPointsText.text = $"{record:F0}";
         

        if (_totalPointsText != null)
            _totalPointsText.text = $"{_currentTotalPoints}";
    }
}