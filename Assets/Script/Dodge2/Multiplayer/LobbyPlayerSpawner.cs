using Fusion;
using UnityEngine;

public class LobbyPlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    public GameObject LobbyPlayerPrefab;

    public void PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer)
        {
            GameModeState.HasJoinedSession = true; // PlayScene에서 Start() 스폰을 바로 시도해도 되는 신호
            Runner.Spawn(LobbyPlayerPrefab, Vector3.zero, Quaternion.identity, player);
        }
    }
}