using System.Reflection;
using _02._Script._01_Players;
using _02._Script._01_Players.Components.DamageCompo;
using _02._Script._03_TrapAndEnemy.Traps;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 미역 줄기 함정 (기존 SeaweedStem 대체).
    ///
    /// 기존 SeaweedStem 문제: _originalSpeed 가 한 번도 설정되지 않아(0) 닿는 순간 Player.ChangeSpeed(0)이 되고,
    /// 벗어나도 속도 0으로 "복구"되어 영영 움직일 수 없게 된다. 원본은 건드리지 않고 이 스크립트로 대체했다.
    ///
    /// 동작: 닿으면 속도가 slowMultiplier 배로 줄어든다. 머문 시간이 dangerTime을 넘으면 휘감겨 사망.
    /// 줄기 색이 점점 붉어져 위험을 알려준다. 여러 덤불이 겹쳐도 원래 속도는 한 번만 저장/복구한다.
    /// </summary>
    public sealed class CATestSeaweed : TrapBase {
        [SerializeField, Range(0.1f, 1f)] private float slowMultiplier = 0.45f;
        [SerializeField] private float dangerTime = 3.2f;
        [SerializeField] private SpriteRenderer[] stems;
        [SerializeField] private Color dangerColor = new(1f, 0.35f, 0.35f, 1f);

        private static readonly FieldInfo MoveSpeedField =
            typeof(Player).GetField("moveSpeed", BindingFlags.NonPublic | BindingFlags.Instance);

        private static int _slowCount;
        private static float _baseSpeed = -1f;

        private Player _player;
        private float _timer;
        private Color[] _baseColors;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() {
            _slowCount = 0;
            _baseSpeed = -1f;
        }

        private void Awake() {
            if (stems == null) return;
            _baseColors = new Color[stems.Length];
            for (var i = 0; i < stems.Length; i++) if (stems[i] != null) _baseColors[i] = stems[i].color;
        }

        protected override void OnPlayerEnter(Player player) {
            if (_player != null || player.IsDead) return;
            _player = player;
            _timer = 0f;
            if (_slowCount++ == 0 && MoveSpeedField != null) {
                _baseSpeed = (float)MoveSpeedField.GetValue(player);
                player.ChangeSpeed(_baseSpeed * slowMultiplier);
            }
        }

        protected override void OnPlayerExit(Player player) {
            if (_player == player) Release();
        }

        private void OnDisable() {
            if (_player != null) Release();
        }

        private void Release() {
            var p = _player;
            _player = null;
            _timer = 0f;
            if (--_slowCount <= 0) {
                _slowCount = 0;
                if (p != null && _baseSpeed > 0f) p.ChangeSpeed(_baseSpeed);
            }
        }

        private void Update() {
            if (_player != null) {
                if (_player.IsDead) { Release(); return; }
                _timer += Time.deltaTime;
                if (_timer >= dangerTime) {
                    var p = _player;
                    Release();
                    if (p.TryGetComponent(out DamageModule dmg)) dmg.TakeDamage();
                }
            }
            else {
                _timer = Mathf.MoveTowards(_timer, 0f, Time.deltaTime * 2f);
            }

            if (stems == null || _baseColors == null) return;
            var k = Mathf.Clamp01(_timer / dangerTime);
            for (var i = 0; i < stems.Length; i++) {
                if (stems[i] == null) continue;
                stems[i].color = Color.Lerp(_baseColors[i], dangerColor, k * k);
                stems[i].transform.localRotation = Quaternion.Euler(0, 0,
                    Mathf.Sin(Time.time * (1.2f + k * 6f) + i * 0.7f) * (4f + k * 6f));
            }
        }
    }
}
