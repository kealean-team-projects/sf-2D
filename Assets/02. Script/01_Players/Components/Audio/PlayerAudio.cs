using csiimnida.CSILib.SoundManager.RunTime;
using UnityEngine;

namespace _02._Script._01_Players.Components.Audio {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Player))]
    public sealed class PlayerAudio : MonoBehaviour {
        [SerializeField] private Player player;
        [SerializeField] private Mover mover;
        [SerializeField] private Rigidbody2D body;
        [Header("Footsteps")]
        [SerializeField, Min(0.01f)] private float minimumMoveSpeed = 0.1f;
        [Tooltip("걷기·달리기 소리 전용으로 발밑 감지 범위를 아래로 늘리는 거리(Unity 단위). 0이면 기존 접지 판정을 사용합니다.")]
        [SerializeField, Min(0f)] private float footstepDetectionDistance = 0.1f;
        [Tooltip("좌우 전환 등으로 잠깐 멈춘 뒤 같은 발소리를 이어 재생할 수 있는 시간(초). 0이면 매번 처음부터 재생합니다.")]
        [SerializeField, Min(0f)] private float footstepResumeWindow = 0.12f;
        [Header("Landing")]
        [Tooltip("착지음에 필요한 최소 연속 추락 시간(초). 상승·등반 시간은 제외하며, 0이면 모든 착지에서 재생합니다.")]
        [SerializeField, Min(0f)] private float minimumFallTime = 0.08f;
        private string walkSound = "Walk";
        private string runSound = "Run";
        private string landingSound = "Landing";
        
        private string stoneWalkSound = "Walk_2";
        private string stoneRunSound = "Run_2";
        private string stoneLandingSound = "Landing_2";

        private SoundManager _manager;
        private AudioSource _footsteps;
        private AudioSource _landing;
        private bool _footstepsPaused;
        private float _footstepsPausedAt;
        private string _currentSound;
        private bool _sampled;
        private bool _wasGrounded;
        private int _restoreVersion;
        private Vector2 _lastPosition;
        private float _fallTime;
        private bool _landingPlayed;

        private void Awake() {
            if (player == null) player = GetComponent<Player>();
            if (mover == null) mover = GetComponentInChildren<Mover>();
            if (body == null) body = GetComponent<Rigidbody2D>();
        }

        private void OnEnable() {
            _sampled = false;
            _fallTime = 0f;
            _landingPlayed = false;
            if (mover != null) mover.GroundUpdated += OnGroundUpdated;
        }

        private void OnDisable() {
            if (mover != null) mover.GroundUpdated -= OnGroundUpdated;
            StopMovementSounds();
            _sampled = false;
            _fallTime = 0f;
            _landingPlayed = false;
        }

        private void LateUpdate() {
            // Physics stops while paused; silence loops without waiting for another physics tick.
            if (player == null || player.IsDead || !player.CanMove)
                StopMovementSounds();
            else if (Time.timeScale == 0f)
                StopFootsteps();
            else if (_footstepsPaused && Time.unscaledTime - _footstepsPausedAt >= footstepResumeWindow)
                StopFootsteps();
        }

        private void OnGroundUpdated() {
            if (player == null || body == null || player.Mover == null || player.SprintControl == null)
                return;

            var grounded = mover.IsGround && mover.GroundCollider != null;
            var position = body.position;
            if (!_sampled || _restoreVersion != mover.RestoreVersion || player.IsDead) {
                _sampled = !player.IsDead;
                _restoreVersion = mover.RestoreVersion;
                _wasGrounded = grounded;
                _lastPosition = position;
                _fallTime = 0f;
                _landingPlayed = false;
                StopMovementSounds();
                return;
            }

            var speed = Mathf.Abs(position.x - _lastPosition.x) / Time.fixedDeltaTime;
            var rising = position.y > _lastPosition.y + 0.0001f || body.linearVelocity.y > 0.05f;
            var falling = !player.IsClimb && position.y < _lastPosition.y - 0.0001f;
            // Count completed falling physics steps, including the final step onto the ground.
            // Displacement still captures that last step after collision has zeroed velocity.
            if (!_wasGrounded && falling)
                _fallTime += Time.fixedDeltaTime;
            else if (!grounded)
                _fallTime = 0f;
            if (!grounded && (player.IsClimb || position.y > _lastPosition.y + 0.0001f))
                _landingPlayed = false;
            _lastPosition = position;
            var footstepGround = grounded ? mover.GroundCollider
                : !rising && !player.IsClimb && footstepDetectionDistance > 0f
                    ? mover.FindSoundSurface(footstepDetectionDistance) : null;
            var surface = footstepGround != null
                ? footstepGround.GetComponentInParent<FootstepSurface>() : null;
            var stone = surface != null && surface.Material == FootstepMaterial.Stone;

            if (_manager == null) _manager = FindAnyObjectByType<SoundManager>();
            // Play on the airborne -> grounded transition, never from a distant surface query.
            if (grounded && !_wasGrounded && !_landingPlayed && _fallTime >= minimumFallTime && _manager != null) {
                var landingSurface = mover.GroundCollider;
                if (landingSurface != null) {
                    var material = landingSurface.GetComponentInParent<FootstepSurface>();
                    var landingOnStone = material != null && material.Material == FootstepMaterial.Stone;
                    StopMovementSounds();
                    _landing = _manager.PlayTrackedSound(landingOnStone ? stoneLandingSound : landingSound);
                    _landingPlayed = true;
                }
            }
            if (grounded) {
                _fallTime = 0f;
                _landingPlayed = false;
            }
            _wasGrounded = grounded;

            // Landing and footsteps share one audible movement voice per player.
            if ((_landing != null && (_landing.isPlaying || AudioListener.pause)) ||
                footstepGround == null || !player.CanMove || player.IsClimb) {
                StopFootsteps();
                return;
            }
            if (!player.IsMoving || speed < minimumMoveSpeed) {
                PauseFootsteps();
                return;
            }

            var sound = player.SprintControl.IsSprinting
                ? (stone ? stoneRunSound : runSound)
                : (stone ? stoneWalkSound : walkSound);
            if (_currentSound == sound && _footsteps != null) {
                if (!_footstepsPaused) return;
                if (Time.unscaledTime - _footstepsPausedAt < footstepResumeWindow) {
                    _footsteps.UnPause();
                    _footstepsPaused = false;
                    return;
                }
            }
            StopFootsteps();
            if (_manager == null) return;
            _footsteps = _manager.PlayTrackedSound(sound);
            _currentSound = sound;
        }

        private void StopFootsteps() {
            if (_manager != null && _footsteps != null) _manager.StopSound(_footsteps);
            _footsteps = null;
            _currentSound = null;
            _footstepsPaused = false;
        }

        private void PauseFootsteps() {
            if (_footsteps == null) return;
            if (footstepResumeWindow <= 0f ||
                (_footstepsPaused && Time.unscaledTime - _footstepsPausedAt >= footstepResumeWindow)) {
                StopFootsteps();
                return;
            }
            if (_footstepsPaused) return;
            _footsteps.Pause();
            _footstepsPaused = true;
            _footstepsPausedAt = Time.unscaledTime;
        }

        private void StopMovementSounds() {
            StopFootsteps();
            if (_manager != null && _landing != null) _manager.StopSound(_landing);
            _landing = null;
        }
    }
}
