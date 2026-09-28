using System.Collections.Generic;
using _02._Script._01_Players;
using _02._Script._01_Players.Components.DamageCompo;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LHS_CATest {
    /// <summary>
    /// 심해 보스 "심연의 눈" — 플레이어를 찾아다니는 눈.
    ///
    /// ■ 공간 구조 (멀리 뒤에 떠 있는 거대한 눈 + 플레이 평면의 시야)
    ///   - 실제 판정은 플레이 평면(z=0)의 한 점 "시야 원점(origin)"에서 부채꼴로 한다.
    ///   - 눈 그림(eyeRoot)은 z = eyeZ(뒤쪽 멀리)에 있다. 원근 카메라라서 멀리 있는 물체는 화면에서 덜 움직이므로,
    ///     매 프레임 카메라 위치로 역산해서 "화면상 시야 원점 바로 위"에 보이도록 x,y 를 맞춘다(시차 보정).
    ///   - 빛줄기 메쉬는 꼭짓점 하나를 눈(3D), 나머지를 평면 위 부채꼴 끝(지형에 닿으면 잘림)에 둔다.
    ///     → 빛이 가려진 곳(바위 아래)이 곧 안전지대로 보인다.
    ///
    /// ■ 행동 (Mode)
    ///   Dormant  : 감긴 채 대기 (보스 방 입장 컷신 전)
    ///   Hunt     : 현재 앵커 위에 머물며 좌우로 훑는다.
    ///              플레이어가 현재 앵커를 advanceDistance 이상 앞질러 나아가면 → 다음 앵커로 이동(Moving)
    ///   Moving   : 다음 앵커로 천천히 이동. 이동 중에도 빛은 켜져 있고 발각 속도는 절반.
    ///   Alert    : 발각 게이지가 가득 참 → 빛이 붉어지고 플레이어를 조준, lockTime 뒤 사망
    ///   Rage     : 분노(추격전) — 색이 붉게 변하고 가까이 다가와(z 감소) 플레이어를 곧장 쫓는다. 닿으면 사망.
    ///   Blocked  : 굴 입구에 막힘 — 입구를 향해 몇 번 들이받는다(컷신)
    ///   Leaving  : 포기하고 어둠 속으로 멀어지며 사라짐
    ///
    /// ■ 난이도
    ///   앵커가 뒤로 갈수록 sweepSpeed 가 빨라지고 멈춤 시간이 짧아진다(sweepGain, pauseGain).
    ///   발각 → 사망까지 detectTime + lockTime ≈ 0.5초 (이전 1.4초) — 들키면 거의 바로 끝난다.
    /// </summary>
    public sealed class CATestEyeHunter : MonoBehaviour {
        public enum Mode { Dormant, Hunt, Moving, Alert, Rage, Blocked, Leaving, Gone }

        [Header("Visual")]
        [SerializeField] private Transform eyeRoot;
        [SerializeField] private Transform eyeball;
        [SerializeField] private Transform iris;
        [SerializeField] private Transform lidTop;
        [SerializeField] private Transform lidBottom;
        [SerializeField] private SpriteRenderer[] bodyRenderers;
        [SerializeField] private SpriteRenderer irisRenderer;
        [SerializeField] private Light2D eyeGlow;
        [SerializeField] private float irisTravel = 1.3f;
        [SerializeField] private float eyeZ = 40f;
        [SerializeField] private float rageEyeZ = 14f;
        [SerializeField] private float visualLift = 7f;

        [Header("Scan")]
        [SerializeField] private Transform origin;
        [SerializeField] private Light2D beamLight;
        [SerializeField] private MeshFilter beamMesh;
        [SerializeField] private LayerMask coverMask = 1 << 6;
        [SerializeField] private float viewDistance = 44f;
        [SerializeField, Range(4f, 80f)] private float viewAngle = 22f;
        [SerializeField] private float sweepHalf = 42f;
        [SerializeField] private float sweepSpeed = 20f;
        [SerializeField] private float sweepGain = 3.5f;
        [SerializeField] private float edgePause = 0.7f;
        [SerializeField] private float pauseGain = -0.06f;
        [SerializeField, Min(3)] private int rays = 30;
        [SerializeField] private float beamAlpha = 0.17f;

        [Header("Anchors (시야 원점이 머무는 곳, 플레이 평면 좌표)")]
        [SerializeField] private Vector2[] anchors = { new(540, -60) };
        [SerializeField] private float advanceDistance = 7f;
        [SerializeField] private float moveSpeed = 7f;

        [Header("Detection")]
        [SerializeField] private float detectTime = 0.28f;
        [SerializeField] private float forgetSpeed = 1.2f;
        [SerializeField] private float lockTime = 0.22f;

        [Header("Rage (추격)")]
        [SerializeField] private float chaseSpeed = 9f;
        [SerializeField] private float chaseCatchUp = 0.45f;
        [SerializeField] private float chaseLeash = 16f;
        [SerializeField] private float killRange = 2.6f;
        [SerializeField] private float rageHeight = 5f;
        [SerializeField] private float safeX = 99999f; // 이 x 보다 앞(굴 안)이면 추격 판정 없음

        [Header("Colors")]
        [SerializeField] private Color calmColor = new(0.55f, 0.95f, 1f, 1f);
        [SerializeField] private Color suspiciousColor = new(1f, 0.85f, 0.35f, 1f);
        [SerializeField] private Color alertColor = new(1f, 0.18f, 0.12f, 1f);
        [SerializeField] private Color rageBodyTint = new(1f, 0.35f, 0.32f, 1f);

        public Mode CurrentMode { get; private set; } = Mode.Dormant;
        public int AnchorIndex => _anchor;
        public Vector3 EyeWorldPosition => eyeRoot != null ? eyeRoot.position : transform.position;
        public Vector2 OriginPosition => _pos;

        private Player _player;
        private int _anchor;
        private Vector2 _pos;
        private float _angle;
        private float _dir = 1f;
        private float _pause;
        private float _suspicion;
        private float _stateTime;
        private float _open;
        private float _rage;          // 0 → 1 (색/거리 전환)
        private float _currentZ;
        private float _fade = 1f;     // Leaving 시 사라짐
        private Vector2 _lastSeen;
        private Mesh _mesh;
        private Vector3[] _verts;
        private Color[] _cols;
        private Color[] _bodyBase;
        private Vector3 _lunge;
        private bool _chasing;

        private void Awake() {
            _pos = anchors.Length > 0 ? anchors[0] : (Vector2)transform.position;
            _currentZ = eyeZ;
            _angle = -sweepHalf;
            if (bodyRenderers != null) {
                _bodyBase = new Color[bodyRenderers.Length];
                for (var i = 0; i < bodyRenderers.Length; i++) _bodyBase[i] = bodyRenderers[i] != null ? bodyRenderers[i].color : Color.white;
            }
            BuildMesh();
        }

        // ───────── 외부(감독 스크립트)에서 부르는 명령 ─────────
        public void Wake() { if (CurrentMode == Mode.Dormant) SetMode(Mode.Hunt); _open = Mathf.Max(_open, 0.05f); }

        public void ResetToStart(bool hunting) {
            _anchor = 0;
            _pos = anchors.Length > 0 ? anchors[0] : _pos;
            _suspicion = 0f;
            _angle = -sweepHalf;
            _dir = 1f;
            _rage = 0f;
            _fade = 1f;
            _lunge = Vector3.zero;
            _chasing = false;
            _currentZ = eyeZ;
            gameObject.SetActive(true);
            SetMode(hunting ? Mode.Hunt : Mode.Dormant);
            _open = hunting ? 1f : 0f;
        }

        /// <summary>분노 상태로 전환(색·거리 변화). 추격은 StartChase 를 부를 때까지 시작하지 않는다(컷신 중 사망 방지).</summary>
        public void EnterRage() {
            SetMode(Mode.Rage);
            _suspicion = 0f;
            _chasing = false;
        }

        /// <summary>추격 시작. 플레이어보다 최소 headStart 만큼 뒤에서 출발한다.</summary>
        public void StartChase(float headStart) {
            if (_player != null) _pos.x = Mathf.Min(_pos.x, _player.transform.position.x - headStart);
            _chasing = true;
        }

        public void SetSafeX(float x) => safeX = x;

        public void BlockAt(Vector2 mouth) {
            SetMode(Mode.Blocked);
            _pos = new Vector2(mouth.x - 3f, mouth.y + rageHeight * 0.5f);
        }

        /// <summary>굴 입구를 한 번 들이받는다(컷신에서 여러 번 호출).</summary>
        public async UniTask Lunge() {
            for (var t = 0f; t < 0.18f; t += Time.deltaTime) {
                if (this == null) return;
                _lunge = new Vector3(Mathf.Lerp(0f, 2.2f, t / 0.18f), 0f, -2f * (t / 0.18f));
                await UniTask.Yield();
            }
            CATestCutscene.Shake(0.45f, 0.35f);
            for (var t = 0f; t < 0.5f; t += Time.deltaTime) {
                if (this == null) return;
                _lunge = Vector3.Lerp(new Vector3(2.2f, 0f, -2f), Vector3.zero, t / 0.5f);
                await UniTask.Yield();
            }
            _lunge = Vector3.zero;
        }

        public void Leave() => SetMode(Mode.Leaving);

        private void SetMode(Mode m) {
            CurrentMode = m;
            _stateTime = 0f;
        }

        // ───────── 매 프레임 ─────────
        private void Update() {
            _stateTime += Time.deltaTime;
            _player = CATestHUD.CurrentPlayer;
            var alive = _player != null && _player.isActiveAndEnabled && !_player.IsDead;
            // 컷신 중(조작 잠금)에는 발각 판정을 하지 않는다 → 입장 컷신에서 억울하게 죽지 않게
            var canDetect = alive && !CATestCutscene.IsPlaying;

            var targetOpen = CurrentMode switch {
                Mode.Dormant => 0f,
                Mode.Gone => 0f,
                Mode.Alert or Mode.Rage or Mode.Blocked => 1.15f,
                _ => 1f
            };
            _open = Mathf.MoveTowards(_open, targetOpen, Time.deltaTime * (CurrentMode == Mode.Hunt && _open < 0.95f ? 0.6f : 1.8f));
            var rageTarget = CurrentMode is Mode.Rage or Mode.Blocked or Mode.Leaving ? 1f : 0f;
            _rage = Mathf.MoveTowards(_rage, rageTarget, Time.deltaTime * 0.9f);

            switch (CurrentMode) {
                case Mode.Hunt:
                    Sweep();
                    if (canDetect) Watch(1f);
                    if (alive && _anchor + 1 < anchors.Length && _player.transform.position.x > anchors[_anchor].x + advanceDistance) {
                        _anchor++;
                        SetMode(Mode.Moving);
                    }
                    break;
                case Mode.Moving:
                    Sweep();
                    if (canDetect) Watch(0.5f);
                    _pos = Vector2.MoveTowards(_pos, anchors[_anchor], moveSpeed * Time.deltaTime);
                    if ((_pos - anchors[_anchor]).sqrMagnitude < 0.01f) {
                        // 여러 앵커를 한꺼번에 앞질렀으면 계속 이동
                        if (alive && _anchor + 1 < anchors.Length && _player.transform.position.x > anchors[_anchor].x + advanceDistance) _anchor++;
                        else SetMode(Mode.Hunt);
                    }
                    break;
                case Mode.Alert:
                    if (!canDetect) { SetMode(Mode.Hunt); break; }
                    _angle = Mathf.MoveTowardsAngle(_angle, AngleTo(_player.transform.position), 160f * Time.deltaTime);
                    if (!CanSee(_player)) {
                        _suspicion -= Time.deltaTime * forgetSpeed;
                        if (_suspicion < 0.5f) SetMode(Mode.Hunt);
                    }
                    else if (_stateTime >= lockTime) Kill();
                    break;
                case Mode.Rage:
                    if (alive && _chasing) Chase();
                    else if (alive) {
                        // 컷신 중: 플레이어 뒤쪽 위에서 위협적으로 맴돈다
                        var p = (Vector2)_player.transform.position;
                        _pos = Vector2.MoveTowards(_pos, new Vector2(p.x - 14f, p.y + rageHeight + 3f), 6f * Time.deltaTime);
                        _angle = Mathf.MoveTowardsAngle(_angle, AngleTo(p), 120f * Time.deltaTime);
                    }
                    break;
                case Mode.Blocked:
                    if (alive) _angle = Mathf.MoveTowardsAngle(_angle, AngleTo(_player.transform.position), 90f * Time.deltaTime);
                    break;
                case Mode.Leaving:
                    _pos += new Vector2(-4f, 3f) * Time.deltaTime;
                    _currentZ += 30f * Time.deltaTime;
                    _fade = Mathf.MoveTowards(_fade, 0f, Time.deltaTime * 0.45f);
                    if (_fade <= 0f) { SetMode(Mode.Gone); gameObject.SetActive(false); }
                    break;
            }

            var zTarget = Mathf.Lerp(eyeZ, rageEyeZ, _rage);
            if (CurrentMode != Mode.Leaving) _currentZ = Mathf.MoveTowards(_currentZ, zTarget, Time.deltaTime * 12f);
            if (origin != null) origin.position = new Vector3(_pos.x, _pos.y, 0f);
            UpdateBeam();
        }

        private void LateUpdate() => PlaceVisual();

        private float SweepSpeedNow => sweepSpeed + sweepGain * _anchor;
        private float PauseNow => Mathf.Max(0.2f, edgePause + pauseGain * _anchor);

        private void Sweep() {
            if (_pause > 0f) { _pause -= Time.deltaTime; return; }
            _angle += _dir * SweepSpeedNow * Time.deltaTime;
            if (_angle > sweepHalf) { _angle = sweepHalf; _dir = -1f; _pause = PauseNow; }
            if (_angle < -sweepHalf) { _angle = -sweepHalf; _dir = 1f; _pause = PauseNow; }
        }

        private void Watch(float rateScale) {
            if (CanSee(_player)) {
                var rate = rateScale / Mathf.Max(0.05f, detectTime);
                if (_player.IsSprinting) rate *= 1.3f;
                _suspicion += Time.deltaTime * rate;
                _lastSeen = _player.transform.position;
                if (_suspicion >= 1f) SetMode(Mode.Alert);
            }
            else _suspicion = Mathf.Max(0f, _suspicion - Time.deltaTime * forgetSpeed);
        }

        private void Chase() {
            var p = (Vector2)_player.transform.position;
            // 고무줄: 멀리 떨어질수록 빨라져 긴장감 유지, 가까우면 기본 속도
            var gap = p.x - _pos.x;
            var speed = chaseSpeed + Mathf.Max(0f, gap - chaseLeash) * chaseCatchUp;
            var target = new Vector2(p.x, p.y + rageHeight);
            _pos = Vector2.MoveTowards(_pos, target, speed * Time.deltaTime);
            _angle = Mathf.MoveTowardsAngle(_angle, AngleTo(p), 240f * Time.deltaTime);
            if (p.x < safeX && Mathf.Abs(p.x - _pos.x) < killRange && Mathf.Abs(p.y - (_pos.y - rageHeight)) < 6f) Kill();
        }

        private void Kill() {
            if (_player == null || _player.IsDead) return;
            if (CurrentMode == Mode.Alert) SetMode(Mode.Hunt);
            _suspicion = 0f;
            if (_player.TryGetComponent(out DamageModule dmg)) dmg.TakeDamage();
        }

        private float AngleTo(Vector2 p) {
            var d = p - _pos;
            return Mathf.Atan2(d.x, -d.y) * Mathf.Rad2Deg;
        }

        private static Vector2 Dir(float angle) {
            var r = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(r), -Mathf.Cos(r));
        }

        private bool CanSee(Player p) {
            if (p == null || p.IsDead || CATestCover.IsHidden(p)) return false;
            Vector2 target = p.transform.position + Vector3.up * 0.6f;
            var d = target - _pos;
            var dist = d.magnitude;
            if (dist > viewDistance) return false;
            if (Mathf.Abs(Mathf.DeltaAngle(AngleTo(target), _angle)) > viewAngle * 0.5f) return false;
            var hit = Physics2D.Raycast(_pos, d / dist, dist, coverMask);
            return hit.collider == null || hit.distance >= dist - 0.9f;
        }

        // ───────── 그리기 ─────────
        private void PlaceVisual() {
            if (eyeRoot == null) return;
            var cam = Camera.main;
            var screenPoint = new Vector3(_pos.x, _pos.y + Mathf.Lerp(visualLift, visualLift * 0.5f, _rage), 0f);
            var world = screenPoint;
            if (cam != null) {
                // 시차 보정: 카메라에서 평면까지 거리 d, 눈은 그보다 z 만큼 더 멀다.
                // 평면 위 점 P 와 같은 화면 위치에 보이려면 눈 위치 = C + (P - C) * (d + z) / d
                var c = cam.transform.position;
                var d = Mathf.Max(1f, -c.z);
                var k = (d + _currentZ) / d;
                world = new Vector3(c.x + (screenPoint.x - c.x) * k, c.y + (screenPoint.y - c.y) * k, _currentZ);
            }
            var bob = new Vector3(0f, Mathf.Sin(Time.time * 0.7f) * 0.6f, 0f);
            var shake = (CurrentMode is Mode.Alert or Mode.Rage or Mode.Blocked) ? (Vector3)(Random.insideUnitCircle * (0.15f + 0.25f * _rage)) : Vector3.zero;
            eyeRoot.position = world + bob + shake + _lunge;
            eyeRoot.localScale = Vector3.one * Mathf.Lerp(1f, 1.12f, _rage);

            if (lidTop != null) lidTop.localScale = new Vector3(1f, Mathf.Lerp(1f, 0.12f, Mathf.Clamp01(_open)), 1f);
            if (lidBottom != null) lidBottom.localScale = new Vector3(1f, Mathf.Lerp(1f, 0.12f, Mathf.Clamp01(_open)), 1f);
            if (iris != null) {
                var look = (CurrentMode is Mode.Alert or Mode.Rage or Mode.Blocked) && _player != null
                    ? ((Vector2)_player.transform.position - _pos).normalized
                    : Dir(_angle);
                iris.localPosition = new Vector3(look.x * irisTravel, look.y * irisTravel * 0.8f, iris.localPosition.z);
                var dilate = _rage > 0.5f ? 0.65f : (CurrentMode == Mode.Alert ? 0.8f : 1f);
                iris.localScale = Vector3.Lerp(iris.localScale, Vector3.one * dilate, Time.deltaTime * 4f);
            }
            if (irisRenderer != null) irisRenderer.color = Color.Lerp(Color.white, new Color(1f, 0.25f, 0.2f), _rage) * new Color(1, 1, 1, _fade);
            if (bodyRenderers != null)
                for (var i = 0; i < bodyRenderers.Length; i++) {
                    var r = bodyRenderers[i];
                    if (r == null) continue;
                    var c = _bodyBase[i] * Color.Lerp(Color.white, rageBodyTint, _rage);
                    c.a = _bodyBase[i].a * _fade;
                    r.color = c;
                }
        }

        private void BuildMesh() {
            if (beamMesh == null) return;
            _mesh = new Mesh { name = "HunterBeam" };
            _mesh.MarkDynamic();
            _verts = new Vector3[rays + 2];
            _cols = new Color[rays + 2];
            var tris = new List<int>();
            for (var i = 1; i < rays; i++) { tris.Add(0); tris.Add(i); tris.Add(i + 1); }
            for (var i = 1; i < rays; i++) { tris.Add(rays + 1); tris.Add(i + 1); tris.Add(i); }
            _mesh.vertices = _verts;
            _mesh.triangles = tris.ToArray();
            beamMesh.sharedMesh = _mesh;
        }

        private void UpdateBeam() {
            var color = CurrentMode switch {
                Mode.Alert => alertColor,
                Mode.Rage or Mode.Blocked or Mode.Leaving => Color.Lerp(calmColor, alertColor, _rage),
                _ => Color.Lerp(calmColor, suspiciousColor, Mathf.Clamp01(_suspicion * 1.4f))
            };
            var vis = Mathf.Clamp01((_open - 0.35f) / 0.65f) * _fade * (CurrentMode == Mode.Moving ? 0.75f : 1f);
            if (beamLight != null) {
                beamLight.enabled = vis > 0.01f;
                beamLight.color = color;
                beamLight.intensity = 1.25f * vis * (CurrentMode is Mode.Alert or Mode.Rage ? 1.6f : 1f);
                beamLight.pointLightOuterRadius = viewDistance;
                beamLight.pointLightInnerAngle = viewAngle * 0.55f;
                beamLight.pointLightOuterAngle = viewAngle;
                beamLight.transform.position = new Vector3(_pos.x, _pos.y, beamLight.transform.position.z);
                beamLight.transform.rotation = Quaternion.Euler(0f, 0f, _angle + 180f);
            }
            if (eyeGlow != null) {
                eyeGlow.color = color;
                eyeGlow.intensity = (0.4f + 1.6f * vis) * _fade;
            }
            if (_mesh == null) return;
            var apex = eyeball != null ? eyeball.position : new Vector3(_pos.x, _pos.y, 0f);
            _verts[0] = beamMesh.transform.InverseTransformPoint(apex);
            _cols[0] = new Color(color.r, color.g, color.b, beamAlpha * 0.25f * vis);
            for (var i = 0; i < rays; i++) {
                var t = i / (rays - 1f);
                var a = _angle - viewAngle * 0.5f + viewAngle * t;
                var dir = Dir(a);
                var hit = Physics2D.Raycast(_pos, dir, viewDistance, coverMask);
                var len = hit.collider != null ? hit.distance : viewDistance;
                var end = _pos + dir * len;
                _verts[i + 1] = beamMesh.transform.InverseTransformPoint(new Vector3(end.x, end.y, 0.4f));
                _cols[i + 1] = new Color(color.r, color.g, color.b, beamAlpha * vis * Mathf.Sin(t * Mathf.PI));
            }
            _verts[rays + 1] = beamMesh.transform.InverseTransformPoint(new Vector3(_pos.x, _pos.y, 0.4f));
            _cols[rays + 1] = new Color(color.r, color.g, color.b, beamAlpha * 0.6f * vis);
            _mesh.vertices = _verts;
            _mesh.colors = _cols;
            _mesh.RecalculateBounds();
            _mesh.bounds = new Bounds(_mesh.bounds.center, _mesh.bounds.size + Vector3.one * 200f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.cyan;
            if (anchors == null) return;
            for (var i = 0; i < anchors.Length; i++) {
                Gizmos.DrawWireSphere(anchors[i], 0.8f);
                if (i > 0) Gizmos.DrawLine(anchors[i - 1], anchors[i]);
            }
        }
#endif
    }
}
