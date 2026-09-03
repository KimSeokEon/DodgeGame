using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// =============================================================================
// SceneToolsWindow.cs  (Editor 전용 — 빌드에 안 들어감)
// -----------------------------------------------------------------------------
// 씬 관리 도구 창. 메뉴: Tools ▸ Scene Tools
//  - 프로젝트의 모든 씬을 목록으로 보고, 버튼으로 열기 / 추가로 열기
//  - Build Settings 씬 리스트를 버튼으로 추가/제거, ▲▼로 순서 변경, 체크로 on/off
//  - 씬을 직접 열지 않아도 "+빌드" 버튼으로 빌드 목록에 넣을 수 있음
// 인스펙터 옆에 탭으로 도킹해서 쓰면 편함.
// =============================================================================
public class SceneToolsWindow : EditorWindow
{
    [MenuItem("Tools/Scene Tools")]
    public static void Open()
    {
        var w = GetWindow<SceneToolsWindow>("Scene Tools");
        w.minSize = new Vector2(340, 320);
        w.Show();
    }

    private string _search = "";
    private Vector2 _scrollBuild;
    private Vector2 _scrollProject;

    private void OnEnable()
    {
        EditorBuildSettings.sceneListChanged += Repaint;
        EditorApplication.hierarchyChanged += Repaint;
    }

    private void OnDisable()
    {
        EditorBuildSettings.sceneListChanged -= Repaint;
        EditorApplication.hierarchyChanged -= Repaint;
    }

    private void OnGUI()
    {
        DrawOpenScenesBar();
        EditorGUILayout.Space(6);
        DrawBuildList();
        EditorGUILayout.Space(10);
        DrawProjectScenes();
    }

    // ── 현재 열린 씬 상태 ──────────────────────────────────────────────
    private void DrawOpenScenesBar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            int n = EditorSceneManager.sceneCount;
            bool anyDirty = false;
            for (int i = 0; i < n; i++)
                if (EditorSceneManager.GetSceneAt(i).isDirty) anyDirty = true;

            GUILayout.Label($"열린 씬 {n}개" + (anyDirty ? "   *변경됨" : ""), EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(!anyDirty))
            {
                if (GUILayout.Button("전부 저장", EditorStyles.miniButton, GUILayout.Width(70)))
                    EditorSceneManager.SaveOpenScenes();
            }
        }
    }

    // ── Build Settings 씬 리스트 ──────────────────────────────────────
    private void DrawBuildList()
    {
        EditorGUILayout.LabelField("빌드에 포함될 씬 (Build Settings)  —  더블클릭으로 열기", EditorStyles.boldLabel);

        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Count == 0)
        {
            EditorGUILayout.HelpBox("빌드 씬 목록이 비어 있습니다. 아래 '프로젝트 씬'에서 [+빌드]로 추가하세요.", MessageType.Info);
            return;
        }

        Action deferred = null; // 목록 그리는 중엔 배열을 안 건드리고, 다 그린 뒤 적용

        _scrollBuild = EditorGUILayout.BeginScrollView(_scrollBuild, GUILayout.MaxHeight(190));

        int enabledCounter = 0;
        for (int i = 0; i < scenes.Count; i++)
        {
            int idx = i; // 클로저 캡처용
            var s = scenes[i];
            bool exists = File.Exists(s.path);
            string scenePath = s.path;

            Rect rowRect = EditorGUILayout.BeginHorizontal();
            {
                bool en = EditorGUILayout.Toggle(s.enabled, GUILayout.Width(16));
                if (en != s.enabled)
                    deferred = () => SetEnabled(idx, en);

                string indexLabel = (s.enabled && exists) ? enabledCounter.ToString() : "–";
                GUILayout.Label(indexLabel, GUILayout.Width(22));

                string sceneName = Path.GetFileNameWithoutExtension(s.path);
                var nameStyle = exists
                    ? EditorStyles.label
                    : new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.9f, 0.4f, 0.4f) } };
                GUILayout.Label(new GUIContent(exists ? sceneName : sceneName + "  (파일 없음)", s.path), nameStyle, GUILayout.MinWidth(70));

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(i == 0))
                    if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(22)))
                        deferred = () => Move(idx, idx - 1);
                using (new EditorGUI.DisabledScope(i == scenes.Count - 1))
                    if (GUILayout.Button("▼", EditorStyles.miniButtonMid, GUILayout.Width(22)))
                        deferred = () => Move(idx, idx + 1);
                if (GUILayout.Button("✕", EditorStyles.miniButtonRight, GUILayout.Width(24)))
                    deferred = () => RemoveAt(idx);
            }
            EditorGUILayout.EndHorizontal();

            // 행 위에서 더블클릭 → 그 씬 열기 (버튼 영역은 버튼이 먼저 이벤트를 먹으므로 이름/빈 공간에서 동작)
            if (exists)
            {
                EditorGUIUtility.AddCursorRect(rowRect, MouseCursor.Link);
                var e = Event.current;
                if (e.type == EventType.MouseDown && e.clickCount == 2 && rowRect.Contains(e.mousePosition))
                {
                    deferred = () => OpenSceneSingle(scenePath);
                    e.Use();
                }
            }

            if (s.enabled && exists) enabledCounter++;
        }

        EditorGUILayout.EndScrollView();

        deferred?.Invoke();
    }

    // ── 프로젝트 전체 씬 ─────────────────────────────────────────────
    private void DrawProjectScenes()
    {
        EditorGUILayout.LabelField("프로젝트 씬", EditorStyles.boldLabel);
        _search = EditorGUILayout.TextField("검색", _search);

        var buildPaths = new HashSet<string>(EditorBuildSettings.scenes.Select(s => s.path));

        var paths = AssetDatabase.FindAssets("t:SceneAsset")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.StartsWith("Assets/"))
            .Where(p => string.IsNullOrEmpty(_search) ||
                        p.ToLowerInvariant().Contains(_search.ToLowerInvariant()))
            .OrderBy(p => p)
            .ToList();

        Action deferred = null;

        _scrollProject = EditorGUILayout.BeginScrollView(_scrollProject);
        foreach (var p in paths)
        {
            string path = p;
            bool inBuild = buildPaths.Contains(path);

            using (new EditorGUILayout.HorizontalScope())
            {
                string sceneName = Path.GetFileNameWithoutExtension(path);
                if (GUILayout.Button(new GUIContent(sceneName, path), EditorStyles.label, GUILayout.MinWidth(80)))
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(path));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("열기", EditorStyles.miniButtonLeft, GUILayout.Width(40)))
                    deferred = () => OpenSceneSingle(path);
                if (GUILayout.Button("+씬", EditorStyles.miniButtonMid, GUILayout.Width(40)))
                    deferred = () => OpenSceneAdditive(path);
                using (new EditorGUI.DisabledScope(inBuild))
                {
                    if (GUILayout.Button(inBuild ? "빌드 ✓" : "+빌드", EditorStyles.miniButtonRight, GUILayout.Width(50)))
                        deferred = () => AddToBuild(path);
                }
            }
        }
        EditorGUILayout.EndScrollView();

        deferred?.Invoke();
    }

    // ── Build Settings 조작 헬퍼 ─────────────────────────────────────
    private static void SetEnabled(int index, bool enabled)
    {
        var list = EditorBuildSettings.scenes.ToList();
        if (index < 0 || index >= list.Count) return;
        list[index] = new EditorBuildSettingsScene(list[index].path, enabled);
        EditorBuildSettings.scenes = list.ToArray();
    }

    private static void Move(int from, int to)
    {
        var list = EditorBuildSettings.scenes.ToList();
        if (from < 0 || from >= list.Count || to < 0 || to >= list.Count) return;
        var item = list[from];
        list.RemoveAt(from);
        list.Insert(to, item);
        EditorBuildSettings.scenes = list.ToArray();
    }

    private static void RemoveAt(int index)
    {
        var list = EditorBuildSettings.scenes.ToList();
        if (index < 0 || index >= list.Count) return;
        list.RemoveAt(index);
        EditorBuildSettings.scenes = list.ToArray();
    }

    private static void AddToBuild(string path)
    {
        var list = EditorBuildSettings.scenes.ToList();
        if (list.Any(s => s.path == path)) return;
        list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    // ── 씬 열기 헬퍼 ────────────────────────────────────────────────
    private static void OpenSceneSingle(string path)
    {
        if (!File.Exists(path)) return;
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }

    private static void OpenSceneAdditive(string path)
    {
        if (!File.Exists(path)) return;
        EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
    }
}
