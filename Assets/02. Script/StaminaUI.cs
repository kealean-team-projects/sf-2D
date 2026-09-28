using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

namespace _02._Script
{
    public class StaminaUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0f)] private float hideDelay = 2f;
        
        [SerializeField] private Image staminaFill;
        [SerializeField] private Image staminaWhiteFill;
        [SerializeField] private Canvas staminaCanvas;
        
        [SerializeField, Min(0.01f)] private float fillSpeed = 1f;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 2f, 0f);
        
        [SerializeField, Min(0f)] private float fillDelay = 0.25f;
        [SerializeField, Min(0.01f)] private float fillDuration = 0.5f;
        
        [SerializeField] private Color emptyColor = Color.red;
        
        [SerializeField, Min(0.01f)] private float fullFlashDuration = 0.25f;

        private Color normalFillColor;
        private Tween fullFlash;

        private float targetRatio;
        private bool initialized;
        private Transform followTarget;
        private Color normalTrailColor;
        
        private float delayRemaining;

        private RectTransform rectTransform;
        private Camera cam;

        private Tween whiteTween;
        
        private float followAt;
        private float hideAt = float.PositiveInfinity;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            cam = Camera.main;
            normalTrailColor = staminaWhiteFill.color;
            normalFillColor = staminaFill.color;
            canvasGroup.alpha = 0f;
        }
        
        private void OnDisable()
        {
            fullFlash.Stop();
            staminaFill.color = normalFillColor;
        }

        public void SetStamina(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            
            if (ratio < 1f)
                canvasGroup.alpha = 1f;

            hideAt = ratio >= 1f
                ? Time.time + hideDelay
                : float.PositiveInfinity;
            
            
            
            bool reachedFull = initialized
                               && staminaFill.fillAmount < 1f
                               && ratio >= 1f;

            if (reachedFull)
            {
                fullFlash.Stop();
                fullFlash = Tween.Custom(0f, 1f, fullFlashDuration,
                    t => staminaFill.color = Color.Lerp(Color.white, normalFillColor, t));
            }

            if (ratio < staminaFill.fillAmount &&
                Mathf.Approximately(staminaWhiteFill.fillAmount, staminaFill.fillAmount))
                followAt = Time.time + fillDelay;

            staminaFill.fillAmount = ratio;
            staminaWhiteFill.color = ratio <= 0f ? emptyColor : normalTrailColor;

            if (!initialized || ratio > staminaWhiteFill.fillAmount)
                staminaWhiteFill.fillAmount = ratio;

            initialized = true;
        }

        private void Update()
        {
            if (Time.time >= hideAt)
                canvasGroup.alpha = 0f;
            
            if (!initialized || Time.time < followAt) return;

            staminaWhiteFill.fillAmount = Mathf.MoveTowards(
                staminaWhiteFill.fillAmount,
                staminaFill.fillAmount,
                fillSpeed * Time.deltaTime);
        }
        
        public void SetTarget(Transform target) => followTarget = target;

        private void LateUpdate()
        {
            if (followTarget == null) return;
            if (cam == null) return;

            Vector3 screenPoint = cam.WorldToScreenPoint(
                followTarget.position + followOffset);

            screenPoint.z = 0f;
            transform.position = screenPoint;
        }
    }
}