using System;
using System.Collections.Generic;
using _02._Script._01_Players.FSM.MoveState;
using _02._Script._01_Players.Interface;
using UnityEngine;

namespace _02._Script._01_Players.FSM {
    public class MoveStateMachine {
        private const float RisingSpeedThreshold = 0.1f;
        private readonly Dictionary<Type, PlayerMoveState> _states = new();
        public PlayerMoveState CurrentState { get; private set; }
        public event Action<PlayerMoveState, PlayerMoveState> StateChanged;

        public void Tick() {
            CurrentState?.UpdateState();
        }


        public void AddState(PlayerMoveState state) {
            if (state == null) return;
            _states.Add(state.GetType(), state);
        }

        public void ChangeState<T>() where T : PlayerMoveState {
            if (_states.TryGetValue(typeof(T), out var nextState)) ChangeState(nextState);
        }

        public bool TryGetState<T>(out T state) where T : PlayerMoveState {
            if (_states.TryGetValue(typeof(T), out var s) && s is T typedState) {
                state = typedState;
                return true;
            }

            state = null;
            return false;
        }

        public void ChangeState(PlayerMoveState nextState) {
            if (nextState == null || CurrentState == nextState)
                return;

            var previousState = CurrentState;

            CurrentState?.Exit();

            CurrentState = nextState;
            CurrentState.Enter();

            StateChanged?.Invoke(previousState, CurrentState);
        }

        public void HandleJumpInput() {
            CurrentState?.HandleJumpInput();
        }

        public static bool IsRising(float verticalSpeed) {
            var verticalVelocity = new Vector2(0f, verticalSpeed);
            return verticalVelocity.magnitude > RisingSpeedThreshold && verticalVelocity.y > 0f;
        }

        public void ReturnToMovement(IPlayerMoveContext context) {
            if (context.IsClimb) {
                ChangeState<ClimbState>();
            }
            else if (!context.IsGrounded) {
                if (IsRising(context.VerticalSpeed))
                    ChangeState<JumpState>();
                else
                    ChangeState<AirState>();
            }
            else if (context.IsCrouching) {
                ChangeState<CrouchState>();
            }
            else if (context.MoveInput == 0f) {
                ChangeState<IdleState>();
            }
            else if (context.IsSprinting) {
                ChangeState<RunState>();
            }
            else {
                ChangeState<WalkState>();
            }
        }
    }
}