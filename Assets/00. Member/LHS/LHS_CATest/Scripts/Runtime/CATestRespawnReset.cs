using _02._Script._05_Managers;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 밀기 퍼즐용 상자/바위를 플레이어가 죽었다 살아날 때 처음 위치로 되돌린다.
    /// (상자를 구덩이에 빠뜨려 퍼즐을 못 풀게 되는 상황 방지)
    /// </summary>
    public sealed class CATestRespawnReset : MonoBehaviour {
        private Vector3 _pos;
        private Quaternion _rot;
        private Rigidbody2D _rb;

        private void Awake() {
            _pos = transform.position;
            _rot = transform.rotation;
            _rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable() => GameManager.OnRespawnReset += OnRespawn;
        private void OnDisable() => GameManager.OnRespawnReset -= OnRespawn;

        private UniTask OnRespawn() {
            transform.SetPositionAndRotation(_pos, _rot);
            if (_rb != null) {
                _rb.position = _pos;
                _rb.linearVelocity = Vector2.zero;
                _rb.angularVelocity = 0f;
            }
            return UniTask.CompletedTask;
        }
    }
}
