using System;
using _02._Script.Players.Interface;
using UnityEngine;

namespace _02._Script.Players.Components.ControllerCompo {
    public sealed class FacingController
        : MonoBehaviour, IAgentModule, IFacingController {
        private Transform _playerTransform;

        public Type Type => typeof(IFacingController);

        public void Initialize(Agent owner) {
            _playerTransform = owner.transform;
            IsFacingLeft = Mathf.Approximately(_playerTransform.eulerAngles.y, 180f);
        }

        public bool IsFacingLeft { get; private set; }

        public void UpdateFacing(float xMove) {
            if (xMove == 0f) return;

            var shouldFaceLeft = xMove < 0f;

            if (shouldFaceLeft == IsFacingLeft) return;

            IsFacingLeft = shouldFaceLeft;

            _playerTransform.eulerAngles = IsFacingLeft
                ? new Vector3(0f, 180f, 0f)
                : Vector3.zero;
        }
    }
}