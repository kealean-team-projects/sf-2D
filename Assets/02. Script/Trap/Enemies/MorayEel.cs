using System;
using _02._Script.Component;
using _02._Script.Players;
using UnityEngine;

namespace _02._Script.Trap.Enemies
{
    public class MorayEel : EnemyBase
    {   
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float wallCheckDistance = 0.6f;

        [SerializeField] private Transform startPos, endPos;
        
        private Vector2 _targetPosition;
        
        private int dir = 1;

        private void Start()
        {
            _targetPosition = endPos.position;
        }

        protected override void Move()
        {
            Vector2 target = dir == 1 ? endPos.position : startPos.position;
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