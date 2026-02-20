using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Singleton scene transition manager. Persists across scenes.
/// Provides FadeAndLoad(sceneName) for a smooth black-fade transition.
///
/// SETUP: Add this script to a new empty GameObject in your STARTING scene.
/// The canvas and overlay image are created automatically at runtime.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Fade Settings")]
    [Tooltip("Seconds to fade to black before loading scene")]
    public float fadeOutDuration = 0.6f;
    [Tooltip("Seconds to fade from black after scene loads")]
    public float fadeInDuration  = 0.8f;
    [Tooltip("Fade color (usually black)")]
    public Color fadeColor = Color.black;

    private CanvasGroup canvasGroup;
    private bool isFading;

    void Awake()
    {
        // Singleton — one instance across all scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildCanvas();
        // Start invisible
        canvasGroup.alpha = 0f;
    }

    void BuildCanvas()
    {
        // Create a full-screen canvas on top of everything
        var canvasGO  = new GameObject("_FadeCanvas");
        canvasGO.transform.SetParent(transform);
        var canvas        = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Full-screen image
        var imgGO = new GameObject("_FadeImage");
        imgGO.transform.SetParent(canvasGO.transform, false);
        var img = imgGO.AddComponent<Image>();
        img.color = fadeColor;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta  = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        canvasGroup = canvasGO.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false; // Don't block interaction while transparent
        canvasGroup.interactable   = false;
    }

    // --------------------------------------------------------
    //  Public API
    // --------------------------------------------------------

    /// <summary>Call this instead of SceneManager.LoadScene for fade transition.</summary>
    public void FadeAndLoad(string sceneName)
    {
        if (isFading) return;
        StartCoroutine(FadeAndLoadRoutine(sceneName));
    }

    /// <summary>Manually fade to black (without loading a scene).</summary>
    public void FadeOut(System.Action onComplete = null)
    {
        if (isFading) return;
        StartCoroutine(FadeRoutine(0f, 1f, fadeOutDuration, onComplete));
    }

    /// <summary>Manually fade from black to clear.</summary>
    public void FadeIn(System.Action onComplete = null)
    {
        StartCoroutine(FadeRoutine(1f, 0f, fadeInDuration, onComplete));
    }

    // --------------------------------------------------------
    //  Internal coroutines
    // --------------------------------------------------------

    IEnumerator FadeAndLoadRoutine(string sceneName)
    {
        isFading = true;

        // 1. Fade to black
        yield return FadeRoutine(0f, 1f, fadeOutDuration);

        // 2. Load scene (synchronously on next frame)
        SceneManager.LoadScene(sceneName);

        // 3. Wait one frame for new scene to initialise
        yield return null;
        yield return null;

        // 4. Fade from black
        yield return FadeRoutine(1f, 0f, fadeInDuration);

        isFading = false;
    }

    IEnumerator FadeRoutine(float fromAlpha, float toAlpha, float duration, System.Action onComplete = null)
    {
        canvasGroup.blocksRaycasts = (toAlpha > 0.5f); // block input during black screen
        canvasGroup.alpha = fromAlpha;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, timer / duration);
            yield return null;
        }
        canvasGroup.alpha = toAlpha;
        canvasGroup.blocksRaycasts = (toAlpha > 0.5f);
        onComplete?.Invoke();
    }
}
