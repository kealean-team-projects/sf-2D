using _02._Script._01_Players;
using UnityEngine;

namespace _02._Script._03_TrapAndEnemy.Enemies
{
    public class ElectronicEnemy : EnemyBase
    {
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float moveDistance;
        
        private Vector2 _targetPosition;
        private Vector2 _startPos;
        private Vector2 _endPos;
        
        private int dir = 1;

        private void Awake() {
            _startPos = rb.position;
            _endPos = _startPos + new Vector2(moveDistance, 0);
        }

        protected override void Move()
        {
            Vector2 target = dir == 1 ? _endPos : _startPos;
            Vector2 offset = target - rb.position;

            rb.linearVelocity = Vector2.ClampMagnitude(offset / Time.fixedDeltaTime, moveSpeed);
            if (offset.sqrMagnitude < 0.0001f) dir *= -1;
        }

        protected override void OnPlayerEnter(Player player)
        {
            DealDamage(player);
        }
    }
}