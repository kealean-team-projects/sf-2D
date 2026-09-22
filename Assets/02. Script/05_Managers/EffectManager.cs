using BalioProductions.CameraFilterPack.URP;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace _02._Script._05_Managers {
    public class EffectManager : MonoBehaviour {
        [SerializeField] private Image fade;
        [SerializeField] private float duration = 1f;
        [SerializeField] private Volume filterVolume;
        public static EffectManager Instance { get; private set; }

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            var color = fade.color;
            color.a = 0f;
            fade.color = color;
            fade.raycastTarget = false;
        }

        private void OnDestroy() {
            if (Instance == this) Instance = null;
        }

        public void setFilter(bool on) {
            if (filterVolume.profile.TryGet(out CameraFilterPackVolume_Blur_Focus focus)) focus.active = on;

            if (filterVolume.profile.TryGet(out MotionBlur motionBlur)) motionBlur.active = on;

            if (filterVolume.profile.TryGet(out CameraFilterPackVolume_Distortion_Wave wave)) wave.active = on;
        }

        public async UniTask ShowDeathEffect() {
            await Tween.Alpha(
                fade, fade.color.a, 1f, duration,
                Ease.InExpo, useUnscaledTime: true);
        }

        public async UniTask HideDeathEffect() {
            await Tween.Alpha(
                fade, fade.color.a, 0f, duration,
                Ease.OutExpo, useUnscaledTime: true);
        }
    }
}