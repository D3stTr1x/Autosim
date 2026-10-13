using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuButtonManager : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private string driftSceneName = "DriftScene2";
    [SerializeField] private string nascarSceneName = "NascarScene";

    [SerializeField] private Image menuImg;
    [SerializeField] private Button menuButton1;
    [SerializeField] private Button menuButton2;
    [SerializeField] private Button menuButton3;


    private string scene;
    private bool checkMenu = false;

    private void Update()
    {
        scene = SceneManager.GetActiveScene().name;
        OpenMenu();
    }

    public void ContnueBut()
    {
        menuImg.gameObject.SetActive(false);
        menuButton1.gameObject.SetActive(false);
        menuButton2.gameObject.SetActive(false);
        menuButton3.gameObject.SetActive(false);

        checkMenu = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ToMenu()
    {
        menuImg.gameObject.SetActive(false);
        menuButton1.gameObject.SetActive(false);
        menuButton2.gameObject.SetActive(false); 
        menuButton3.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneTransition.SwitchToScene(menuSceneName);
    }

    public void LoadDriftScene()
    {
        SceneTransition.SwitchToScene(driftSceneName);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void LoadNascarScene()
    {
        SceneTransition.SwitchToScene(nascarSceneName);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
    
    public void OpenMenu()
    {
        if ((scene != "MainMenu") && (!checkMenu) && (Input.GetKeyDown(KeyCode.Escape)))
        {
            menuImg.gameObject.SetActive(true);
            menuButton1.gameObject.SetActive(true);
            menuButton2.gameObject.SetActive(true);
            menuButton3.gameObject.SetActive(true);

            menuButton1.interactable = true;
            menuButton2.interactable = true;
            menuButton3.interactable = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            checkMenu = true;
        }
        else if ((checkMenu) && (Input.GetKeyDown(KeyCode.Escape)))
        {
            ContnueBut();
        }
    }
}