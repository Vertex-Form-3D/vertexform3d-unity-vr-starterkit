using UnityEngine;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;
    [SerializeField] private float fadeWaitTime = 1.0f; // Wait time between fade in and fade out
    [SerializeField] private Color fadeColor = Color.black; // Color used for the fade effect

    private Texture2D fadeTexture;
    private float alpha = 1.0f; // Current alpha value of the fade effect
    private int drawDepth = -1000; // Order in the draw hierarchy

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        fadeTexture = new Texture2D(1, 1);
        fadeTexture.SetPixel(0, 0, fadeColor);
        fadeTexture.Apply();
        ScreenFadeIn(0);
    }

    public void ScreenFadeIn(float fadeDuration)
    {
        StartCoroutine(FadeIn(fadeDuration));
    }

    public void ScreenFadeOut(float fadeDuration)
    {
        StartCoroutine(FadeOut(fadeDuration));
    }

    public void ScreenFadeInAndOut()
    {
        StartCoroutine(FadeInAndOut());
    }

    private IEnumerator FadeInAndOut()
    {
        yield return FadeOut(0.15f);
        yield return new WaitForSeconds(fadeWaitTime);
        yield return FadeIn(0.15f);
    }

    private IEnumerator FadeIn(float fadeDuration)
    {
        alpha = 1.0f;

        float timer = 0.0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            alpha = 1.0f - (timer / fadeDuration);

            yield return null;
        }

        alpha = 0.0f;
    }

    private IEnumerator FadeOut(float fadeDuration)
    {
        alpha = 0.0f;

        float timer = 0.0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            alpha = timer / fadeDuration;

            yield return null;
        }

        alpha = 1.0f;
    }

    private void OnGUI()
    {
        if (fadeTexture == null) return;

        GUI.depth = drawDepth;
        GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, alpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeTexture);
    }

    private void OnDestroy()
    {
        if (fadeTexture != null)
        {
            Destroy(fadeTexture);
        }
    }
}