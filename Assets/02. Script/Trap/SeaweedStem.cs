using _02._Script.Players;
using UnityEngine;

namespace _02._Script.Trap {
    public class SeaweedStem : MonoBehaviour {
        [SerializeField] private float speed;
        private bool _isSeaweed;
        private Player _player;
        private float _timer = 1f;

        private void Update() {
            if (_isSeaweed) {
                _timer += Time.deltaTime * 2;
                _player.ChangeSpeed(speed / _timer);
                if (_timer >= 5f) {
                    Destroy(_player.gameObject);
                    _player = null;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other) {
            if (other.gameObject.TryGetComponent(out _player)) {
                Debug.Log("이건 늪이다");
                _isSeaweed = true;
                _player.CanJump = false;
            }
        }

        private void OnTriggerExit2D(Collider2D other) {
            _timer = 1f;
            _isSeaweed = false;
            _player.CanJump = true;
            _player.ChangeSpeed(speed);
            _player = null;
        }
    }
}