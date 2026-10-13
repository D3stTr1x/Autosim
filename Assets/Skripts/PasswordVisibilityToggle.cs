using UnityEngine;
using TMPro; // Не забудьте добавить этот неймспейс
using UnityEngine.UI;

public class PasswordVisibilityToggle : MonoBehaviour
{
    [Header("Настройки компонентов")]
    public TMP_InputField passwordField; // Ссылка на поле ввода
    public Image toggleIcon;             // Ссылка на иконку (опционально)

    [Header("Иконки (опционально)")]
    public Sprite visibleIcon;           // Спрайт "Глаз открыт"
    public Sprite hiddenIcon;            // Спрайт "Глаз закрыт"

    private bool isPasswordVisible = false;

    public void ToggleVisibility()
    {
        isPasswordVisible = !isPasswordVisible;

        if (isPasswordVisible)
        {
            passwordField.contentType = TMP_InputField.ContentType.Standard;
        }
        else
        {
            passwordField.contentType = TMP_InputField.ContentType.Password;
        }

        passwordField.ForceLabelUpdate();

        if (toggleIcon != null && visibleIcon != null && hiddenIcon != null)
        {
            toggleIcon.sprite = isPasswordVisible ? visibleIcon : hiddenIcon;
        }
    }
}