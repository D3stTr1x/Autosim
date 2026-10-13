using UnityEngine;
using Npgsql;
using System;
using System.Security.Cryptography;
using System.Text;

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance;
    private string connString = "Host=localhost;Port=23690;Username=postgres;Password=Rage1945qftr326;Database=RaceGame;Pooling=false;";
    public int currentUserId = -1;
    // Переменная для хранения ID текущего игрока после входа
    public static int CurrentPlayerId { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            // Если мы первые, то мы - оригинал. Защищаем себя от удаления.
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Если оригинал уже существует, а мы клон из новой сцены - убиваем себя!
            Destroy(gameObject);
        }
    }

    // 1. Метод для создания Хэша пароля (SHA256)
    private string HashPassword(string password)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
                builder.Append(bytes[i].ToString("x2"));
            return builder.ToString();
        }
    }

    // 2. РЕГИСТРАЦИЯ
    public bool Register(string username, string password)
    {
        using (var conn = new NpgsqlConnection(connString))
        {
            try
            {
                conn.Open();
                string sql = "INSERT INTO players (username, password_hash) VALUES (@u, @p)";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("u", username);
                    cmd.Parameters.AddWithValue("p", HashPassword(password));
                    cmd.ExecuteNonQuery();
                    Debug.Log("Регистрация успешна!");
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Ошибка регистрации (возможно, ник занят): " + e.Message);
                return false;
            }
        }
    }

    // 3. ВХОД (Авторизация)
    public bool Login(string username, string password)
    {
        using (var conn = new NpgsqlConnection(connString))
        {
            try
            {
                conn.Open();
                string sql = "SELECT id, password_hash FROM players WHERE username = @u";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("u", username);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string savedHash = reader.GetString(1);
                            if (savedHash == HashPassword(password))
                            {
                                currentUserId = reader.GetInt32(0); 
                                CurrentPlayerId = currentUserId;    
                                
                                Debug.Log("Вход выполнен! ID игрока: " + currentUserId);
                                return true;
                            }
                        }
                    }
                }
                Debug.LogWarning("Неверный логин или пароль");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError("Ошибка входа: " + e.Message);
                return false;
            }
        }
    }
}