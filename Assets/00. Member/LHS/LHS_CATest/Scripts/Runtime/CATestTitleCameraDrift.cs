using UnityEngine;

namespace LHS_CATest {
    /// <summary>타이틀 배경 카메라를 아주 천천히 좌우/상하로 흔들어(떠다니는 느낌) 멈춘 그림처럼 보이지 않게 한다.</summary>
    public sealed class CATestTitleCameraDrift : MonoBehaviour {
        [SerializeField] private Vector2 amplitude = new(3f, 0.8f);
        [SerializeField] private float speed = 0.05f;
        private Vector3 _base;

        private void Awake() => _base = transform.position;

        private void LateUpdate() {
            var t = Time.unscaledTime * speed * Mathf.PI * 2f;
            transform.position = _base + new Vector3(Mathf.Sin(t) * amplitude.x, Mathf.Sin(t * 1.7f + 1f) * amplitude.y, 0f);
        }
    }
}
