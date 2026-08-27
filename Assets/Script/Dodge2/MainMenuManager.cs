using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public GameObject buttonGroup;
    public GameObject modeSelectPanel;

    public void OnStartButton()
    {
        buttonGroup.SetActive(false);
        modeSelectPanel.SetActive(true);
    }

    public void OnBackToMainButton()
    {
        modeSelectPanel.SetActive(false);
        buttonGroup.SetActive(true);
    }

    public void OnSingleplayerButton()
    {
        GameModeState.Mode = GameMode.Single;
        SceneManager.LoadScene("PlayScene");
    }

    public void OnMultiplayerButton()
    {
        SceneManager.LoadScene("Lobby");
    }

    public void GameExitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}