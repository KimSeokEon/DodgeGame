using Fusion;
using UnityEngine;

// =============================================================================
// LobbyPlayer.cs
// -----------------------------------------------------------------------------
// 로비에 접속한 한 명을 나타내는 네트워크 오브젝트.
// 닉네임은 "로비 입장 전"(MainMenu 등)에서 PlayerPrefs["Nickname"] 로 정해두고,
// 여기 Spawned()에서 읽어 네트워크로 공유한다 (다른 클라도 보게).
// =============================================================================
public class LobbyPlayer : NetworkBehaviour
{
    public const string PrefsKey = "Nickname";
    public const string Fallback = "Player";

    [Networked] public NetworkBool IsReady { get; set; }
    [Networked] public NetworkString<_32> Nick { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            string saved = PlayerPrefs.GetString(PrefsKey, "").Trim();
            Nick = string.IsNullOrEmpty(saved) ? Fallback : saved;
        }
    }

    public void ToggleReady()
    {
        if (!HasStateAuthority) return; // 내 것만 바꿀 수 있음
        IsReady = !IsReady;
    }
}
