using System;
using Fusion;
using UnityEngine;

// =============================================================================
// PlayerSpawner.cs
// -----------------------------------------------------------------------------
// 역할 : Fusion 네트워크 세션에서 내 캐릭터(Player 프리팹)를 생성해준다.
// 붙는 곳 : PlayScene.unity의 "NetworkLauncher" 오브젝트 (NetworkLauncher.cs와 같이 붙어있음)
// 두 가지 경로로 스폰이 트리거될 수 있다:
//   1) PlayerJoined 콜백 - 이 씬에서 "새로" 접속하는 경우 (싱글플레이,
//      또는 로비를 안 거치고 PlayScene을 직접 여는 테스트 상황)
//   2) Update() - 로비(Lobby 씬)에서 이미 세션에 접속되어 있던 상태로 이 씬에
//      들어온 경우. GameModeState.HasJoinedSession으로 판단.
// 주의(중요, 실제로 겪은 버그 2가지) :
//   (a) Start()에서 무조건 스폰을 시도하면 안 된다 — GameMode.Single은 접속이
//       워낙 빨라서 Start() 시점에 이미 Runner.LocalPlayer가 채워져 있는
//       것처럼 보이지만, 그 시점에 스폰하면 Fusion이 곧이어 처리하는 "진짜"
//       PlayerJoined 이벤트가 그 오브젝트를 파괴해버린다. 그래서 새로 접속하는
//       중이면(HasJoinedSession == false) 반드시 PlayerJoined만 기다린다.
//   (b) 로비를 거쳐 이미 접속된 채로 이 씬에 들어온 경우, 이 컴포넌트(SimulationBehaviour)
//       자신의 Runner 프로퍼티는 끝까지 null로 남는다 — Fusion이 "새로 로드된
//       씬에 있던 이 컴포넌트"를 러너에 자동으로 등록해주지 않기 때문으로 보임.
//       그래서 이 경우엔 자기 Runner 프로퍼티 대신, 씬에서 직접 NetworkRunner를
//       찾아서(FindFirstObjectByType) 그걸로 스폰한다.
// 주의(멀티플레이) : 스폰 위치가 항상 Vector3.zero라서, 여러 명이 접속하면
//        전부 같은 자리에 겹쳐서 생성된다. 접속 순서(PlayerRef)별로 다른
//        스폰 지점을 골라주는 게 후속 작업.
// =============================================================================
public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    [Header("PlayerId별 스폰 위치 (씬에 빈 오브젝트 만들어서 연결)")]
    public Transform[] spawnPoints;
    
    public GameObject PlayerPrefab; // Assets/Resources/Player (1).prefab (NetworkObject가 붙어있는 네트워크 프리팹)

    public static event Action LocalPlayerSpawned; // 내 캐릭터가 스폰된 후에 보낼 신호

    private bool spawned = false;

    // 로비를 거쳐서 이미 접속된 상태로 이 씬에 들어온 경우를 위한 경로.
    // 스폰될 때까지 매 프레임 재시도한다 (Runner 등록이 몇 프레임 늦게 될 수 있어서).
    void Update()
    {
        if (spawned || !GameModeState.HasJoinedSession) return;

        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null || runner.LocalPlayer == PlayerRef.None) return;
        if (runner.IsSceneManagerBusy) return; // 씬 전환(로딩)이 아직 안 끝났으면 대기

        TrySpawnLocalPlayer(runner, runner.LocalPlayer);
    }

    // Fusion이 "누군가 접속했다"고 알려줄 때마다 호출됨 (본인 포함, 모든 클라이언트에서 각자 호출됨)
    // 이 씬에서 "새로" 접속하는 경우(싱글플레이 등)에 정상적으로 오는 콜백.
    public void PlayerJoined(PlayerRef player)
    {
        if (Runner != null && player == Runner.LocalPlayer)
        {
            GameModeState.HasJoinedSession = true; // 이 세션에 정식으로 접속 완료
            TrySpawnLocalPlayer(Runner, player);
        }
    }

    private void TrySpawnLocalPlayer(NetworkRunner runner, PlayerRef localPlayer)
    {
        if (spawned) return;
        spawned = true;

        // PlayerId로 스폰 지점 선택 (포인트 수보다 많아지면 wrap)
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int idx = localPlayer.PlayerId % spawnPoints.Length;
            Transform t = spawnPoints[idx];
            spawnPos = t.position;
            spawnRot = t.rotation;
        }

        var playerObj = runner.Spawn(PlayerPrefab, spawnPos, spawnRot, localPlayer);

        var follow = FindFirstObjectByType<Follow>();
        if (follow != null)
            follow.target = playerObj.transform;

        LocalPlayerSpawned?.Invoke();
    }
}
