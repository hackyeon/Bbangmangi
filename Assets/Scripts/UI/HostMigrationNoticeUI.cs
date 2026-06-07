using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HostMigrationNoticeUI : MonoBehaviour
{
    private const string NoticeFontName = "Pretendard-Regular SDF";

    private static HostMigrationNoticeUI instance;

    private CanvasGroup canvasGroup;
    private TMP_Text messageText;
    private Coroutine fadeRoutine;

    public static HostMigrationNoticeUI GetOrCreate()
    {
        if (instance != null)
            return instance;

        HostMigrationNoticeUI existing =
            FindFirstObjectByType<HostMigrationNoticeUI>();

        if (existing != null)
            return existing;

        GameObject uiObject =
            new GameObject(nameof(HostMigrationNoticeUI));

        DontDestroyOnLoad(uiObject);
        return uiObject.AddComponent<HostMigrationNoticeUI>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        Build();
        HideImmediate();
    }

    public void Show(string message)
    {
        if (messageText != null)
            messageText.text = message;

        gameObject.SetActive(true);
        FadeTo(1f);
    }

    public void Hide()
    {
        FadeTo(0f);
    }

    private void HideImmediate()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        gameObject.SetActive(false);
    }

    private void FadeTo(float targetAlpha)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private System.Collections.IEnumerator FadeRoutine(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        const float duration = 0.18f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                elapsed / duration
            );

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        if (Mathf.Approximately(targetAlpha, 0f))
            gameObject.SetActive(false);
    }

    private void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        GameObject panelObject =
            new GameObject("NoticePanel", typeof(RectTransform));

        panelObject.transform.SetParent(transform, false);

        RectTransform panelRect =
            panelObject.GetComponent<RectTransform>();

        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, 170f);
        panelRect.sizeDelta = new Vector2(620f, 112f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.62f);

        GameObject textObject =
            new GameObject("NoticeText", typeof(RectTransform));

        textObject.transform.SetParent(panelObject.transform, false);

        RectTransform textRect =
            textObject.GetComponent<RectTransform>();

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(28f, 16f);
        textRect.offsetMax = new Vector2(-28f, -16f);

        messageText = textObject.AddComponent<TextMeshProUGUI>();
        messageText.font = FindNoticeFont();
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.color = Color.white;
        messageText.fontSize = 28f;
        messageText.raycastTarget = false;
        messageText.enableWordWrapping = true;
    }

    private static TMP_FontAsset FindNoticeFont()
    {
        TMP_FontAsset[] fonts =
            Resources.FindObjectsOfTypeAll<TMP_FontAsset>();

        foreach (TMP_FontAsset font in fonts)
        {
            if (font != null && font.name == NoticeFontName)
                return font;
        }

        return null;
    }
}
