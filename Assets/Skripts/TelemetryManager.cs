using UnityEngine;
using System.Collections.Generic;
using Npgsql;
using System.Text;
using System.Globalization;

// Класс для хранения одного кадра данных телеметрии
[System.Serializable]
public class TelemetryFrame
{
    public float timeOffset;
    public float posX, posY, posZ;
    public float speed;
    public float steeringAngle;
    public float wheelSlip;
}

public class TelemetryManager : MonoBehaviour
{
    [Header("Ссылки на автомобиль")]
    public Rigidbody carRigidbody; 
    public CarController driftController;
    public NascarController nascarController;

    [Header("Настройки БД")]
    private string connString = "Host=localhost;Port=23690;Username=postgres;Password=Rage1945qftr326;Database=RaceGame";

    [Header("Состояние гонки (только для чтения)")]
    [SerializeField] private int currentSessionId = -1;
    [SerializeField] private float raceStartTime;
    [SerializeField] private bool isRecording = false;

    // Список, в котором мы копим данные во время заезда
    private List<TelemetryFrame> sessionData = new List<TelemetryFrame>();

    /// <summary>
    /// Вызывать в момент пересечения стартовой линии
    /// </summary>
    public void StartRace(string mode, string trackName)
    {
        // Проверяем, зашел ли игрок
        if (DatabaseManager.CurrentPlayerId <= 0)
        {
            Debug.LogWarning("Игрок не авторизован! Заезд не будет записан в БД.");
            // Подсказка: для тестов прямо в Unity Editor, когда лень вводить логин, 
            // можно временно раскомментировать строку ниже:
            // DatabaseManager.CurrentPlayerId = 1; 
            return;
        }

        sessionData.Clear();
        
        // 1. Создаем новую сессию в базе и получаем её ID
        using (var conn = new NpgsqlConnection(connString))
        {
            conn.Open();
            string sql = @"INSERT INTO sessions (player_id, game_mode, track_name) 
                           VALUES (@pid, @mode, @track) RETURNING id;";
            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("pid", DatabaseManager.CurrentPlayerId);
                cmd.Parameters.AddWithValue("mode", mode);
                cmd.Parameters.AddWithValue("track", trackName);
                
                currentSessionId = System.Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        Debug.Log("Гонка началась! ID Сессии: " + currentSessionId);
        raceStartTime = Time.time;
        isRecording = true;
        
        // 2. Запускаем сбор данных 10 раз в секунду (каждые 0.1 сек)
        InvokeRepeating(nameof(RecordFrame), 0f, 0.1f);
    }

    /// <summary>
    /// Этот метод автоматически вызывается каждые 0.1 секунды
    /// </summary>
    private void RecordFrame()
    {
        if (!isRecording) return;

        TelemetryFrame frame = new TelemetryFrame();
        frame.timeOffset = Time.time - raceStartTime;
        
        // Считываем координаты
        frame.posX = carRigidbody.position.x;
        frame.posY = carRigidbody.position.y;
        frame.posZ = carRigidbody.position.z;
        
        // Считываем скорость
        frame.speed = carRigidbody.linearVelocity.magnitude;
        
        // Считываем данные из активного контроллера
        float tempSteering = 0f;
        float tempSlip = 0f;

        if (driftController != null && driftController.enabled)
        {
            tempSteering = driftController.currentSteeringAngle; 
            tempSlip = driftController.currentSlipAngle;
        }
        else if (nascarController != null && nascarController.enabled)
        {
            tempSteering = nascarController.currentSteeringAngle;
            tempSlip = nascarController.currentSlipAngle;
        }

        frame.steeringAngle = tempSteering;
        frame.wheelSlip = tempSlip;

        // Добавляем кадр в буфер
        sessionData.Add(frame);
    }

    /// <summary>
    /// Вызывать в момент пересечения финишной черты
    /// </summary>
    public void FinishRace(float finalLapTime, float finalDriftScore)
    {
        if (!isRecording) return;

        isRecording = false;
        CancelInvoke(nameof(RecordFrame)); // Останавливаем запись
        Debug.Log($"Финиш! Собрано кадров: {sessionData.Count}. Отправка в БД...");
        
        using (var conn = new NpgsqlConnection(connString))
        {
            conn.Open();

            // 1. Обновляем время круга и очки дрифта в таблице sessions
            string updateSql = "UPDATE sessions SET lap_time = @time, drift_score = @score WHERE id = @sid";
            using (var cmd = new NpgsqlCommand(updateSql, conn))
            {
                cmd.Parameters.AddWithValue("time", finalLapTime);
                cmd.Parameters.AddWithValue("score", finalDriftScore);
                cmd.Parameters.AddWithValue("sid", currentSessionId);
                cmd.ExecuteNonQuery();
            }

            // 2. Сохраняем все точки пути
            if (sessionData.Count > 0)
            {
                SaveTelemetryBatch(conn);
            }
        }
    }

    /// <summary>
    /// Оптимизированная массовая вставка телеметрии
    /// </summary>
    private void SaveTelemetryBatch(NpgsqlConnection conn)
    {
        // Используем транзакцию. Если что-то пойдет не так, база отменит изменения.
        using (var transaction = conn.BeginTransaction())
        {
            // Формируем один гигантский текстовый SQL-запрос для скорости
            StringBuilder sql = new StringBuilder("INSERT INTO telemetry (session_id, time_offset, pos_x, pos_y, pos_z, speed, steering_angle, wheel_slip) VALUES ");
            
            for (int i = 0; i < sessionData.Count; i++)
            {
                var f = sessionData[i];
                // InvariantCulture нужен, чтобы Unity передавал числа с точкой (1.5), а не с запятой (1,5)
                string values = string.Format(CultureInfo.InvariantCulture,
                    "({0}, {1:F3}, {2:F3}, {3:F3}, {4:F3}, {5:F3}, {6:F3}, {7:F3})",
                    currentSessionId, f.timeOffset, f.posX, f.posY, f.posZ, f.speed, f.steeringAngle, f.wheelSlip);
                
                sql.Append(values);
                
                if (i < sessionData.Count - 1)
                    sql.Append(", "); // Ставим запятую между строками
            }

            using (var cmd = new NpgsqlCommand(sql.ToString(), conn, transaction))
            {
                cmd.ExecuteNonQuery();
            }
            
            transaction.Commit(); // Подтверждаем транзакцию
            Debug.Log("<color=green>Успех! Телеметрия сохранена в PostgreSQL.</color>");
        }
    }

    private void OnDestroy()
    {
        // Проверяем: если запись шла, но гонка прервалась (не было вызвано FinishRace)
        if (isRecording && currentSessionId != -1)
        {
            CancelRace();
        }
    }

    // Метод для очистки базы от брошенных данных
    private void CancelRace()
    {
        isRecording = false;

        try
        {
            using (Npgsql.NpgsqlConnection conn = new Npgsql.NpgsqlConnection(connString))
            {
                conn.Open();
                // Удаляем сначала кадры телеметрии этой сессии, а затем саму пустую сессию
                string query = "DELETE FROM telemetry WHERE session_id = @id; DELETE FROM sessions WHERE id = @id;";
                
                using (Npgsql.NpgsqlCommand cmd = new Npgsql.NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", currentSessionId);
                    cmd.ExecuteNonQuery();
                }
            }
            Debug.Log("<color=orange>Гонка прервана. Пустая сессия удалена из БД.</color>");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Ошибка при удалении прерванной сессии: " + e.Message);
        }
    }
}