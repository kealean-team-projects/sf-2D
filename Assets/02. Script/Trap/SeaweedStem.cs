using System;
using UnityEngine;

namespace _02._Script.Trap {
    public class SeaweedStem : MonoBehaviour {
        private Player.Player _player;
        private float _timer = 1f;
        [SerializeField] private float speed;
        private bool _isSeaweed;
        
        private void OnTriggerEnter2D(Collider2D other) {
            if (other.gameObject.TryGetComponent<Player.Player>(out _player)) {
                Debug.Log("이건 늪이다");
                _isSeaweed = true;
            }
        }

        private void Update() {
            if (_isSeaweed) {
                _timer += Time.deltaTime * 2;
                _player.ChangeSpeed(speed/_timer);
                if (_timer >= 5f) {
                    Destroy(_player.gameObject);
                    _player = null;
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other) {
            _timer = 1f;
            _isSeaweed = false;
            _player.ChangeSpeed(speed);
            _player = null;
        }
    }
}