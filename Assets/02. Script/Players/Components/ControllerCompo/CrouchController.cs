using System;
using _02._Script.Players.Interface;
using UnityEngine;

namespace _02._Script.Players.Components.ControllerCompo {
    public sealed class CrouchController : MonoBehaviour, IAgentModule, ICrouchController {
        [SerializeField] private CapsuleCollider2D targetCollider;

        [SerializeField] private Vector2 crouchingSize = new(1f, 1f);

        [SerializeField] private Vector2 crouchingOffset = new(0f, -0.5f);

        [SerializeField] [Range(0f, 1f)] private float crouchingSpeedMultiplier = 0.5f;

        private bool _isCrouching;
        private Vector2 _standingOffset;

        private Vector2 _standingSize;

        private void Reset() {
            targetCollider = transform.root.GetComponent<CapsuleCollider2D>();
        }

        public Type Type => typeof(ICrouchController);

        public void Initialize(Agent owner) {
            if (targetCollider == null)
                targetCollider = owner.GetComponent<CapsuleCollider2D>();

            _standingSize = targetCollider.size;
            _standingOffset = targetCollider.offset;
        }

        public float MoveSpeedMultiplier =>
            _isCrouching ? crouchingSpeedMultiplier : 1f;

        public void Crouch() {
            targetCollider.size = crouchingSize;
            targetCollider.offset = crouchingOffset;
            _isCrouching = true;
        }

        public void Stand() {
            targetCollider.size = _standingSize;
            targetCollider.offset = _standingOffset;
            _isCrouching = false;
        }
    }
}