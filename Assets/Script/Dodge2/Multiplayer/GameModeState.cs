using Fusion;

public static class GameModeState
{
    public static GameMode Mode = GameMode.Single;
    public const string RoomName = "DodgeRoom";

    // 로비(Lobby) 등 이전 씬에서 이미 Fusion 세션 접속(PlayerJoined)을 마쳤는지.
    // PlayScene의 PlayerSpawner가 Start()에서 바로 스폰을 시도해도 되는지 판단하는 데 씀
    // (신선하게 접속하는 중이면 Start() 시점에 스폰하면 안 됨 - PlayerJoined보다 먼저
    // 스폰했다가 Fusion이 진짜 join을 처리하면서 그 오브젝트를 파괴해버리는 문제가 있었음)
    public static bool HasJoinedSession = false;
}