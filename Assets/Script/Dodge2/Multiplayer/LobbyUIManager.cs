using System.Linq;
using System.Text;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    public string playSceneName = "PlayScene";

    public TMP_Text playerListText;
    public Button readyButton;
    public TMP_Text readyButtonLabel;
    public Button startButton;
    public Button leaveButton;

    private NetworkRunner runner;
    private bool isLeaving = false;

    void Start()
    {
        readyButton.onClick.AddListener(OnReadyClicked);
        startButton.onClick.AddListener(OnStartClicked);
        leaveButton.onClick.AddListener(OnLeaveClicked);
    }

    void Update()
    {
        if (isLeaving) return;
        if (runner == null) runner = FindFirstObjectByType<NetworkRunner>();
        RefreshPlayerList();
        RefreshButtons();
    }

    private void RefreshPlayerList()
    {
        var players = FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None)
            .Where(p => p.Object != null && p.Object.IsValid)
            .OrderBy(p => p.Object.InputAuthority.PlayerId)
            .ToList();

        var sb = new StringBuilder();

        foreach (var p in players)
        {
            string nk = p.Nick.ToString();
            string name = string.IsNullOrEmpty(nk) ? LobbyPlayer.Fallback : nk;
            sb.AppendLine($"{name}{(p.HasStateAuthority ? " (Me)" : "")} - {(p.IsReady ? "Ready" : "Not Ready")}");
        }

        if (players.Count == 0) sb.AppendLine("Connecting..");

        playerListText.text = sb.ToString();
    }

    private void RefreshButtons()
    {
        var localPlayer = FindLocalLobbyPlayer();
        if (localPlayer != null && readyButtonLabel != null)
            readyButtonLabel.text = localPlayer.IsReady ? "READY" : "READY";

        bool isHost = runner != null && runner.IsSharedModeMasterClient;

        // 스폰이 끝난(Object.IsValid) LobbyPlayer만 대상으로 전원 Ready 여부 확인
        var allPlayers = FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);
        bool allReady = allPlayers.Length > 0 &&
                        allPlayers.All(p => p.Object != null && p.Object.IsValid && p.IsReady);

        startButton.interactable = isHost && allReady;
    }

    private LobbyPlayer FindLocalLobbyPlayer()
    {
        // p.HasStateAuthority / p.IsReady 는 Spawned() 전엔 접근 불가 → Object.IsValid 가드
        return FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.Object != null && p.Object.IsValid && p.HasStateAuthority);
    }

    private void OnReadyClicked()
    {
        FindLocalLobbyPlayer()?.ToggleReady();
    }

    private void OnStartClicked()
    {
        if (runner == null || !runner.IsSharedModeMasterClient) return;
        int buildIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/{playSceneName}.unity");
        if (buildIndex < 0) { Debug.LogError($"'{playSceneName}'이 Build Settings에 없습니다."); return; }

        isLeaving = true;
        runner.LoadScene(SceneRef.FromIndex(buildIndex), LoadSceneMode.Single, LocalPhysicsMode.None, false);
    }

    private void OnLeaveClicked()
    {
        isLeaving = true;
        if (runner != null) _ = runner.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }
}