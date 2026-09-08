using System.Collections;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RestartVoteUI : MonoBehaviour
{
    private RectTransform panel;
    private CanvasGroup group;
    private TMP_Text status, count, footer;
    private Button yesButton, noButton;
    private Coroutine slide;
    private int shownVoteId = -1, sentVoteId = -1;
    private bool visible;
    private GameClock observedClock;
    private TMP_FontAsset runtimeFont;

    public static void Create(PauseMenuManager owner)
    {
        var root = new GameObject("RestartVoteUI", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(RestartVoteUI));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        TMP_FontAsset font = null;
        foreach (var label in owner.GetComponentsInChildren<TMP_Text>(true))
            if (label.font != null) { font = label.font; break; }
        var view = root.GetComponent<RestartVoteUI>();
        var sourceFont = Resources.Load<Font>("RestartVoteKoreanFont");
        if (sourceFont != null)
        {
            view.runtimeFont = TMP_FontAsset.CreateFontAsset(sourceFont);
            font = view.runtimeFont;
        }
        view.Build(font != null ? font : TMP_Settings.defaultFontAsset);
    }

    private void Build(TMP_FontAsset font)
    {
        panel = Rect("RestartVotePanel", transform, new Vector2(400, 270), Vector2.zero);
        panel.anchorMin = panel.anchorMax = new Vector2(1, 0.5f);
        panel.pivot = new Vector2(1, 0.5f);
        panel.anchoredPosition = new Vector2(440, 0);
        panel.gameObject.AddComponent<Image>().color = new Color(0.055f, 0.075f, 0.12f, 0.97f);
        var outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.25f, 0.65f, 0.9f, 0.6f);
        outline.effectDistance = new Vector2(1, -1);
        group = panel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0;
        group.blocksRaycasts = false;
        group.interactable = false;

        Label("Title", "재시작 투표", font, new Vector2(344, 42), new Vector2(0, 87), 30);
        status = Label("Status", "모두 동의하면 게임을 다시 시작합니다", font,
            new Vector2(350, 40), new Vector2(0, 43), 18);
        status.color = new Color(0.72f, 0.79f, 0.88f);
        count = Label("Count", "YES  0 / 0", font, new Vector2(350, 40), new Vector2(0, 2), 26);
        count.color = new Color(0.4f, 0.86f, 1);
        yesButton = MakeButton("Yes", font, new Vector2(-87, -53), new Color(0.08f, 0.43f, 0.58f), true);
        noButton = MakeButton("No", font, new Vector2(87, -53), new Color(0.36f, 0.18f, 0.24f), false);
        footer = Label("Footer", "남은 시간 30초", font, new Vector2(350, 30), new Vector2(0, -105), 17);
        footer.color = new Color(0.64f, 0.71f, 0.81f);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private TMP_Text Label(string name, string text, TMP_FontAsset font, Vector2 size, Vector2 position, float fontSize)
    {
        var label = Rect(name, panel, size, position).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private Button MakeButton(string title, TMP_FontAsset font, Vector2 position, Color color, bool yes)
    {
        var rect = Rect(title + "Button", panel, new Vector2(158, 50), position);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.65f);
        button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var text = Label(title + "Label", title, font, new Vector2(150, 44), Vector2.zero, 25);
        text.transform.SetParent(rect, false);
        button.onClick.AddListener(() => Submit(yes));
        return button;
    }

    private void Submit(bool yes)
    {
        var clock = GameClock.Instance;
        if (clock == null || clock != observedClock || clock.RestartVotePhase != GameClock.VoteOpen ||
            sentVoteId == shownVoteId || shownVoteId != clock.RestartVoteId) return;
        sentVoteId = shownVoteId;
        yesButton.interactable = noButton.interactable = false;
        clock.CastRestartVote(shownVoteId, yes);
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    private void Update()
    {
        if (panel == null) return;
        var clock = GameClock.Instance;
        if (clock == null || clock.Object == null || !clock.Object.IsValid)
        {
            SetVisible(false);
            observedClock = null;
            return;
        }
        if (clock != observedClock)
        {
            observedClock = clock;
            shownVoteId = sentVoteId = -1;
        }
        int phase = clock.RestartVotePhase;
        bool show = phase != GameClock.VoteIdle;
        if (show && shownVoteId != clock.RestartVoteId)
        {
            shownVoteId = clock.RestartVoteId;
            sentVoteId = -1;
        }
        SetVisible(show);
        if (!show) return;

        int choice = 0;
        bool eligible = clock.RestartBallots.TryGet(clock.Runner.LocalPlayer, out choice);
        bool canVote = phase == GameClock.VoteOpen && eligible && choice == 0 && sentVoteId != shownVoteId;
        yesButton.interactable = noButton.interactable = canVote;
        count.text = "YES  " + clock.RestartYesCount + " / " + clock.RestartBallots.Count;
        if (phase == GameClock.VoteOpen)
        {
            status.text = choice == 1 ? "Yes 투표 완료 · 다른 플레이어를 기다리는 중" :
                sentVoteId == shownVoteId ? "투표를 전달하고 있습니다" :
                "모두 동의하면 게임을 다시 시작합니다";
            footer.text = "남은 시간 " + Mathf.CeilToInt(clock.RestartDeadline.RemainingTime(clock.Runner) ?? 0) + "초";
        }
        else
        {
            status.text = phase == GameClock.VotePassed ? "만장일치! 게임을 다시 시작합니다" :
                phase == GameClock.VoteRejected ? "No 응답으로 투표가 종료되었습니다" :
                phase == GameClock.VoteCancelled ? "참가자가 변경되어 투표가 취소되었습니다" :
                phase == GameClock.VoteTimedOut ? "시간이 초과되어 투표가 종료되었습니다" :
                "재시작하지 못했습니다";
            footer.text = phase == GameClock.VotePassed ? "잠시만 기다려 주세요" : "게임을 계속 진행합니다";
        }
    }

    private void SetVisible(bool show)
    {
        if (visible == show) return;
        visible = show;
        if (slide != null) StopCoroutine(slide);
        group.blocksRaycasts = group.interactable = show;
        slide = StartCoroutine(Slide(show));
    }

    private IEnumerator Slide(bool show)
    {
        float from = panel.anchoredPosition.x, alpha = group.alpha;
        float duration = show ? 0.42f : 0.28f;
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 3);
            panel.anchoredPosition = new Vector2(Mathf.Lerp(from, show ? -28 : 440, eased), 0);
            group.alpha = Mathf.Lerp(alpha, show ? 1 : 0, eased);
            yield return null;
        }
        panel.anchoredPosition = new Vector2(show ? -28 : 440, 0);
        group.alpha = show ? 1 : 0;
        slide = null;
    }


    private void OnDestroy()
    {
        if (runtimeFont == null) return;
        foreach (var texture in runtimeFont.atlasTextures)
            if (texture != null) Destroy(texture);
        if (runtimeFont.material != null) Destroy(runtimeFont.material);
        Destroy(runtimeFont);
    }
}
