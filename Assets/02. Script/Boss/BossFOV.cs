using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace _02._Script.Boss
{
    public class BossFOV : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float viewDistance = 10f;
        [SerializeField, Range(0f, 360f)] private float viewAngle = 90f;
        [SerializeField] private Light2D scanLight;

        
        private void Awake() => Show(false);

        public bool CanSee(Transform target)
        {
            Vector2 direction = target.position - transform.position;

            if (direction.magnitude > viewDistance) return false;

            if (Vector2.Angle(transform.right, direction) > viewAngle * 0.5f)
                return false;

            return true;
        }
        
        
        public void Show(bool visible)
        {
            scanLight.enabled = visible;
        }
        
        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;

            Vector3 leftDirection =
                Quaternion.Euler(0f, 0f, viewAngle * 0.5f) * transform.right;

            Vector3 rightDirection =
                Quaternion.Euler(0f, 0f, -viewAngle * 0.5f) * transform.right;

            Gizmos.DrawWireSphere(transform.position, viewDistance);
            Gizmos.DrawRay(transform.position, leftDirection * viewDistance);
            Gizmos.DrawRay(transform.position, rightDirection * viewDistance);
        }
        #endif
    }
}
