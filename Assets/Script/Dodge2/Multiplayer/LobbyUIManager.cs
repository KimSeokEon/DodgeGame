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
        var players = FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);
        var sb = new StringBuilder();
        foreach (var p in players)
        {
            int id = p.Object != null ? p.Object.InputAuthority.PlayerId : -1;
            string readyText = p.IsReady ? "Ready" : "Not Ready";
            sb.AppendLine($"Player {id}{(p.HasStateAuthority ? " (Me)" : "")} - {readyText}");
        }
        if (players.Length == 0) sb.AppendLine("Connecting..");
        playerListText.text = sb.ToString();
    }

    private void RefreshButtons()
    {
        var localPlayer = FindLocalLobbyPlayer();
        if (localPlayer != null && readyButtonLabel != null)
            readyButtonLabel.text = localPlayer.IsReady ? "READY" : "READY";

        bool isHost = runner != null && runner.IsSharedModeMasterClient;
        var allPlayers = FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None);
        bool allReady = allPlayers.Length > 0 && allPlayers.All(p => p.IsReady);
        startButton.interactable = isHost && allReady;
    }

    private LobbyPlayer FindLocalLobbyPlayer()
    {
        return FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None).FirstOrDefault(p => p.HasStateAuthority);
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