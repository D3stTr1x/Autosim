using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class DriftRaceManager : MonoBehaviour
{
    [Header("UI Кнопки")]
    [SerializeField] private GameObject restartButton;
    [Header("Связи со скриптами")]
    [SerializeField] private DriftPoints driftPointsScript;
    [SerializeField] private TelemetryManager telemetryManager;

    [Header("Настройки трассы")]
    public string trackName = "Drift Track";
    [SerializeField] private int requiredCheckpoints = 26;
    private int currentCheckpoints = 0;
    
    private List<Collider> passedCheckpoints = new List<Collider>();

    private bool isRaceStarted = false;
    private bool isRaceFinished = false;

    // --- НОВОЕ: Таймер езды не в ту сторону ---
    private float wrongWayTimer = 0f;

    [Header("UI Интерфейс")]
    [SerializeField] private TextMeshProUGUI levelCompleteText;
    [SerializeField] private TextMeshProUGUI resultScoreText;

    private float raceTime = 0f;

    private void Start()
    {
        if (levelCompleteText) levelCompleteText.gameObject.SetActive(false);
        if (resultScoreText) resultScoreText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            OnClickRestart();
        }
        if (isRaceStarted && !isRaceFinished)
        {
            raceTime += Time.deltaTime;

            // --- ЛОГИКА ШТРАФА (WRONG WAY) ---
            if (driftPointsScript != null && driftPointsScript.IsDrivingWrongWay())
            {
                wrongWayTimer += Time.deltaTime;

                if (wrongWayTimer > 3f) // Если едет назад 3 секунды подряд
                {
                    DisqualifyPlayer();
                }
            }
            else
            {
                // Если машина едет нормально, сбрасываем таймер ошибки
                wrongWayTimer = 0f; 
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isRaceFinished) return;

        if (other.CompareTag("StartLine"))
        {
            if (!isRaceStarted)
            {
                isRaceStarted = true;
                raceTime = 0f;
                currentCheckpoints = 0;
                wrongWayTimer = 0f; 
                passedCheckpoints.Clear();
                
                if (telemetryManager != null) telemetryManager.StartRace("DRIFT", trackName);
                Debug.Log("<color=cyan>Гонка началась! Телеметрия пишет.</color>");
            }
        }

        if (other.CompareTag("CheckPoint"))
        {
            if (!passedCheckpoints.Contains(other))
            {
                passedCheckpoints.Add(other);
                currentCheckpoints++;
                Debug.Log($"Чекпоинт пройден! ({currentCheckpoints}/{requiredCheckpoints})");
            }
        }
        
        if (other.CompareTag("Finish"))
        {
            if (currentCheckpoints >= requiredCheckpoints)
            {
                FinishDriftRace();
            }
            else
            {
                Debug.LogWarning($"Вы пропустили чекпоинты! Собрано {currentCheckpoints} из {requiredCheckpoints}");
            }
        }
    }

    private void FinishDriftRace()
    {
        isRaceFinished = true;
        
        // 1. ПЕРВЫМ ДЕЛОМ останавливаем начисление и забираем очки из последнего заноса
        if (driftPointsScript != null)
        {
            driftPointsScript.FinalizeDrift();
        }

        // 2. ТЕПЕРЬ получаем итоговые очки (они уже будут включать последний занос)
        float finalScore = driftPointsScript.GetTotalDriftPoints();

        // 3. Показываем UI и отправляем в базу
        if (levelCompleteText) 
        {
            levelCompleteText.text = "ЗАЕЗД ЗАВЕРШЕН!";
            levelCompleteText.gameObject.SetActive(true);
        }
        
        if (resultScoreText) 
        {
            resultScoreText.text = $"Твой результат: {finalScore:F0} очков\nВремя: {FormatTime(raceTime)}";
            resultScoreText.gameObject.SetActive(true);
        }

        if (telemetryManager != null)
        {
            telemetryManager.FinishRace(raceTime, finalScore);
        }

        StartCoroutine(DisableTextsWithDelay());

        if (restartButton) restartButton.SetActive(true);


        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // --- МЕТОД ДИСКВАЛИФИКАЦИИ ---
    private void DisqualifyPlayer()
    {
        isRaceFinished = true;
        
        if (levelCompleteText) 
        {
            levelCompleteText.text = "<color=red>ДИСКВАЛИФИКАЦИЯ!\nЕзда в обратном направлении</color>";
            levelCompleteText.color = Color.red;
            levelCompleteText.gameObject.SetActive(true);
        }
        
        if (resultScoreText) 
        {
            resultScoreText.text = "<color=red>Заезд отменен</color>";
            resultScoreText.color = Color.red;
            resultScoreText.gameObject.SetActive(true);
        }
        
        // ВАЖНО: При дисквалификации мы НЕ вызываем telemetryManager.FinishRace(), 
        // чтобы эта испорченная попытка не сохранялась в базу PostgreSQL!

        Debug.Log("<color=red>Игрок дисквалифицирован за движение в обратную сторону.</color>");
        StartCoroutine(DisableTextsWithDelay());
    }

    private string FormatTime(float time)
    {
        System.TimeSpan t = System.TimeSpan.FromSeconds(time);
        return string.Format("{0:00}:{1:00}:{2:00}", t.Minutes, t.Seconds, t.Milliseconds / 10);
    }

    private IEnumerator DisableTextsWithDelay()
    {
        yield return new WaitForSeconds(5f); 
        if (levelCompleteText) levelCompleteText.gameObject.SetActive(false);
        if (resultScoreText) resultScoreText.gameObject.SetActive(false);
    }
    
    public void OnClickRestart()
    {
        // Перезагружает текущую активную сцену
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}