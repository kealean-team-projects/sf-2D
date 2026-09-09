using System;
using _02._Script.Player.Interface;
using UnityEngine;

namespace _02._Script.Player.Components.ControllerCompo
{
    public sealed class CrouchController : MonoBehaviour, IAgentModule, ICrouchController
    {
        [SerializeField] private CapsuleCollider2D targetCollider;

        [SerializeField]
        private Vector2 crouchingSize = new(1f, 1f);

        [SerializeField]
        private Vector2 crouchingOffset = new(0f, -0.5f);

        [SerializeField, Range(0f, 1f)]
        private float crouchingSpeedMultiplier = 0.5f;

        private Vector2 _standingSize;
        private Vector2 _standingOffset;
        private bool _isCrouching;

        public Type Type => typeof(ICrouchController);

        public float MoveSpeedMultiplier =>
            _isCrouching ? crouchingSpeedMultiplier : 1f;

        public void Initialize(Agent owner)
        {
            if (targetCollider == null)
                targetCollider = owner.GetComponent<CapsuleCollider2D>();

            _standingSize = targetCollider.size;
            _standingOffset = targetCollider.offset;
        }

        public void Crouch()
        {
            targetCollider.size = crouchingSize;
            targetCollider.offset = crouchingOffset;
            _isCrouching = true;
        }

        public void Stand()
        {
            targetCollider.size = _standingSize;
            targetCollider.offset = _standingOffset;
            _isCrouching = false;
        }

        private void Reset()
        {
            targetCollider = transform.root.GetComponent<CapsuleCollider2D>();
        }
    }
}