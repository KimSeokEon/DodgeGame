using Fusion;

public class LobbyPlayer : NetworkBehaviour
{
    [Networked] public NetworkBool IsReady { get; set; }

    public void ToggleReady()
    {
        if (!HasStateAuthority) return; // 내 것만 바꿀 수 있음
        IsReady = !IsReady;
    }
}