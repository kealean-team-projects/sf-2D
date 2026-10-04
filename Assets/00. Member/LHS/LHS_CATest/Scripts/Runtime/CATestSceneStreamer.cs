using System;
using System.Collections.Generic;
using _02._Script._05_Managers;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LHS_CATest {
    /// <summary>
    /// CoreScene에 상주하면서 플레이어 위치를 보고 맵 씬을 Additive로 미리 불러오고(Load), 멀리 떨어지면 내리는(Unload) 스트리머.
    ///
    /// 흐름
    ///  1) 각 맵 씬은 월드 좌표상 차지하는 영역(bounds)을 가진다.
    ///  2) 플레이어가 bounds + loadPadding 안으로 들어오면 그 씬을 Additive 로드한다. (숲 끝자락에 오면 심해가 미리 올라옴)
    ///  3) 플레이어가 bounds + unloadPadding 밖으로 나가면 언로드한다. (load < unload 로 간격을 둬서 경계에서 깜빡이지 않게 함 = 히스테리시스)
    ///  4) 사망 후 부활할 때는 GameManager.OnRespawnReset 에서 부활 위치의 씬이 로드될 때까지 기다린 뒤 플레이어를 옮긴다.
    ///
    /// Build Settings에 씬이 없어도 에디터 Play 모드에서는 scenePath로 로드된다(EditorSceneManager.LoadSceneAsyncInPlayMode).
    /// 실제 빌드에서는 Build Profiles의 Scene List에 두 맵 씬을 등록해야 한다.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class CATestSceneStreamer : MonoBehaviour {
        [Serializable]
        public class StreamZone {
            public string sceneName;
            public string scenePath;
            [Tooltip("이 맵 씬이 월드에서 차지하는 영역 (x, y, width, height)")]
            public Rect bounds;
            [Tooltip("bounds 바깥 이 거리 안으로 들어오면 미리 로드")]
            public float loadPadding = 35f;
            [Tooltip("bounds 바깥 이 거리보다 멀어지면 언로드 (loadPadding보다 커야 함)")]
            public float unloadPadding = 70f;

            [NonSerialized] public bool busy;

            public float Distance(Vector2 p) {
                var dx = Mathf.Max(bounds.xMin - p.x, 0f, p.x - bounds.xMax);
                var dy = Mathf.Max(bounds.yMin - p.y, 0f, p.y - bounds.yMax);
                return Mathf.Sqrt(dx * dx + dy * dy);
            }
        }

        [SerializeField] private Transform target;
        [SerializeField] private List<StreamZone> zones = new();
        [SerializeField, Min(0.05f)] private float checkInterval = 0.2f;
        [SerializeField] private bool drawGizmos = true;

        private float _nextCheck;
        // "고정점": EnsureLoadedAt 으로 불러 둔 곳은 잠시 동안 언로드하지 않는다.
        // (플레이어가 아직 멀리 있는 상태에서 목적지 맵을 먼저 불러 두면, 순간이동 직전에
        //  주기 검사가 '플레이어와 멀다'고 판단해 방금 불러온 맵을 내려 버리는 문제 방지 — 프롤로그 → 숲, 심해 → 황혼)
        private Vector2 _pin;
        private float _pinUntil = -1f;
        public static CATestSceneStreamer Instance { get; private set; }
        public IReadOnlyList<StreamZone> Zones => zones;

        private void Awake() {
            Instance = this;
            // 프롤로그 방(CATest_Prologue)은 CoreScene 을 다시 빌드하지 않아도 되도록 실행 중에 스트리밍 목록에 추가한다.
            AddZoneIfMissing("CATest_Prologue", CATestSceneFlow.PrologueScenePath, CATestWorld.PrologueBounds, 30f, 60f);
        }

        /// <summary>같은 이름의 구역이 없을 때만 스트리밍 구역을 추가(실행 중 전용 — 씬 파일은 바뀌지 않음).</summary>
        public void AddZoneIfMissing(string sceneName, string scenePath, Rect bounds, float load, float unload) {
            if (zones.Exists(z => z.sceneName == sceneName)) return;
            zones.Add(new StreamZone { sceneName = sceneName, scenePath = scenePath, bounds = bounds, loadPadding = load, unloadPadding = unload });
        }

        private void OnEnable() {
            GameManager.OnRespawnReset += HandleRespawn;
        }

        private void OnDisable() {
            GameManager.OnRespawnReset -= HandleRespawn;
        }

        private void OnDestroy() {
            if (Instance == this) Instance = null;
        }

        public void SetTarget(Transform t) => target = t;

        private void Update() {
            if (target == null || Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + checkInterval;
            Refresh(target.position);
        }

        private void Refresh(Vector2 p) {
            foreach (var zone in zones) {
                if (zone.busy) continue;
                var d = zone.Distance(p);
                var loaded = IsLoaded(zone);
                var pinned = Time.unscaledTime < _pinUntil && zone.Distance(_pin) <= zone.unloadPadding;
                if (!loaded && d <= zone.loadPadding) LoadZone(zone).Forget();
                else if (loaded && d > zone.unloadPadding && !pinned) UnloadZone(zone).Forget();
            }
        }

        /// <summary>위치 p 주변에 필요한 씬을 모두 로드하고 끝날 때까지 기다린다.</summary>
        public async UniTask EnsureLoadedAt(Vector2 p) {
            _pin = p;
            _pinUntil = float.PositiveInfinity; // 로드가 끝날 때까지 고정
            var tasks = new List<UniTask>();
            foreach (var zone in zones)
                if (zone.Distance(p) <= zone.loadPadding)
                    tasks.Add(LoadZone(zone));
            await UniTask.WhenAll(tasks);
            // 로드 중이던 다른 작업이 있으면 끝날 때까지 대기
            await UniTask.WaitUntil(() => !zones.Exists(z => z.busy && z.Distance(p) <= z.loadPadding));
            // 로드가 끝난 뒤에도 몇 초 더 고정 → 호출한 쪽이 플레이어를 옮길 시간
            if (_pin == p) _pinUntil = Time.unscaledTime + 4f;
        }

        private async UniTask HandleRespawn() {
            // 사망 연출(화면 암전) 중에 부활 지점의 씬이 올라와 있도록 보장한다.
            var pos = CATestSavePoint.LastSavePosition ?? CATestBootstrap.StartPosition;
            if (pos.HasValue) await EnsureLoadedAt(pos.Value);
        }

        private static Scene FindScene(StreamZone zone) {
            var s = string.IsNullOrEmpty(zone.scenePath) ? default : SceneManager.GetSceneByPath(zone.scenePath);
            if (!s.IsValid()) s = SceneManager.GetSceneByName(zone.sceneName);
            return s;
        }

        public static bool IsLoaded(StreamZone zone) {
            var s = FindScene(zone);
            return s.IsValid() && s.isLoaded;
        }

        private async UniTask LoadZone(StreamZone zone) {
            if (zone.busy || IsLoaded(zone)) return;
            zone.busy = true;
            try {
                AsyncOperation op = null;
                if (Application.CanStreamedLevelBeLoaded(zone.sceneName))
                    op = SceneManager.LoadSceneAsync(zone.sceneName, LoadSceneMode.Additive);
#if UNITY_EDITOR
                else if (!string.IsNullOrEmpty(zone.scenePath))
                    op = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(zone.scenePath,
                        new LoadSceneParameters(LoadSceneMode.Additive));
#endif
                if (op == null) {
                    Debug.LogWarning($"[CATestSceneStreamer] '{zone.sceneName}' 씬을 로드할 수 없습니다. Build Profiles Scene List를 확인하세요.", this);
                    return;
                }
                await op;
            }
            finally {
                zone.busy = false;
            }
        }

        private async UniTask UnloadZone(StreamZone zone) {
            var s = FindScene(zone);
            if (zone.busy || !s.IsValid() || !s.isLoaded) return;
            zone.busy = true;
            try {
                var op = SceneManager.UnloadSceneAsync(s);
                if (op != null) await op;
            }
            finally {
                zone.busy = false;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            if (!drawGizmos) return;
            foreach (var z in zones) {
                var c = new Vector3(z.bounds.center.x, z.bounds.center.y, 0f);
                Gizmos.color = new Color(0.2f, 1f, 0.6f, 0.9f);
                Gizmos.DrawWireCube(c, new Vector3(z.bounds.width, z.bounds.height, 0f));
                Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.35f);
                Gizmos.DrawWireCube(c, new Vector3(z.bounds.width + z.loadPadding * 2, z.bounds.height + z.loadPadding * 2, 0f));
                Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.25f);
                Gizmos.DrawWireCube(c, new Vector3(z.bounds.width + z.unloadPadding * 2, z.bounds.height + z.unloadPadding * 2, 0f));
            }
        }
#endif
    }
}
