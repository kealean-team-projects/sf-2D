using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script {
    public class ScreenFade : MonoBehaviour {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeDuration = 0.25f;

        private void Awake() {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
        }

        public IEnumerator FadeOut() {
            yield return FadeCoroutine(0f, 1f);
        }

        public IEnumerator FadeIn() {
            yield return FadeCoroutine(1f, 0f);
        }


        private async UniTaskVoid FadeCoroutine(float start, float end) {
            var elapsedTime = 0f;

            while (elapsedTime < fadeDuration) {
                elapsedTime += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsedTime / fadeDuration);
                canvasGroup.alpha = Mathf.Lerp(start, end, t);

                await UniTask.Yield();
            }

            canvasGroup.alpha = end;
        }
    }
}