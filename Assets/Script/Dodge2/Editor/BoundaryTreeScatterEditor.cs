using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// =============================================================================
// BoundaryTreeScatterEditor.cs  (Editor 전용 — 빌드에 안 들어감)
// -----------------------------------------------------------------------------
// BoundaryTreeScatter 의 커스텀 인스펙터. [나무 생성] / [지우기] 버튼과
// 실제 배치 로직을 담는다. Undo 지원.
// =============================================================================
[CustomEditor(typeof(BoundaryTreeScatter))]
public class BoundaryTreeScatterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var s = (BoundaryTreeScatter)target;
        var lr = s.boundary != null ? s.boundary : s.GetComponent<LineRenderer>();

        EditorGUILayout.Space(8);

        int existing = CountExisting(s);
        EditorGUILayout.LabelField("현재 배치된 나무", existing.ToString());

        using (new EditorGUI.DisabledScope(lr == null || s.treePrefab == null))
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("나무 생성", GUILayout.Height(28)))
                Generate(s, lr);
            using (new EditorGUI.DisabledScope(existing == 0))
                if (GUILayout.Button("지우기", GUILayout.Height(28), GUILayout.Width(80)))
                    Clear(s);
        }

        if (lr == null)
            EditorGUILayout.HelpBox("LineRenderer(boundary)가 필요합니다.", MessageType.Warning);
        if (s.treePrefab == null)
            EditorGUILayout.HelpBox("treePrefab(나무)이 필요합니다.", MessageType.Warning);
    }

    // ------------------------------------------------------------------

    static int CountExisting(BoundaryTreeScatter s)
    {
        var c = s.transform.Find(BoundaryTreeScatter.ContainerName);
        return c != null ? c.childCount : 0;
    }

    static void Clear(BoundaryTreeScatter s)
    {
        var c = s.transform.Find(BoundaryTreeScatter.ContainerName);
        if (c != null)
            Undo.DestroyObjectImmediate(c.gameObject);
        EditorSceneManager.MarkSceneDirty(s.gameObject.scene);
    }

    static void Generate(BoundaryTreeScatter s, LineRenderer lr)
    {
        Clear(s);

        var container = new GameObject(BoundaryTreeScatter.ContainerName);
        Undo.RegisterCreatedObjectUndo(container, "Generate Boundary Trees");
        container.transform.SetParent(s.transform, false);
        container.transform.localPosition = Vector3.zero;
        container.transform.localRotation = Quaternion.identity;
        // BoundaryLine 오브젝트가 (30,1,30) 같은 스케일을 갖고 있으면 자식 나무가 눌린다.
        // 컨테이너를 부모 스케일의 역수로 맞춰서 나무가 월드에서 1:1 크기로 서게 한다.
        Vector3 pls = s.transform.lossyScale;
        container.transform.localScale = new Vector3(
            Mathf.Approximately(pls.x, 0f) ? 1f : 1f / pls.x,
            Mathf.Approximately(pls.y, 0f) ? 1f : 1f / pls.y,
            Mathf.Approximately(pls.z, 0f) ? 1f : 1f / pls.z);

        // 경계선 폴리곤 점 (월드 좌표로 통일)
        int n = lr.positionCount;
        if (n < 2) { Debug.LogWarning("BoundaryLine 점이 부족합니다."); return; }
        var pts = new Vector3[n];
        for (int i = 0; i < n; i++)
            pts[i] = lr.useWorldSpace ? lr.GetPosition(i) : lr.transform.TransformPoint(lr.GetPosition(i));

        int edges = lr.loop ? n : n - 1;

        Vector3 centroid = Vector3.zero;
        for (int i = 0; i < n; i++) centroid += pts[i];
        centroid /= n;

        var rnd = new System.Random(s.seed);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        Vector3 baseScale = s.treePrefab.transform.localScale;
        int rows = Mathf.Max(1, s.rows);
        int count = 0;

        for (int e = 0; e < edges; e++)
        {
            Vector3 a = pts[e];
            Vector3 b = pts[(e + 1) % n];
            Vector3 seg = b - a;
            float len = seg.magnitude;
            if (len < 0.01f) continue;

            Vector3 dir = seg / len;
            Vector3 perp = Vector3.Cross(Vector3.up, dir).normalized;
            // perp 가 "바깥쪽"을 향하도록 (중심에서 멀어지는 방향)
            if (Vector3.Dot(perp, ((a + b) * 0.5f) - centroid) < 0f) perp = -perp;

            float margin = s.avoidCorners ? Mathf.Min(s.spacing * 0.5f, len * 0.15f) : 0f;

            for (int row = 0; row < rows; row++)
            {
                float rowOut = s.outwardOffset + row * s.rowGap;
                // 겹마다 시작 위치를 어긋나게 해서 줄이 안 맞게
                float d = margin + R(0f, s.spacing) + row * s.spacing * 0.5f;

                while (d < len - margin)
                {
                    float along = Mathf.Clamp(d + R(-s.alongJitter, s.alongJitter), 0f, len);
                    Vector3 pos = a
                        + dir * along
                        + perp * (rowOut + R(-s.lateralJitter, s.lateralJitter));
                    pos.y = s.groundY - s.sinkDepth;

                    var tree = (GameObject)PrefabUtility.InstantiatePrefab(s.treePrefab, container.transform);
                    tree.transform.position = pos;

                    // 프리팹 기본 회전(축 보정 등)을 유지한 채, 세계축 기준으로 방향/기울기만 준다.
                    tree.transform.rotation = s.treePrefab.transform.rotation;
                    tree.transform.Rotate(Vector3.up, R(0f, 360f), Space.World);      // 무작위 방향
                    tree.transform.Rotate(Vector3.right, R(-s.tiltMax, s.tiltMax), Space.World);   // 살짝 기울임
                    tree.transform.Rotate(Vector3.forward, R(-s.tiltMax, s.tiltMax), Space.World);

                    // 크기: 균일 스케일 (회전된 루트에 비균일 스케일을 주면 찌그러지므로)
                    float m = R(s.scaleMultiplier.x, s.scaleMultiplier.y);
                    tree.transform.localScale = baseScale * m;

                    tree.isStatic = true;
                    Undo.RegisterCreatedObjectUndo(tree, "Generate Boundary Trees");

                    count++;
                    d += s.spacing * Mathf.Max(0.15f, 1f + R(-s.spacingJitter, s.spacingJitter));
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(s.gameObject.scene);
        Debug.Log($"BoundaryTreeScatter: 나무 {count}그루 생성 (seed {s.seed})");
        Selection.activeGameObject = container;
    }
}
