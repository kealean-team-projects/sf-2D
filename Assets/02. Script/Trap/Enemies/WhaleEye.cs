using _02._Script.Players;
using UnityEngine;

namespace _02._Script.Trap.Enemies
{
    public class WhaleEye : EnemyBase
    {
        [SerializeField] private float eyeOpenTime = 2f, eyeOpeningTime = 3f, eyeClosedTime = 1f;
        private enum EyeState { Closed, Opening, Open}
        
        private EyeState _state = EyeState.Closed;
        private Player _player;
        
        private float _timer;

        private void Update()
        {
            _timer += Time.deltaTime;

            switch (_state)
            {
                case EyeState.Closed:
                    if (_timer >= eyeClosedTime)
                        ChangeState(EyeState.Opening);
                    break;

                case EyeState.Opening:
                    if (_timer >= eyeOpeningTime)
                        ChangeState(EyeState.Open);
                    break;

                case EyeState.Open:
                    if (_timer >= eyeOpenTime)
                        ChangeState(EyeState.Closed);
                    break;
            }
        }

        protected override void OnPlayerEnter(Player player)
        {
            _player = player;
            HandleState(player);
        }

        protected override void OnPlayerStay(Player player)
        {
            _player = player;
            HandleState(player);
        }
        
        private void HandleState(Player player)
        {
            switch (_state)
            {
                case EyeState.Closed:
                    break;
                case EyeState.Opening:
                    break;
                case EyeState.Open:
                    if (player.IsMoving || !player.IsGrounded)
                        DealDamage(player);
                    break;
            }
        }

        protected override void OnPlayerExit(Player player)
        {
            if (_player == player)
                _player = null;
        }
        
        private void ChangeState(EyeState nextState)
        {
            if (_state == nextState) return;

            _state = nextState;
            _timer = 0f;
            Debug.Log($"State가 {_state}로 바뀌었습니다.");
        }
    }
}