using System;
using _01.Script.Player.Interface;
using UnityEngine;

namespace _01.Script.Player.Components {
    public class Mover : MonoBehaviour, IAgentModule, IMover {
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private float speed;
        [SerializeField] private float jumpForce;

        [Header("CheckGround")]
        [field: SerializeField]
        public bool IsGround { get; private set; }

        [SerializeField] private Vector3 checker;
        [SerializeField] private Vector2 checkerSize;
        [SerializeField] private LayerMask whatIsGround;

        [Header("ExtraGravity Settings")] [SerializeField]
        private float extraGravity = 15f;

        [SerializeField] private float gravityDelay = 0.15f;
        private bool _canDoubleJump;

        private float _moveInput;

        private float _timeInAir;

        private void Reset() {
            rb = transform.root.GetComponent<Rigidbody2D>();
        }

        private void Update() {
            CalculateAirTime();
        }

        private void FixedUpdate() {
            rb.linearVelocityX = _moveInput * speed;
            IsGround = CheckGround();
            ApplyExtraGravity();
        }


#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + checker, checkerSize);
            Gizmos.color = Color.yellow;
        }
#endif

        public Type Type => typeof(IMover);

        public void Initialize(Agent owner) { }

        public void SetMoveInput(float moveInput) {
            _moveInput = moveInput;
        }

        public void Jump(float multiplier = 1) {
            StopImmediately(false, true);
            rb.AddForceY(jumpForce * multiplier, ForceMode2D.Impulse);
        }

        private void CalculateAirTime() {
            if (!IsGround)
                _timeInAir += Time.deltaTime;
            else
                _timeInAir = 0;
        }


        private void ApplyExtraGravity() {
            if (_timeInAir > gravityDelay)
                rb.AddForceY(-extraGravity);
        }

        private bool CheckGround() {
            var col = Physics2D.OverlapBox(transform.position + checker, checkerSize, 0f, whatIsGround);
            return col != null;
        }

        private void StopImmediately(bool isXStop, bool isYStop) {
            if (isXStop)
                rb.linearVelocityX = 0;
            if (isYStop)
                rb.linearVelocityY = 0;
        }
    }
}