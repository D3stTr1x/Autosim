using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions; // Обязательно добавляем для работы с Regex

public class AuthUI : MonoBehaviour 
{
    [Header("База данных")]
    public DatabaseManager db;
    public GameObject raceModePanel;
    public GameObject loginPanel;
    public TMP_InputField loginUserInput;
    public TMP_InputField loginPassInput;
    public TMP_Text loginErrorText;

    [Header("Окно Регистрации")]
    public GameObject registerPanel;
    public TMP_InputField regUserInput;
    public TMP_InputField regPassInput;
    public TMP_InputField regPassConfirmInput;
    public TMP_Text regErrorText;
    public GameObject exitButton;

    [Header("Настройки валидации")]
    private const int MIN_USERNAME_LENGTH = 3;
    private const int MAX_USERNAME_LENGTH = 16;
    private const int MIN_PASSWORD_LENGTH = 6;
    // Разрешаем только латинские буквы, цифры и нижнее подчеркивание
    private const string USERNAME_REGEX = @"^[a-zA-Z0-9_]+$";

    private void Start()
    {
        // 1. Принудительно находим "выживший" оригинал базы данных
        if (DatabaseManager.Instance != null)
        {
            db = DatabaseManager.Instance;
        }

        // 2. Если в менеджере БД уже есть ID игрока, значит он уже входил
        if (db != null && db.currentUserId != -1)
        {
            Debug.Log("Игрок уже авторизован, ID: " + db.currentUserId);
            // Сразу показываем кнопки выбора режима, минуя логин
            loginPanel.SetActive(false);
            registerPanel.SetActive(false);
            raceModePanel.SetActive(true);
            exitButton.SetActive(false);
        }
        else
        {
            // Если ID нет (-1), показываем окно входа как обычно
            ShowLoginScreen();
            raceModePanel.SetActive(false);
            exitButton.SetActive(true);
        }
    }

    // --- МЕТОДЫ ПЕРЕКЛЮЧЕНИЯ ИНТЕРФЕЙСА ---

    public void ShowLoginScreen()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        if (loginErrorText != null) loginErrorText.text = ""; 
    }

    public void ShowRegisterScreen()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        if (regErrorText != null) regErrorText.text = "";
    }

    // --- МЕТОДЫ АВТОРИЗАЦИИ ---

    public void OnClickLogin() 
    {
        // Убираем случайные пробелы по краям
        string username = loginUserInput.text.Trim();
        string password = loginPassInput.text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            loginErrorText.text = "Заполните все поля!";
            return;
        }

        if (db.Login(username, password)) 
        {
            loginPanel.SetActive(false);
            raceModePanel.SetActive(true); 
            exitButton.SetActive(false);
        }
        else
        {
            loginErrorText.text = "Неверный логин или пароль!";
        }
    }

    public void OnClickRegister() 
    {
        // Очищаем ввод от лишних пробелов по краям
        string username = regUserInput.text.Trim();
        string password = regPassInput.text.Trim();
        string confirmPassword = regPassConfirmInput.text.Trim();

        // 1. Проверяем, что все три поля заполнены
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            regErrorText.text = "Заполните все поля!";
            return;
        }

        // 2. Валидация длины логина
        if (username.Length < MIN_USERNAME_LENGTH || username.Length > MAX_USERNAME_LENGTH)
        {
            regErrorText.text = $"Длина логина должна быть от {MIN_USERNAME_LENGTH} до {MAX_USERNAME_LENGTH} символов!";
            return;
        }

        // 3. Валидация допустимых символов логина
        if (!Regex.IsMatch(username, USERNAME_REGEX))
        {
            regErrorText.text = "Логин может содержать только a-z, A-Z, 0-9 и '_'.";
            return;
        }

        // 4. Валидация длины пароля
        if (password.Length < MIN_PASSWORD_LENGTH)
        {
            regErrorText.text = $"Пароль должен состоять минимум из {MIN_PASSWORD_LENGTH} символов!";
            return;
        }

        // 5. Проверяем, совпадают ли пароли
        if (password != confirmPassword)
        {
            regErrorText.text = "Пароли не совпадают!";
            return;
        }

        // 6. Отправляем в базу данных
        if (db.Register(username, password)) 
        {
            db.Login(username, password);
            registerPanel.SetActive(false);
            raceModePanel.SetActive(true); 
            exitButton.SetActive(false);
        }
        else
        {
            regErrorText.text = "Никнейм уже занят!";
        }
    }
}