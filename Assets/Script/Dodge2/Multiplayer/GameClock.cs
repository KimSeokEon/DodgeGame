using Fusion;
using UnityEngine;

// =============================================================================
// GameClock.cs
// -----------------------------------------------------------------------------
// 역할 : 생존 타이머를 "네트워크 공유 시계"로 만든다.
//        예전엔 GameManager2가 각 클라에서 Time.deltaTime을 따로 누적해서
//        호스트/게스트 타이머가 서로 다르게 흘렀다. 이제는 마스터가 정한
//        StartTick 하나만 공유하고, 경과 시간은 각 클라가 그 틱에서 로컬로
//        계산하므로 모든 화면에서 완전히 동일하다.
// 붙는 곳 : Assets/Resources/GameClock.prefab (NetworkObject).
//        PlayerSpawner가 마스터(또는 싱글)에서 딱 한 번 Runner.Spawn 한다.
// 사용 : GameManager2가 GameClock.Instance.ElapsedSeconds 를 읽어서 표시만 한다.
// =============================================================================
public class GameClock : NetworkBehaviour
{
    // 씬에 하나만 존재. GameManager2 등에서 편하게 접근하려고 정적으로 들고 있는다.
    public static GameClock Instance { get; private set; }

    [Networked] private int StartTick { get; set; }          // 시계가 시작된 네트워크 틱
    [Networked] private NetworkBool Running { get; set; }     // 흐르는 중인지
    [Networked] private float FrozenElapsed { get; set; }     // 정지 시점까지 누적된 시간(초)

    public override void Spawned()
    {
        Instance = this;

        // 마스터(StateAuthority)가 시계를 시작한다. 이후 StartTick이 전 클라에 복제된다.
        if (HasStateAuthority && !Running)
        {
            FrozenElapsed = 0f;
            StartTick = Runner.Tick;
            Running = true;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this) Instance = null;
    }

    // 모든 클라가 같은 값을 얻는다.
    // Running이면: (지금 틱 - 공유된 StartTick) × 틱당 시간. 각자 로컬 계산이지만 기준이 같아 일치.
    // 멈춘 뒤엔: 정지 시점에 굳혀둔 FrozenElapsed 그대로.
    public float ElapsedSeconds
    {
        get
        {
            if (!Running) return FrozenElapsed;
            int elapsedTicks = Runner.Tick - StartTick;
            if (elapsedTicks < 0) elapsedTicks = 0;
            return FrozenElapsed + elapsedTicks * Runner.DeltaTime;
        }
    }

    // 마스터만. 게임오버 시 GameManager2.Endgame()에서 호출된다.
    // 비마스터가 불러도 무해하게 무시된다(마스터도 자기 화면에서 같이 호출하므로 결국 멈춤).
    public void Stop()
    {
        if (!HasStateAuthority || !Running) return;

        int elapsedTicks = Runner.Tick - StartTick;
        if (elapsedTicks < 0) elapsedTicks = 0;
        FrozenElapsed += elapsedTicks * Runner.DeltaTime;
        Running = false;
    }
}
