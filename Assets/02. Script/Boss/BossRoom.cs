using System;
using System.Threading;
using _02._Script._04_Interaction;
using _02._Script._05_Managers;
using _02._Script.Boss.BossPatterns;
using _02._Script.Boss.BossZones;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace _02._Script.Boss {
    public class BossRoom : MonoBehaviour
    {
        [SerializeField] private float startDelay = 1f;
        [SerializeField] private float orthographSize = 24f;
        [SerializeField] private CinemachineCamera bossCamera;
        [SerializeField] private BossTrigger trigger;
        [SerializeField] private BossTimeLine timeLine;
        [SerializeField] private Boss boss;
        [SerializeField] private RoomLightCycle lightCycle;
        [SerializeField] private BossZone zones;
        [SerializeField] private LayerMask navigationObstacles = (1 << 6) | (1 << 9);
        [SerializeField] private BossNavigation navigation;

        [SerializeField] private Transform light;

        private InteractLight[] lights;
        private CancellationTokenSource roomCts;
        
        [SerializeField] private Light2D globalLight;
        private float _normalGlobalIntensity;
        private Vector3 initialBossPosition;
        private Quaternion initialBossRotation;
        private float entryLensSize;

        public InteractLight[] Lights => lights;

        public bool IsStarted { get; private set; }

        private void Awake() {
            initialBossPosition = boss.transform.position;
            initialBossRotation = boss.transform.rotation;
            lights = light.GetComponentsInChildren<InteractLight>();

            foreach (var roomLight in lights)
                if (roomLight != null)
                    roomLight.TurnOff();
            
            _normalGlobalIntensity = globalLight.intensity;
        }

        private void OnEnable() {
            trigger.OnEnter += Begin;
            GameManager.OnRespawnReset += ResetRoom;
        }

        private void OnDisable() {
            trigger.OnEnter -= Begin;
            GameManager.OnRespawnReset -= ResetRoom;
            Stop();
        }

        public void Begin() {
            if (IsStarted || !isActiveAndEnabled) return;
            if (boss == null || lightCycle == null) {
                Debug.LogError("BossRoom의 Boss와 Light Cycle을 연결하세요.", this);
                return;
            }
            if (navigation == null || !navigation.IsReady) {
                Debug.LogError("보스방 NavMesh를 먼저 Bake하세요: Tools > Boss > Bake SecondMap Navigation", this);
                return;
            }

            IsStarted = true;
            boss.Navigation = navigation;
            if (bossCamera != null) {
                entryLensSize = bossCamera.Lens.OrthographicSize;
                bossCamera.Lens.OrthographicSize = orthographSize;
            }
            boss.gameObject.SetActive(true);
            roomCts = new CancellationTokenSource();
            Run(roomCts.Token).Forget();
        }

        private async UniTask Run(CancellationToken token) {
            try {
                await UniTask.Delay(TimeSpan.FromSeconds(startDelay), cancellationToken: token);
                foreach (var roomLight in lights)
                    roomLight?.TurnOn();
                
                SetLightBrightness(1f);

                var firstCycle = true;
                while (true) {
                    token.ThrowIfCancellationRequested();
                    SetLightBrightness(1f);
                    
                    await lightCycle.Wait(token); // 추가
                    await lightCycle.Dim(this, token);
                    
                    var darkUntil = Time.time + lightCycle.DarkHoldDuration;

                    if (firstCycle && timeLine != null)
                        await timeLine.Descend(token);

                    firstCycle = false;
                    await boss.RunPatterns(token);

                    while (Time.time < darkUntil)
                        await UniTask.NextFrame(token);

                    SetLightBrightness(1f);
                    await UniTask.NextFrame(token);
                }
            }
            catch (OperationCanceledException) { }
            finally {
                roomCts.Dispose();
                roomCts = null;
                SetLightBrightness(1f);
            }
        }

        public void Stop() {
            roomCts?.Cancel();
        }

        private async UniTask ResetRoom()
        {
            if (!IsStarted) return;
            Stop();
            boss.Stop();
            // Wait until the old pattern's finally blocks have finished before restoring state.
            await UniTask.WaitUntil(() => roomCts == null);

            boss.ResetForRetry(initialBossPosition, initialBossRotation);

            foreach (var roomLight in lights) {
                if (roomLight == null) continue;
                roomLight.ResetForRetry();
            }
            if (globalLight != null) globalLight.intensity = _normalGlobalIntensity;
            if (bossCamera != null) bossCamera.Lens.OrthographicSize = entryLensSize;
            IsStarted = false;
        }

        public void SetLightBrightness(float ratio) {
            ratio = Mathf.Clamp01(ratio);

            if (globalLight != null)
                globalLight.intensity = _normalGlobalIntensity * ratio;

            foreach (var roomLight in lights) {
                if (roomLight != null)
                    roomLight.SetBrightness(ratio);
            }
        }

        public InteractLight GetClosestLight(Vector2 position) {
            InteractLight closest = null;
            var closestDistance = float.MaxValue;

            foreach (var roomLight in lights) {
                if (roomLight == null || !roomLight.IsActive) continue;
                if (!roomLight.isActiveAndEnabled || !boss.CanSee(roomLight.transform)) continue;

                var distance = ((Vector2)roomLight.transform.position - position).sqrMagnitude;

                if (distance >= closestDistance) continue;

                closestDistance = distance;
                closest = roomLight;
            }

            return closest;
        }
        
        public BossZone GetZone(Vector2 position) => zones;
    }
}
