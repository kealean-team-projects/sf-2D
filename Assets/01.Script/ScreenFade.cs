using System;
using System.Collections;
using UnityEngine;

public class ScreenFade : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.25f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
    }

    public IEnumerator FadeOut()
    {
        yield return FadeCoroutine(0f, 1f);
    }

    public IEnumerator FadeIn()
    {
        yield return FadeCoroutine(1f, 0f);
    }


    private IEnumerator FadeCoroutine(float start, float end)
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(start, end, t);

            yield return null;
        }

        canvasGroup.alpha = end;
    }
}
