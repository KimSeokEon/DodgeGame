using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

// =============================================================================
// LobbyCharacterPreview.cs
// -----------------------------------------------------------------------------
// 역할 : 로비에 접속한 사람 수만큼 CharacterPreview 모델을 한 줄로 세우고,
//        각자 머리 위에 닉네임을 표시하며, PreviewCamera가 전원을 담도록 프레이밍.
// 붙는 곳 : Lobby.unity 의 "Preview Stage" 오브젝트 (PreviewCamera가 자식)
// 동작 : 플레이 모드 → 씬의 LobbyPlayer 들을 보고 자동으로 캐릭터를 늘리고 줄임.
//        에디터(비플레이) → editorDummyCount 개의 더미로 레이아웃 미리보기.
// 결과물(캐릭터/닉네임)은 씬에 저장되지 않음 (HideFlags.DontSave).
// =============================================================================
[ExecuteAlways]
[RequireComponent(typeof(Transform))]
public class LobbyCharacterPreview : MonoBehaviour
{
    [Header("참조")]
    public GameObject previewPrefab;        // Assets/Resources/CharacterPreview.prefab
    public Camera previewCamera;

    [Header("배치")]
    public float slotSpacing = 3.4f;        // 캐릭터 사이 간격
    public float faceYaw = 8f;              // 기본으로 정면에서 튼 각도 (3/4 살짝)
    [Tooltip("캐릭터마다 이 각도(±) 안에서 조금씩 다르게 정면을 봄. 0이면 전원 동일")]
    public float faceYawVariance = 15f;
    [Tooltip("아주 살짝 위/아래로도 흔들림")]
    public float facePitchVariance = 3f;
    public float groundY = 0f;              // 발이 닿는 로컬 높이

    [Header("닉네임")]
    public float nickHeight = 5.0f;         // 캐릭터 로컬 기준 텍스트 높이
    public float nickFontSize = 5f;
    public Color nickColor = Color.white;
    [Tooltip("커스텀 TMP 폰트 (비워두면 TMP 기본 폰트)")]
    public TMP_FontAsset nickFont;
    public FontStyles nickFontStyle = FontStyles.Normal;

    [Header("카메라 프레이밍")]
    public float paddingX = 1.35f;          // 좌우 여백 배율
    public float paddingY = 1.12f;          // 상하 여백 배율
    public float minDistance = 4f;

    [Header("에디터 미리보기")]
    [Range(0, 4)] public int editorDummyCount = 2;

    // 캐릭터 대략 크기 (CharacterPreview 프리팹 기준). 첫 생성 때 실측으로 갱신.
    private Vector3 _charSize = new Vector3(3.9f, 4.46f, 2.2f);

    private readonly Dictionary<int, GameObject> _views = new Dictionary<int, GameObject>();
    private int _lastDummyCount = -1;

    void OnEnable()
    {
        RemoveManualInstances();
        Rebuild();
    }

    void OnDisable()
    {
        ClearViews();
    }

    void Update()
    {
        // 에디터에서 더미 수를 바꾸면 즉시 반영, 그 외엔 매 프레임 위치/닉네임만 갱신
        if (!Application.isPlaying && editorDummyCount != _lastDummyCount)
            Rebuild();
        else
            Layout();
    }

    // ── 뷰 재구성 ────────────────────────────────────────────────────
    void Rebuild()
    {
        var ids = CurrentIds();

        // 없어진 것 제거
        foreach (var key in _views.Keys.Where(k => !ids.Contains(k)).ToList())
            DestroyView(key);

        // 새로 필요한 것 생성
        foreach (var id in ids)
            if (!_views.ContainsKey(id) || _views[id] == null)
                _views[id] = CreateView(id);

        _lastDummyCount = editorDummyCount;
        Layout();
    }

    void Layout()
    {
        var ids = CurrentIds();

        // 플레이 중 인원 변동 감지 → 재구성
        if (ids.Count != _views.Count || ids.Any(id => !_views.ContainsKey(id)))
        {
            Rebuild();
            return;
        }

        int n = ids.Count;
        for (int i = 0; i < n; i++)
        {
            if (!_views.TryGetValue(ids[i], out var v) || v == null) continue;

            // 카메라가 스테이지 +Z 쪽에서 -Z를 보므로, 화면 왼쪽 = 스테이지 +X.
            // index 0 이 화면 왼쪽에 오도록 부호를 뒤집는다.
            float x = ((n - 1) * 0.5f - i) * slotSpacing;
            v.transform.localPosition = new Vector3(x, groundY, 0f);

            // 정면을 보되 캐릭터(=플레이어 id)마다 살짝 다른 각도로. id 기반이라 매 프레임 안 흔들림.
            float yawJitter   = (Hash01(ids[i] * 3 + 1) - 0.5f) * 2f * faceYawVariance;
            float pitchJitter = (Hash01(ids[i] * 7 + 5) - 0.5f) * 2f * facePitchVariance;
            v.transform.localRotation = Quaternion.Euler(pitchJitter, faceYaw + yawJitter, 0f);

            // 닉네임: 텍스트 + 스타일(폰트/크기/색/높이)을 매 프레임 반영 → 인스펙터에서 바로 조정됨
            var tmp = v.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                // 플레이 중이면 LobbyPlayer.Nick(입장 전 정한 이름), 에디터 더미면 "Player {번호}"
                string nk = NickOf(ids[i]);
                tmp.text = string.IsNullOrEmpty(nk) ? $"Player {i + 1}" : nk;
                tmp.fontSize = nickFontSize;
                tmp.color = nickColor;
                tmp.fontStyle = nickFontStyle;
                if (nickFont != null && tmp.font != nickFont) tmp.font = nickFont;
                tmp.transform.localPosition = new Vector3(0f, nickHeight, 0f);
            }
        }

        FrameCamera(n);
    }

    // ── id / 닉네임 소스 ────────────────────────────────────────────
    List<int> CurrentIds()
    {
        if (Application.isPlaying)
        {
            return FindObjectsByType<LobbyPlayer>(FindObjectsInactive.Exclude)
                .Where(p => p.Object != null && p.Object.IsValid)
                .Select(p => p.Object.InputAuthority.PlayerId)
                .OrderBy(id => id)
                .ToList();
        }
        return Enumerable.Range(0, Mathf.Max(0, editorDummyCount)).ToList();
    }

    // 그 플레이어가 입장 전 정한 이름(LobbyPlayer.Nick)을 반환. 플레이 중이 아니면 "".
    // ("Player N" 폴백은 에디터 더미 표시용으로만 Layout() 에서 붙임)
    string NickOf(int id)
    {
        if (Application.isPlaying)
        {
            var lp = FindObjectsByType<LobbyPlayer>(FindObjectsInactive.Exclude)
                .FirstOrDefault(p => p.Object != null && p.Object.IsValid && p.Object.InputAuthority.PlayerId == id);
            if (lp != null) return lp.Nick.ToString();
        }
        return "";
    }

    // ── 뷰 생성/삭제 ───────────────────────────────────────────────
    GameObject CreateView(int id)
    {
        if (previewPrefab == null) return null;

        GameObject go;
#if UNITY_EDITOR
        go = Application.isPlaying
            ? Instantiate(previewPrefab, transform)
            : (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(previewPrefab, transform);
#else
        go = Instantiate(previewPrefab, transform);
#endif
        go.name = "Preview_" + id;
        go.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

        int previewLayer = LayerMask.NameToLayer("Preview");
        SetLayer(go, previewLayer);

        // 크기 실측 (한 번)
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            _charSize = b.size;
        }

        // 닉네임 텍스트
        var nick = new GameObject("NickText", typeof(TextMeshPro));
        nick.transform.SetParent(go.transform, false);
        nick.transform.localPosition = new Vector3(0f, nickHeight, 0f);
        nick.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
        var t = nick.GetComponent<TextMeshPro>();
        t.alignment = TextAlignmentOptions.Center;
        t.fontSize = nickFontSize;
        t.color = nickColor;
        t.fontStyle = nickFontStyle;
        if (nickFont != null) t.font = nickFont;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.GetComponent<RectTransform>().sizeDelta = new Vector2(8f, 2f);
        var fc = nick.AddComponent<FaceCamera>();
        fc.target = previewCamera;
        SetLayer(nick, previewLayer);

        return go;
    }

    void DestroyView(int id)
    {
        if (_views.TryGetValue(id, out var go) && go != null)
            SafeDestroy(go);
        _views.Remove(id);
    }

    void ClearViews()
    {
        foreach (var v in _views.Values) if (v != null) SafeDestroy(v);
        _views.Clear();
        _lastDummyCount = -1;
    }

    void RemoveManualInstances()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            if (previewCamera != null && c == previewCamera.transform) continue;
            if (c.name.StartsWith("CharacterPreview") || c.name.StartsWith("Preview_"))
                SafeDestroy(c.gameObject);
        }
    }

    // ── 카메라 프레이밍 ────────────────────────────────────────────
    void FrameCamera(int n)
    {
        if (previewCamera == null) return;

        // 세로 컨텐츠 = 발끝(groundY) ~ 닉네임 텍스트 위쪽까지
        float contentTop = Mathf.Max(_charSize.y, nickHeight + 0.7f);

        float rowSpan = Mathf.Max(0, n - 1) * slotSpacing;
        float contentW = rowSpan + _charSize.x * paddingX;
        float contentH = contentTop * paddingY;

        float vFov = previewCamera.fieldOfView * Mathf.Deg2Rad;
        float aspect = previewCamera.targetTexture != null
            ? (float)previewCamera.targetTexture.width / previewCamera.targetTexture.height
            : Mathf.Max(0.01f, previewCamera.aspect);
        float hFov = 2f * Mathf.Atan(Mathf.Tan(vFov * 0.5f) * aspect);

        float distW = (contentW * 0.5f) / Mathf.Tan(hFov * 0.5f);
        float distH = (contentH * 0.5f) / Mathf.Tan(vFov * 0.5f);
        float dist = Mathf.Max(minDistance, distW, distH) + _charSize.z * 0.5f;

        // 조준점을 컨텐츠(발끝~닉네임) 세로 중앙에
        Vector3 aimLocal = new Vector3(0f, groundY + contentTop * 0.5f, 0f);
        Vector3 aimWorld = transform.TransformPoint(aimLocal);

        // 캐릭터들이 스테이지 +Z를 보므로 카메라는 +Z 쪽에서 되돌아본다
        Vector3 camPos = aimWorld + transform.forward * dist + Vector3.up * (_charSize.y * 0.05f);
        previewCamera.transform.position = camPos;
        previewCamera.transform.rotation = Quaternion.LookRotation(aimWorld - camPos, Vector3.up);
    }

    // ── 유틸 ──────────────────────────────────────────────────────
    // id 하나로 안정적인 0~1 난수 (매 프레임 같은 값)
    static float Hash01(int n)
    {
        uint h = (uint)n * 2654435761u + 12345u;
        h ^= h >> 15; h *= 2246822519u;
        h ^= h >> 13; h *= 3266489917u;
        h ^= h >> 16;
        return (h & 0xFFFFFF) / (float)0x1000000;
    }

    static void SetLayer(GameObject g, int layer)
    {
        if (layer < 0) return;
        foreach (var tr in g.GetComponentsInChildren<Transform>(true))
            tr.gameObject.layer = layer;
    }

    static void SafeDestroy(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Destroy(o);
        else DestroyImmediate(o);
    }

#if UNITY_EDITOR
    [ContextMenu("미리보기 새로고침")]
    void ContextRebuild() { RemoveManualInstances(); ClearViews(); Rebuild(); }
#endif
}
