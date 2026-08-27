using Fusion;
using UnityEngine;

// =============================================================================
// NetworkLauncher.cs
// -----------------------------------------------------------------------------
// 역할 : PlayScene이 시작될 때 Fusion 네트워크 세션(방)에 자동으로 접속시켜준다.
//        방 이름이 같으면(SessionName = "DodgeRoom") 같은 방에 모이게 된다.
// 붙는 곳 : PlayScene.unity의 "NetworkLauncher" 오브젝트 (PlayerSpawner.cs와 같이 붙어있음)
// 모드 : GameMode.Shared — 별도의 전용 호스트 없이, 접속한 클라이언트끼리
//        서로 상태를 공유하는 방식(Photon Fusion의 Shared Mode).
// =============================================================================
public class NetworkLauncher : MonoBehaviour
{
    async void Start()
    {
        if (FindFirstObjectByType<NetworkRunner>() != null) return; // 로비에서 넘어온 경우 새로 접속하지 않음

        var runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;

        var startArgs = new StartGameArgs()
        {
            GameMode = GameModeState.Mode,       // 기존엔 GameMode.Shared 였던 걸 이걸로 교체
            SessionName = GameModeState.RoomName, // 기존엔 "DodgeRoom" 였던 걸 이걸로 교체
            Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        await runner.StartGame(startArgs);
    }
}
