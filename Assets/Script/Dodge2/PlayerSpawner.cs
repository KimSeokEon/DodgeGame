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
// 스폰 위치 : spawnPoints[] 에 넣어둔 지점들에 "접속 순서"대로 배치한다.
//        PlayerId 를 직접 인덱스로 쓰면 안 됨 — Shared Mode 의 PlayerId 는
//        Photon actor number(1부터 시작, 중간에 나가면 구멍) 라 순번이 아니다.
//        대신 ActivePlayers 중 나보다 PlayerId 작은 사람 수 = 내 0-based 순번.
// =============================================================================
public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    [Header("스폰 위치 (접속 순서대로 배치. 빈 오브젝트 만들어서 연결)")]
    public Transform[] spawnPoints;

    public GameObject PlayerPrefab; // Assets/Resources/Player (1).prefab (NetworkObject가 붙어있는 네트워크 프리팹)

    [Header("네트워크 공유 생존 타이머 (Assets/Resources/GameClock.prefab)")]
    public GameObject GameClockPrefab; // 마스터(또는 싱글)가 딱 한 번 스폰

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

        // 내 "접속 순번" = ActivePlayers 중 나보다 PlayerId 작은 사람 수 (0,1,2,3...)
        // PlayerId 를 그대로 쓰면 안 됨 (Photon actor 값이라 1부터 시작 + 구멍 가능)
        int slot = 0;
        foreach (var p in runner.ActivePlayers)
            if (p.PlayerId < localPlayer.PlayerId) slot++;

        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int idx = slot % spawnPoints.Length; // 인원이 포인트보다 많으면 wrap
            Transform t = spawnPoints[idx];
            if (t != null)
            {
                spawnPos = t.position;
                spawnRot = t.rotation;
            }
        }

        var playerObj = runner.Spawn(PlayerPrefab, spawnPos, spawnRot, localPlayer);

        var follow = FindFirstObjectByType<Follow>();
        if (follow != null)
            follow.target = playerObj.transform;

        // ★ 다른 스크립트(EnemySpawner, GameManager2 등)에 먼저 신호를 보낸다.
        //   아래 GameClock 스폰이 실패하더라도 게임 진행 자체는 막히지 않도록 순서를 앞에 둔다.
        LocalPlayerSpawned?.Invoke();

        // 공유 생존 타이머는 마스터(또는 싱글)가 씬에 한 번만 스폰한다.
        if (GameClockPrefab != null
            && GameClock.Instance == null
            && (runner.IsServer || runner.IsSharedModeMasterClient))
        {
            try
            {
                runner.Spawn(GameClockPrefab);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"GameClock 스폰 실패 (프리팹 테이블 등록 확인 필요): {e.Message}");
            }
        }
    }
}
