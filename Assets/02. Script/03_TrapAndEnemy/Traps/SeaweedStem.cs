using _02._Script._01_Players;
using _02._Script._01_Players.Components.DamageCompo;
using UnityEngine;

namespace _02._Script._03_TrapAndEnemy.Traps {
    public class SeaweedStem : TrapBase {
        private Player _player;
        private float _originalSpeed;
        private float _timer = 1f;

        [SerializeField] private float dieTime = 5f;
        
        private void Update() {
            if (_player == null) return;

            _timer += Time.deltaTime * 2f;
            _player.ChangeSpeed(_originalSpeed / _timer);

            if (_timer >= dieTime)
            {
                var target = _player;
                ReleasePlayer();
                
                if (target.TryGetComponent<DamageModule>(out var dmg))
                    dmg.TakeDamage();
            }
        }

        protected override void OnPlayerEnter(Player player)
        {
            if (_player != null) return;

            _player = player;
            player.ChangeSpeed(_originalSpeed);
            _timer = 1f;

            player.CanSJ = false;
        }

        protected override void OnPlayerExit(Player player)
        {
            if (_player == player)
                ReleasePlayer();
        }
        
        private void ReleasePlayer()
        {
            if (_player != null)
            {
                _player.CanSJ = true;
                _player.ChangeSpeed(_originalSpeed);
            }

            _player = null;
            _timer = 1f;
        }
    }
}