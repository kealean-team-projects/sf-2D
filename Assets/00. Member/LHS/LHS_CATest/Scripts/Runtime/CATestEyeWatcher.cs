using System.Collections.Generic;
using _02._Script._01_Players;
using _02._Script._01_Players.Components.DamageCompo;
using _02._Script._05_Managers;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LHS_CATest {
    /// <summary>
    /// 심해 보스 "심연의 눈".
    ///
    /// 연출 구조 (3D처럼 멀리서 내려다보는 느낌)
    ///  - eyeRoot(몸통/눈알/홍채/눈꺼풀)는 z = +60~80, y = 위쪽에 크게 배치한다.
    ///    원근 카메라라서 z가 멀수록 작고 천천히 움직여 "저 멀리 뒤쪽 위"에 있는 것처럼 보인다.
    ///  - 실제 판정은 플레이 평면(z = 0)에서 한다. scanOrigin(평면 위 한 점)에서 부채꼴 시야가 좌우로 훑는다.
    ///  - 빛줄기 메쉬(beam)는 꼭짓점 하나를 눈알의 3D 위치에, 나머지를 평면 위 부채꼴 끝점에 둬서
    ///    "멀리 있는 눈에서 뿜어져 나온 빛이 바닥을 비추는" 원뿔처럼 보이게 한다.
    ///  - 부채꼴 끝점은 Raycast로 지형(엄폐물)에 닿는 곳에서 잘리므로, 빛이 가려진 곳 = 안전한 곳이 눈에 보인다.
    ///
    /// 판정
    ///  - 플레이어가 부채꼴 안 + 지형에 가려지지 않음 + CATestCover(해초 덤불에서 웅크림)에 숨지 않음 → 발각 게이지 증가
    ///  - 게이지가 차면 Alert: 빛이 붉어지고 플레이어를 추적, lockTime 뒤 사망 처리(DamageModule.TakeDamage)
    ///  - 도중에 엄폐하면 게이지가 줄어들며 다시 수색으로 돌아간다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class CATestEyeWatcher : MonoBehaviour {
        private enum State { Dormant, Waking, Scanning, Suspicious, Alert, Cooldown }

        [Header("Visual (먼 뒤쪽에 놓인 눈)")]
        [SerializeField] private Transform eyeRoot;
        [SerializeField] private Transform eyeball;
        [SerializeField] private Transform iris;
        [SerializeField] private Transform lidTop;
        [SerializeField] private Transform lidBottom;
        [SerializeField] private float irisTravel = 0.9f;
        [SerializeField] private Light2D eyeGlow;

        [Header("Scan (플레이 평면 z=0)")]
        [SerializeField] private Transform scanOrigin;
        [SerializeField] private Light2D beamLight;
        [SerializeField] private MeshFilter beamMesh;
        [SerializeField] private LayerMask coverMask = 1 << 6;
        [SerializeField] private float viewDistance = 42f;
        [SerializeField, Range(4f, 80f)] private float viewAngle = 24f;
        [SerializeField] private float sweepMin = -48f;
        [SerializeField] private float sweepMax = 48f;
        [SerializeField] private float sweepSpeed = 13f;
        [SerializeField] private float edgePause = 1.1f;
        [SerializeField, Min(3)] private int rays = 28;

        [Header("Detection")]
        [SerializeField] private float detectTime = 0.75f;
        [SerializeField] private float forgetSpeed = 0.8f;
        [SerializeField] private float lockTime = 0.9f;
        [SerializeField] private Color calmColor = new(0.55f, 0.95f, 1f, 1f);
        [SerializeField] private Color suspiciousColor = new(1f, 0.85f, 0.35f, 1f);
        [SerializeField] private Color alertColor = new(1f, 0.18f, 0.12f, 1f);
        [SerializeField] private float beamAlpha = 0.16f;

        private State _state = State.Dormant;
        private readonly HashSet<Collider2D> _inside = new();
        private Player _player;
        private float _angle;
        private float _dir = 1f;
        private float _pause;
        private float _suspicion;
        private float _stateTime;
        private float _open;
        private Vector2 _lastSeen;
        private Mesh _mesh;
        private Vector3[] _verts;
        private Color[] _cols;
        private int[] _tris;
        private Vector3 _eyeBase;

        private void Awake() {
            GetComponent<BoxCollider2D>().isTrigger = true;
            _angle = sweepMin;
            if (eyeRoot != null) _eyeBase = eyeRoot.position;
            BuildMesh();
            ApplyVisual(0f);
        }

        private void OnEnable() => GameManager.OnRespawnReset += ResetForRetry;
        private void OnDisable() => GameManager.OnRespawnReset -= ResetForRetry;

        private void OnTriggerEnter2D(Collider2D other) {
            var p = other.GetComponentInParent<Player>();
            if (p == null) return;
            _inside.Add(other);
            _player = p;
            if (_state == State.Dormant) SetState(State.Waking);
        }

        private void OnTriggerExit2D(Collider2D other) {
            if (!_inside.Remove(other) || _inside.Count > 0) return;
            _player = null;
            if (_state != State.Cooldown) SetState(State.Dormant);
        }

        private UniTask ResetForRetry() {
            _suspicion = 0f;
            _angle = sweepMin;
            _dir = 1f;
            _inside.Clear();
            _player = null;
            SetState(State.Dormant);
            _open = 0f;
            return UniTask.CompletedTask;
        }

        private void SetState(State s) {
            _state = s;
            _stateTime = 0f;
        }

        private void Update() {
            _stateTime += Time.deltaTime;
            var targetOpen = _state == State.Dormant ? 0f : (_state == State.Alert ? 1.15f : 1f);
            _open = Mathf.MoveTowards(_open, targetOpen, Time.deltaTime * (_state == State.Waking ? 0.7f : 1.6f));

            switch (_state) {
                case State.Dormant:
                    _suspicion = Mathf.MoveTowards(_suspicion, 0f, Time.deltaTime);
                    break;
                case State.Waking:
                    if (_open >= 0.99f) SetState(State.Scanning);
                    break;
                case State.Scanning:
                    Sweep();
                    Watch();
                    break;
                case State.Suspicious:
                    // 마지막으로 본 곳을 향해 천천히 빛을 옮긴다.
                    _angle = Mathf.MoveTowardsAngle(_angle, AngleTo(_lastSeen), sweepSpeed * 0.6f * Time.deltaTime);
                    Watch();
                    if (_suspicion <= 0f && _stateTime > 1.2f) SetState(State.Scanning);
                    break;
                case State.Alert:
                    if (_player != null) _angle = Mathf.MoveTowardsAngle(_angle, AngleTo(_player.transform.position), 90f * Time.deltaTime);
                    if (_player == null || !CanSee(_player)) {
                        _suspicion -= Time.deltaTime * forgetSpeed;
                        if (_suspicion < 0.6f) SetState(State.Suspicious);
                    }
                    else if (_stateTime >= lockTime) {
                        Kill(_player);
                    }
                    break;
                case State.Cooldown:
                    break;
            }

            UpdateBeam();
            ApplyVisual(_open);
        }

        private void Sweep() {
            if (_pause > 0f) {
                _pause -= Time.deltaTime;
                return;
            }
            _angle += _dir * sweepSpeed * Time.deltaTime;
            if (_angle > sweepMax) { _angle = sweepMax; _dir = -1f; _pause = edgePause; }
            if (_angle < sweepMin) { _angle = sweepMin; _dir = 1f; _pause = edgePause; }
        }

        private void Watch() {
            if (_player != null && CanSee(_player)) {
                var rate = 1f / Mathf.Max(0.05f, detectTime);
                if (_player.IsSprinting) rate *= 1.5f;
                _suspicion += Time.deltaTime * rate;
                _lastSeen = _player.transform.position;
                if (_state == State.Scanning) SetState(State.Suspicious);
                if (_suspicion >= 1f) SetState(State.Alert);
            }
            else {
                _suspicion = Mathf.Max(0f, _suspicion - Time.deltaTime * forgetSpeed);
            }
        }

        private void Kill(Player p) {
            SetState(State.Cooldown);
            if (p.TryGetComponent(out DamageModule dmg)) dmg.TakeDamage();
        }

        private Vector2 Origin => scanOrigin != null ? (Vector2)scanOrigin.position : (Vector2)transform.position;

        private static Vector2 Dir(float angle) {
            var r = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(r), -Mathf.Cos(r));
        }

        private float AngleTo(Vector2 p) {
            var d = p - Origin;
            return Mathf.Atan2(d.x, -d.y) * Mathf.Rad2Deg;
        }

        private bool CanSee(Player p) {
            if (p.IsDead || CATestCover.IsHidden(p)) return false;
            Vector2 target = p.transform.position;
            var o = Origin;
            var d = target - o;
            var dist = d.magnitude;
            if (dist > viewDistance) return false;
            if (Mathf.Abs(Mathf.DeltaAngle(AngleTo(target), _angle)) > viewAngle * 0.5f) return false;
            var hit = Physics2D.Raycast(o, d / dist, dist, coverMask);
            return hit.collider == null || hit.distance >= dist - 0.9f;
        }

        private void BuildMesh() {
            if (beamMesh == null) return;
            _mesh = new Mesh { name = "EyeBeam" };
            _mesh.MarkDynamic();
            // 0 = 눈(3D), 1..rays = 평면 위 끝점, rays+1 = 평면 위 원점
            _verts = new Vector3[rays + 2];
            _cols = new Color[rays + 2];
            var tris = new List<int>();
            for (var i = 1; i < rays; i++) { tris.Add(0); tris.Add(i); tris.Add(i + 1); }
            for (var i = 1; i < rays; i++) { tris.Add(rays + 1); tris.Add(i + 1); tris.Add(i); }
            _tris = tris.ToArray();
            _mesh.vertices = _verts;
            _mesh.triangles = _tris;
            beamMesh.sharedMesh = _mesh;
        }

        private void UpdateBeam() {
            var color = _state switch {
                State.Alert => alertColor,
                State.Suspicious => Color.Lerp(calmColor, suspiciousColor, Mathf.Clamp01(_suspicion * 1.5f)),
                _ => calmColor
            };
            var vis = Mathf.Clamp01((_open - 0.35f) / 0.65f);
            if (beamLight != null) {
                beamLight.enabled = vis > 0.01f;
                beamLight.color = color;
                beamLight.intensity = 1.2f * vis * (_state == State.Alert ? 1.6f : 1f);
                beamLight.pointLightOuterRadius = viewDistance;
                beamLight.pointLightInnerAngle = viewAngle * 0.55f;
                beamLight.pointLightOuterAngle = viewAngle;
                beamLight.transform.position = new Vector3(Origin.x, Origin.y, beamLight.transform.position.z);
                beamLight.transform.rotation = Quaternion.Euler(0f, 0f, _angle + 180f);
            }
            if (eyeGlow != null) {
                eyeGlow.color = color;
                eyeGlow.intensity = 0.4f + 1.4f * vis;
            }
            if (_mesh == null) return;

            var o = Origin;
            var apex = eyeball != null ? eyeball.position : new Vector3(o.x, o.y, 0f);
            _verts[0] = beamMesh.transform.InverseTransformPoint(apex);
            _cols[0] = new Color(color.r, color.g, color.b, beamAlpha * 0.25f * vis);
            for (var i = 0; i < rays; i++) {
                var t = i / (rays - 1f);
                var a = _angle - viewAngle * 0.5f + viewAngle * t;
                var dir = Dir(a);
                var hit = Physics2D.Raycast(o, dir, viewDistance, coverMask);
                var len = hit.collider != null ? hit.distance : viewDistance;
                var end = o + dir * len;
                _verts[i + 1] = beamMesh.transform.InverseTransformPoint(new Vector3(end.x, end.y, 0.4f));
                var edge = Mathf.Sin(t * Mathf.PI);
                _cols[i + 1] = new Color(color.r, color.g, color.b, beamAlpha * vis * edge);
            }
            _verts[rays + 1] = beamMesh.transform.InverseTransformPoint(new Vector3(o.x, o.y, 0.4f));
            _cols[rays + 1] = new Color(color.r, color.g, color.b, beamAlpha * 0.6f * vis);
            _mesh.vertices = _verts;
            _mesh.colors = _cols;
            _mesh.RecalculateBounds();
            _mesh.bounds = new Bounds(_mesh.bounds.center, _mesh.bounds.size + Vector3.one * 200f);
        }

        private void ApplyVisual(float open) {
            if (lidTop != null) lidTop.localScale = new Vector3(1f, Mathf.Lerp(1f, 0.12f, Mathf.Clamp01(open)), 1f);
            if (lidBottom != null) lidBottom.localScale = new Vector3(1f, Mathf.Lerp(1f, 0.12f, Mathf.Clamp01(open)), 1f);
            if (iris != null) {
                Vector2 look;
                if (_state == State.Alert && _player != null) look = ((Vector2)_player.transform.position - Origin).normalized;
                else look = Dir(_angle);
                iris.localPosition = new Vector3(look.x * irisTravel, look.y * irisTravel * 0.8f, iris.localPosition.z);
                var dilate = _state == State.Alert ? 0.8f : (_state == State.Suspicious ? 1.1f : 1f);
                iris.localScale = Vector3.Lerp(iris.localScale, Vector3.one * dilate, Time.deltaTime * 4f);
            }
            if (eyeRoot != null) {
                var shake = _state == State.Alert ? (Vector3)(Random.insideUnitCircle * 0.25f) : Vector3.zero;
                var breathe = Vector3.up * (Mathf.Sin(Time.time * 0.6f) * 0.6f);
                eyeRoot.position = _eyeBase + breathe + shake;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            var o = Origin;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(o, 0.6f);
            foreach (var a in new[] { sweepMin - viewAngle * 0.5f, sweepMax + viewAngle * 0.5f }) {
                Gizmos.DrawLine(o, o + Dir(a) * viewDistance);
            }
        }
#endif
    }
}
