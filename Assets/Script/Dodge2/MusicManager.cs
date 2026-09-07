using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// =============================================================================
// MusicManager.cs
// -----------------------------------------------------------------------------
// 역할 : 배경음악(BGM)을 씬 전환과 무관하게 이어서 재생/전환한다.
//   - MainMenu, Lobby : mainTheme  (두 씬 사이를 오가도 "안 끊기고" 계속)
//   - PlayScene        : ingameTheme
// 원리 : DontDestroyOnLoad 싱글톤. 씬이 로드될 때마다 "이 씬이 원하는 곡"을 보고,
//        지금 나오는 곡과 같으면 아무것도 안 하고(=끊김 없음), 다르면 크로스페이드.
//
// 붙는 곳 : "MusicManager" 프리팹 하나를 만들어서 MainMenu / Lobby / PlayScene
//          3개 씬에 다 넣어둔다. 먼저 로드된 게 살아남고, 나중에 로드된 씬의
//          중복 인스턴스는 Awake에서 스스로 사라진다 → 어느 씬을 직접 열어도 동작.
// =============================================================================
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("클립")]
    [SerializeField] private AudioClip mainTheme;    // MainMenu + Lobby
    [SerializeField] private AudioClip ingameTheme;  // PlayScene

    [Header("씬 이름 매핑")]
    [SerializeField] private string[] mainThemeScenes = { "MainMenu", "Lobby" };
    [SerializeField] private string[] ingameScenes = { "PlayScene" };

    [Header("설정")]
    [Range(0f, 1f)] [SerializeField] private float volume = 0.6f;
    [SerializeField] private float fadeDuration = 1f;

    private AudioSource _source;
    private AudioClip _current;
    private Coroutine _fade;

    void Awake()
    {
        // 이미 다른 씬에서 만들어진 게 있으면 이 인스턴스는 버린다 (곡 안 끊기게)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _source = GetComponent<AudioSource>();
        _source.loop = true;
        _source.playOnAwake = false;
        _source.spatialBlend = 0f; // 2D (카메라 위치 무관)
        _source.volume = volume;

        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyForScene(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyForScene(scene.name);
    }

    private void ApplyForScene(string sceneName)
    {
        AudioClip want = null;
        if (System.Array.IndexOf(mainThemeScenes, sceneName) >= 0) want = mainTheme;
        else if (System.Array.IndexOf(ingameScenes, sceneName) >= 0) want = ingameTheme;

        // 매핑 없는 씬이면 현재 곡 그대로 유지
        if (want == null) return;

        // 이미 그 곡이 나오는 중이면 건드리지 않는다 → MainMenu ↔ Lobby 끊김 없음
        if (want == _current && _source.isPlaying) return;

        _current = want;
        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(SwitchRoutine(want));
    }

    private IEnumerator SwitchRoutine(AudioClip next)
    {
        // 나오던 곡 페이드 아웃
        if (_source.isPlaying)
        {
            float t = 0f;
            float from = _source.volume;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                _source.volume = Mathf.Lerp(from, 0f, t / fadeDuration);
                yield return null;
            }
        }

        _source.clip = next;
        _source.Play();

        // 새 곡 페이드 인
        float u = 0f;
        while (u < fadeDuration)
        {
            u += Time.unscaledDeltaTime;
            _source.volume = Mathf.Lerp(0f, volume, u / fadeDuration);
            yield return null;
        }
        _source.volume = volume;
        _fade = null;
    }

    // 옵션 메뉴 등에서 볼륨 조절할 때 호출
    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        if (_fade == null && _source != null)
            _source.volume = volume;
    }
}
