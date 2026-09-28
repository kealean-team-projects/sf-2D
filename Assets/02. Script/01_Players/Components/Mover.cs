using System;
using _02._Script._01_Players.Interface;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script._01_Players.Components {
    public class Mover : MonoBehaviour, IAgentModule, IMover {
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private float jumpForce;
        [SerializeField] private Vector3 checker;
        [SerializeField] private Vector2 checkerSize;
        [SerializeField] private LayerMask whatIsGround;

        [Header("ExtraGravity Settings")] [SerializeField]
        private float extraGravity = 15f;

        [SerializeField] private float gravityDelay = 0.15f;

        private bool _isClimbCancelPending;
        private bool _isWallDashing;

        private float _originGravityScale;

        private int _restoreVersion;
        private float _timeInAir;

        public event Action GroundUpdated;
        public int RestoreVersion => _restoreVersion;

        // Audio observes the supporting surface without changing movement's ground check.
        public Collider2D GroundCollider { get; private set; }
        public Vector2 SoundSurfacePoint { get; private set; }
        private readonly ContactPoint2D[] _groundContacts = new ContactPoint2D[16];
        private readonly RaycastHit2D[] _surfaceHits = new RaycastHit2D[8];

        private void Awake() {
            rb = transform.root.GetComponent<Rigidbody2D>();
            _originGravityScale = rb.gravityScale;
            _body = rb.GetComponent<CapsuleCollider2D>();
        }

        // 공중에서 (등반 불가) 벽 쪽으로 방향키를 누르고 있으면, 벽에 계속 몸을 밀어붙이는 힘 때문에
        // 마찰력(기본 0.4)이 생겨 떨어지지 않고 벽에 붙어 버린다. → 공중 + 그 방향이 막힌 가파른 벽이면 수평 속도를 0으로.
        // (마찰 0 재질로 바꾸면 경사면에 가만히 서 있을 때 미끄러지므로 이 방식을 사용)
        private CapsuleCollider2D _body;
        private readonly RaycastHit2D[] _wallHits = new RaycastHit2D[4];

        private bool IsPressingIntoWall(float dir) {
            if (_body == null) return false;
            var filter = new ContactFilter2D();
            filter.SetLayerMask(whatIsGround);
            filter.useTriggers = false;
            var count = _body.Cast(new Vector2(dir, 0f), filter, _wallHits, 0.04f);
            for (var i = 0; i < count; i++) {
                var n = _wallHits[i].normal;
                if (Mathf.Abs(n.y) < 0.5f && n.x * dir < -0.5f) return true;
            }
            return false;
        }

        private void FixedUpdate()
        {
            GroundCollider = FindSupportingSurface();
            IsGround = GroundCollider != null;

            GroundUpdated?.Invoke();
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

        [Header("CheckGround Settings")]
        [field: SerializeField]
        public bool IsGround { get; private set; }

        public bool CanClimb => !_isWallDashing && !_isClimbCancelPending;
        public float VerticalSpeed => rb.linearVelocityY;

        public void RestorePosition(Vector2 position) {
            _restoreVersion++;

            _isClimbCancelPending = false;
            _isWallDashing = false;
            _timeInAir = 0f;

            rb.position = position;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = _originGravityScale;

            Physics2D.SyncTransforms();
            GroundCollider = FindSupportingSurface();
            IsGround = GroundCollider != null;
        }

        #region Apply_States

        public void ApplyManualMove(float moveSpeed) {
            rb.gravityScale = _originGravityScale;
            if (!IsGround && moveSpeed != 0f && IsPressingIntoWall(Mathf.Sign(moveSpeed))) moveSpeed = 0f;
            rb.linearVelocityX = moveSpeed;
            ApplyExtraGravity();
        }

        public void ApplyManualMoveY(float moveSpeed) {
            if (Mathf.Approximately(moveSpeed, 0f)) return;
            // Preserve faster jumps/falls in the current's direction without accumulating speed.
            rb.linearVelocityY = moveSpeed > 0f
                ? Mathf.Max(rb.linearVelocityY, moveSpeed)
                : Mathf.Min(rb.linearVelocityY, moveSpeed);
        }

        public void Jump(float multiplier = 1) {
            _timeInAir = 0;
            StopImmediately(false, true);
            rb.AddForceY(jumpForce * multiplier, ForceMode2D.Impulse);
        }

        public void ApplyWallDash(float impulse) {
            rb.AddForceY(impulse, ForceMode2D.Impulse);
        }

        // 벽 등반 대쉬: 대쉬 동안 중력을 끄고 일정한 속도로 벽을 따라 올라간다.
        // (기존 ApplyWallDash는 충격량 3을 더하는 방식이라 질량 1 기준 속도가 3밖에 늘지 않았다)
        public void ApplyWallDashVelocity(float speed) {
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(0f, speed);
        }

        // 대쉬 종료 시 남은 상승 속도를 제한하고 중력을 되돌린다.
        public void ClampRiseSpeed(float maxRiseSpeed) {
            rb.gravityScale = _originGravityScale;
            if (rb.linearVelocityY > maxRiseSpeed) rb.linearVelocityY = maxRiseSpeed;
        }

        public void ApplyWallJump(Vector2 walljumpDir) {
            _timeInAir = 0f;
            rb.gravityScale = _originGravityScale;
            rb.linearVelocity = walljumpDir;
        }

        public void ApplyClimb(float climbSpeed) {
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(0f, climbSpeed);
        }

        #region WallDash Control

        public void WallDash() {
            _isWallDashing = true;
        }

        public void EndWallDash() {
            _isWallDashing = false;
        }

        #endregion

        #region Climb Control

        public void CancelClimb() {
            if (_isClimbCancelPending) return;

            _isClimbCancelPending = true;
            CancelClimbUniTask().Forget();
        }

        private async UniTaskVoid CancelClimbUniTask() {
            var version = _restoreVersion;

            await UniTask.Delay(TimeSpan.FromSeconds(2f));

            if (version != _restoreVersion) return;

            _isClimbCancelPending = false;
        }

        #endregion

        #endregion

        #region Rigidbody


        public Collider2D FindSoundSurface(float extraDistance) {
            // Only extend the sound query; the movement ground check is unchanged.
            return extraDistance > 0f ? FindSupportingSurface(extraDistance) : GroundCollider;
        }

        private Collider2D FindSupportingSurface(float extraDistance = 0f) {
            var filter = new ContactFilter2D();
            filter.SetLayerMask(whatIsGround);
            filter.useTriggers = false;
            var center = (Vector2)(transform.position + checker);
            var origin = center + Vector2.up * (checkerSize.y * 0.5f + 0.05f);
            var distance = checkerSize.y + 0.1f + Mathf.Max(0f, extraDistance);
            // Prefer the surface directly under the feet, then either edge on a ledge.
            for (var sample = 0; sample < 3; sample++) {
                var offset = sample == 0 ? 0f : checkerSize.x * (sample == 1 ? -0.4f : 0.4f);
                var count = Physics2D.Raycast(origin + Vector2.right * offset, Vector2.down,
                    filter, _surfaceHits, distance);
                Collider2D closest = null;
                var closestDistance = float.PositiveInfinity;
                for (var i = 0; i < count; i++) {
                    var hit = _surfaceHits[i];
                    if (hit.fraction == 0f || hit.normal.y < 0.35f || hit.distance >= closestDistance)
                        continue;
                    closest = hit.collider;
                    closestDistance = hit.distance;
                }
                if (closest != null) return closest;
            }
            return null;
        }

        public void CalculateAirTime(bool isClimbing) {
            if (isClimbing) return;
            if (!IsGround)
                _timeInAir += Time.deltaTime;
            else
                _timeInAir = 0;
        }

        private void ApplyExtraGravity() {
            if (_timeInAir > gravityDelay)
                rb.AddForceY(-extraGravity);
        }

        private void StopImmediately(bool isXStop, bool isYStop) {
            if (isXStop)
                rb.linearVelocityX = 0;
            if (isYStop)
                rb.linearVelocityY = 0;
        }

        #endregion
    }
}
