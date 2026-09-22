using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace _02._Script.Boss {
    public class BossFOV : MonoBehaviour {
        [SerializeField] [Min(0f)] private float viewDistance = 10f;
        [SerializeField] [Range(0f, 360f)] private float viewAngle = 90f;
        [SerializeField] private Light2D scanLight;

        private Tween _angleTween;

        private void Awake() {
            Show(false);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.yellow;

            var leftDirection =
                Quaternion.Euler(0f, 0f, viewAngle * 0.5f) * transform.right;

            var rightDirection =
                Quaternion.Euler(0f, 0f, -viewAngle * 0.5f) * transform.right;

            Gizmos.DrawWireSphere(transform.position, viewDistance);
            Gizmos.DrawRay(transform.position, leftDirection * viewDistance);
            Gizmos.DrawRay(transform.position, rightDirection * viewDistance);
        }
#endif

        public bool CanSee(Transform target) {
            Vector2 direction = target.position - transform.position;

            if (direction.magnitude > viewDistance) return false;

            return !(Vector2.Angle(transform.right, direction) > viewAngle * 0.5f);
        }


        public void Show(bool visible) {
            scanLight.enabled = visible;
            _angleTween = visible
                ? Tween.Custom(0f, 90f, 1.5f, value => scanLight.pointLightInnerAngle = value, Ease.InExpo)
                : Tween.Custom(90f, 0f, 0.75f, value => scanLight.pointLightInnerAngle = value, Ease.InExpo);
        }
    }
}