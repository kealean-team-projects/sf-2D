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
        [SerializeField] private LayerMask whatIsWall;
        
        private int dir = 1;
        
        protected override void Move()
        {
            var hit = Physics2D.Raycast(rb.position, Vector2.right * dir, wallCheckDistance, whatIsWall);

            if (hit.collider != null) dir *= -1;

            rb.linearVelocityX = moveSpeed * dir;
        }

        protected override void OnPlayerEnter(Player player)
        {
            DealDamage(player);
        }

        private void OnDrawGizmos()
        {
            if (rb == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, Vector2.right * dir * wallCheckDistance);
        }
    }
}