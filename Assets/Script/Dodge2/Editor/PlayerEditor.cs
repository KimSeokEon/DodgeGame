using UnityEditor;
using UnityEngine;
using Fusion;

// =============================================================================
// PlayerEditor.cs  (Editor 전용 — 빌드에 안 들어감)
// -----------------------------------------------------------------------------
// Player 컴포넌트의 커스텀 인스펙터.
//  - 편집 모드 : 기존 필드 그대로 (DrawDefaultInspector)
//  - 플레이 모드 : 아래에 네트워크/권한 패널 + 실시간 상태(체력/다운/부활/쿨타임) +
//                디버그 버튼(피격/다운/부활)을 추가로 그린다.
// 멀티플레이 테스트할 때 호스트/게스트 각각의 상태를 눈으로 바로 확인하는 용도.
// =============================================================================
[CustomEditor(typeof(Player))]
public class PlayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var player = (Player)target;

        DrawDefaultInspector();

        if (!Application.isPlaying)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox("플레이 모드로 들어가면 여기에 네트워크 상태 패널과 디버그 버튼이 나옵니다.", MessageType.None);
            return;
        }

        NetworkObject no = player.Object;
        if (no == null || !no.IsValid)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox("아직 스폰되지 않았습니다 (네트워크 상태 없음).", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(10);
        Header("네트워크 / 권한");
        DrawAuthority(player, no);

        EditorGUILayout.Space(8);
        Header("실시간 상태");
        DrawState(player);

        EditorGUILayout.Space(8);
        Header("디버그 (자기 캐릭터에만 적용)");
        DrawDebugButtons(player);

        Repaint(); // 런타임 값이 매 프레임 갱신되도록
    }

    // ---------------------------------------------------------------------

    static void Header(string title)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }

    void DrawAuthority(Player player, NetworkObject no)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            Pill("STATE AUTH", player.HasStateAuthority);
            Pill("INPUT AUTH", player.HasInputAuthority);
            bool isMaster = player.Runner != null && player.Runner.IsSharedModeMasterClient;
            Pill("MASTER", isMaster);
        }

        EditorGUILayout.LabelField("PlayerId", no.InputAuthority.PlayerId.ToString());
        if (player.Runner != null)
            EditorGUILayout.LabelField("현재 Tick", ((int)player.Runner.Tick).ToString());
    }

    void DrawState(Player player)
    {
        int max = (player.heartAnimators != null && player.heartAnimators.Length > 0)
            ? player.heartAnimators.Length : 3;

        Rect r = GUILayoutUtility.GetRect(18, 18);
        EditorGUI.ProgressBar(r, max > 0 ? (float)player.Health / max : 0f, $"Health   {player.Health} / {max}");

        Color prev = GUI.color;
        GUI.color = player.IsDead ? new Color(1f, 0.55f, 0.55f) : Color.white;
        EditorGUILayout.LabelField("IsDead (다운)", player.IsDead ? "YES" : "no");
        GUI.color = prev;

        r = GUILayoutUtility.GetRect(18, 18);
        EditorGUI.ProgressBar(r, player.ReviveProgress01, $"부활 진행   {player.ReviveProgress01 * 100f:0}%");

        r = GUILayoutUtility.GetRect(18, 18);
        EditorGUI.ProgressBar(r, player.DodgeCooldownProgress01, $"구르기 쿨타임   {player.DodgeCooldownProgress01 * 100f:0}%");

        EditorGUILayout.LabelField("WantsRestart", player.WantsRestart ? "YES" : "no");
    }

    void DrawDebugButtons(Player player)
    {
        bool canPoke = player.HasStateAuthority;

        using (new EditorGUI.DisabledScope(!canPoke))
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("피격 (−1)")) player.Editor_Hit();
            if (GUILayout.Button("즉시 다운")) player.Editor_ForceDown();
            if (GUILayout.Button("부활")) player.Editor_Revive();
        }

        if (!canPoke)
            EditorGUILayout.HelpBox("이 캐릭터의 StateAuthority가 아니라서 버튼이 비활성화됩니다. (상대 클라의 캐릭터)", MessageType.None);
    }

    static void Pill(string label, bool on)
    {
        var style = new GUIStyle("Button") { fontSize = 10, fixedHeight = 18, alignment = TextAnchor.MiddleCenter };
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = on ? new Color(0.35f, 0.8f, 0.35f) : new Color(0.55f, 0.55f, 0.55f);
        GUILayout.Box(new GUIContent(label), style, GUILayout.MinWidth(72));
        GUI.backgroundColor = prev;
    }
}
