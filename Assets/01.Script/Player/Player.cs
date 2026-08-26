using System;
using _01.Script.Player.Components;
using UnityEngine;

namespace _01.Script.Player {
    public class Player : Agent
    {
        private IInputReader _inputReader;
        private IMover  _mover;

        private void FixedUpdate() {
            _mover.SetMoveInput(_inputReader.MoveInput);
        }

        protected override void Afterinitialize() {
            base.Afterinitialize();
            _inputReader = GetModule<IInputReader>();
            _mover = GetModule<IMover>();
            _inputReader.OnJumpPressed += HandleJumpInput;
        }

        private void HandleJumpInput() {
            _mover.Jump();
        }

        protected override void OnDispose() {
            base.OnDispose();
            _inputReader.OnJumpPressed -= HandleJumpInput;
        
        }
    }
}
