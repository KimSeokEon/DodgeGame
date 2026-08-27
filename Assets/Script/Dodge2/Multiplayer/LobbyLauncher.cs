using Fusion;
using UnityEngine;

public class LobbyLauncher : MonoBehaviour
{
    async void Start()
    {
        DontDestroyOnLoad(gameObject); // 중요: 게임 시작 시 PlayScene으로 넘어가도 세션이 안 끊기게

        var runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;

        var startArgs = new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = GameModeState.RoomName,
            Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        await runner.StartGame(startArgs);
    }
}