using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("패널")]
    public GameObject buttonGroup;
    public GameObject modeSelectPanel;
    public GameObject nicknamePanel;      // 멀티플레이 누르면 뜨는 닉네임 입력 패널

    [Header("닉네임 입력")]
    public TMP_InputField nicknameInput;  // 닉네임 패널 안의 InputField
    [Min(1)] public int maxNicknameLength = 16;

    // ── 메인 → 모드선택 ──────────────────────────────────────────
    public void OnStartButton()
    {
        buttonGroup.SetActive(false);
        modeSelectPanel.SetActive(true);
        if (nicknamePanel != null) nicknamePanel.SetActive(false);
    }

    public void OnBackToMainButton()
    {
        modeSelectPanel.SetActive(false);
        if (nicknamePanel != null) nicknamePanel.SetActive(false);
        buttonGroup.SetActive(true);
    }

    // ── 싱글 ───────────────────────────────────────────────────
    public void OnSingleplayerButton()
    {
        GameModeState.Mode = GameMode.Single;
        SceneManager.LoadScene("PlayScene");
    }

    // ── 멀티 : 바로 씬 이동하지 않고 닉네임 패널을 띄운다 ────────────
    public void OnMultiplayerButton()
    {
        modeSelectPanel.SetActive(false);
        if (nicknamePanel != null) nicknamePanel.SetActive(true);

        if (nicknameInput != null)
        {
            nicknameInput.characterLimit = maxNicknameLength;
            nicknameInput.text = PlayerPrefs.GetString(LobbyPlayer.PrefsKey, "");
            nicknameInput.Select();
            nicknameInput.ActivateInputField();
        }
    }

    // 닉네임 패널의 [확인] 버튼 → 저장 후 로비로
    public void OnNicknameConfirm()
    {
        string name = (nicknameInput != null ? nicknameInput.text : "").Trim();
        if (name.Length > maxNicknameLength) name = name.Substring(0, maxNicknameLength);
        if (string.IsNullOrEmpty(name)) name = LobbyPlayer.Fallback;

        PlayerPrefs.SetString(LobbyPlayer.PrefsKey, name);
        PlayerPrefs.Save();

        SceneManager.LoadScene("Lobby");
    }

    // 닉네임 패널의 [뒤로] 버튼 → 모드선택으로 되돌림
    public void OnNicknameBack()
    {
        if (nicknamePanel != null) nicknamePanel.SetActive(false);
        modeSelectPanel.SetActive(true);
    }

    // ── 종료 ───────────────────────────────────────────────────
    public void GameExitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
