using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using TMPro;

public class BestLap : MonoBehaviour
{
    [Header("Связи со скриптами")]
    [SerializeField] private TelemetryManager telemetryManager; // Ссылка на БД
    public string trackName = "Nascar Oval 1"; // Название трассы для БД

    [Header("Настройки машины и трассы")]
    [SerializeField] private Rigidbody carRigidbody;
    [SerializeField] private int requiredCheckpoints = 3; // Сколько чекпоинтов нужно для круга

    private int _countCheckPoint = 0;

    [Header("UI Интерфейс")]
    [SerializeField] private TextMeshProUGUI nowTimeLap;
    [SerializeField] private TextMeshProUGUI bestTimeLap;
    [SerializeField] private TextMeshProUGUI levelCompleteText;
    [SerializeField] private TextMeshProUGUI countdownText;
    
    private float currentLapTime = 0f;
    private float bestLapTime = float.MaxValue;
    private bool isTiming = false;

    private void Start()
    {
        bestTimeLap.text = "--:--:--";
        nowTimeLap.text = "00:00:00";
        if (countdownText) countdownText.gameObject.SetActive(false);
        if (levelCompleteText) levelCompleteText.gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (!isTiming) return; 
        
        currentLapTime += Time.deltaTime;
        nowTimeLap.text = FormatTime(currentLapTime);

        if (Input.GetButtonDown("ResetLap"))
        {
            ResetBestLap(); 
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // В кольцевых гонках линия старта и финиша — это обычно один и тот же триггер
        if (other.CompareTag("Finish") || other.CompareTag("StartLine"))
        {
            FinishLap();
        }

        if (other.CompareTag("CheckPoint"))
        {
            // Здесь можно добавить защиту от накрутки чекпоинтов, если нужно
            _countCheckPoint++;
        }
    }

    private void FinishLap()
    {
        // 1. ЕСЛИ ЭТО САМЫЙ ПЕРВЫЙ РАЗ, КОГДА МЫ ПЕРЕСЕКЛИ СТАРТ
        if (!isTiming)
        {
            isTiming = true;
            currentLapTime = 0f;
            _countCheckPoint = 0;
            
            // Запускаем сбор телеметрии для первого круга
            if (telemetryManager != null) telemetryManager.StartRace("NASCAR", trackName);
            Debug.Log("<color=cyan>Гонка NASCAR началась! Телеметрия пишет.</color>");
            return;
        }

        // 2. ЕСЛИ КРУГ ЗАВЕРШЕН И ВСЕ ЧЕКПОИНТЫ СОБРАНЫ
        if (_countCheckPoint >= requiredCheckpoints) 
        {
            // ОТПРАВЛЯЕМ ДАННЫЕ В БАЗУ! (Очки дрифта = 0)
            if (telemetryManager != null)
            {
                // Завершаем текущий круг и сохраняем его
                telemetryManager.FinishRace(currentLapTime, 0f);
                
                // Мгновенно запускаем телеметрию заново для следующего круга!
                telemetryManager.StartRace("NASCAR", trackName);
            }

            // Проверяем, побили ли мы рекорд
            if (currentLapTime < bestLapTime)
            {
                bestLapTime = currentLapTime;
                bestTimeLap.text = FormatTime(bestLapTime);
                bestTimeLap.color = Color.yellow;

                countdownText.text = FormatTime(bestLapTime);
                levelCompleteText.text = "Новый Рекорд!";
                levelCompleteText.gameObject.SetActive(true);
                countdownText.gameObject.SetActive(true);
                
                // Запускаем корутину ОДИН раз (исправлен баг)
                StartCoroutine(DisableTextsWithDelay());
            }
        }
        else
        {
            Debug.LogWarning($"Круг не засчитан! Собрано {_countCheckPoint} из {requiredCheckpoints}");
        }

        // 3. ОБНУЛЯЕМ ТАЙМЕРЫ ДЛЯ СЛЕДУЮЩЕГО КРУГА
        nowTimeLap.text = "00:00:00"; 
        _countCheckPoint = 0;
        currentLapTime = 0f; 
    }

    private string FormatTime(float time)
    {
        TimeSpan t = TimeSpan.FromSeconds(time);
        return string.Format("{0:00}:{1:00}:{2:00}", t.Minutes, t.Seconds, t.Milliseconds / 10);
    }

    private IEnumerator DisableTextsWithDelay()
    {
        yield return new WaitForSeconds(3f); 
        if (levelCompleteText) levelCompleteText.gameObject.SetActive(false);
        if (countdownText) countdownText.gameObject.SetActive(false);
    }

    [ContextMenu("Reset Best Lap")]
    public void ResetBestLap()
    {
        bestLapTime = float.MaxValue;
        bestTimeLap.text = "--:--:--";
        bestTimeLap.color = Color.white;
        isTiming = false;  
        currentLapTime = 0f;
        nowTimeLap.text = "00:00:00";
    }
    
}