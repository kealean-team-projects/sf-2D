using System;
using UnityEngine;

namespace _01.Script.Player.Components {
    public class Mover : MonoBehaviour, IAgentModule, IMover {
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private float speed;
        [SerializeField] private float jumpForce;

        private float _moveInput;

        public Type Type => typeof(IMover);

        public void Initialize(Agent owner) { }

        private void FixedUpdate() {
            rb.linearVelocityX = _moveInput * speed;
        }

        private void Reset() {
            rb = transform.root.GetComponent<Rigidbody2D>();
        }

        public void SetMoveInput(float moveInput) {
            _moveInput = moveInput;
        }

        public void Jump(float multiplier = 1) {
            rb.AddForceY(jumpForce * multiplier, ForceMode2D.Impulse);
        }
    }
}