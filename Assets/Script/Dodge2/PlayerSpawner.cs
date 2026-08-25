using System;
using Fusion;
using UnityEngine;

// =============================================================================
// PlayerSpawner.cs
// -----------------------------------------------------------------------------
// 역할 : Fusion 네트워크 세션에 누군가 접속(PlayerJoined)하면, 그 사람의
//        캐릭터(Player 프리팹)를 생성해준다.
// 붙는 곳 : PlayScene.unity의 "NetworkLauncher" 오브젝트 (NetworkLauncher.cs와 같이 붙어있음)
// 주의(멀티플레이) : 스폰 위치가 항상 Vector3.zero라서, 여러 명이 접속하면
//        전부 같은 자리에 겹쳐서 생성된다. "누구 캐릭터인지 구분이 안 된다"는
//        증상의 주요 원인 중 하나 — 접속 순서(PlayerRef)별로 다른 스폰 지점을
//        골라주는 게 후속 작업.
// =============================================================================
public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    public GameObject PlayerPrefab; // Assets/Resources/Player (1).prefab (NetworkObject가 붙어있는 네트워크 프리팹)

    
    public static event Action LocalPlayerSpawned; // 내 캐릭터가 스폰된 후에 보낼 신호 
    
    // Fusion이 "누군가 접속했다"고 알려줄 때마다 호출됨 (본인 포함, 모든 클라이언트에서 각자 호출됨)
    public void PlayerJoined(PlayerRef player)
    {
        // 내가 접속했을 때만 내 캐릭터를 생성한다 (다른 사람이 접속한 이벤트는 무시 —
        // 그 사람 캐릭터는 그 사람의 클라이언트에서 생성되고, Fusion이 알아서 내 화면에도 복제해준다)
        if (player == Runner.LocalPlayer)
        {
            var playerObj = Runner.Spawn(PlayerPrefab, Vector3.zero, Quaternion.identity, player);

            // 카메라가 방금 생성된 내 캐릭터를 따라가도록 연결
            var follow = FindFirstObjectByType<Follow>();
            if (follow != null)
            {
                follow.target = playerObj.transform;
            }
            LocalPlayerSpawned?.Invoke(); //캐릭터 생성이 끝난 이후에 신호 전송 
        }
    }
}
