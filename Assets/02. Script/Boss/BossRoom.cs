using System;
using System.Threading;
using _02._Script._04_Interaction;
using _02._Script._05_Managers;
using _02._Script.Boss.BossPatterns;
using _02._Script.Boss.BossZones;
using Cysharp.Threading.Tasks;
using csiimnida.CSILib.SoundManager.RunTime;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace _02._Script.Boss {
    public class BossRoom : MonoBehaviour
    {
        [SerializeField] private float startDelay = 1f;
        [SerializeField, Min(0.01f)] private float cameraDistance = 127f;
        [SerializeField] private CinemachineCamera bossCamera;
        [SerializeField, Min(0f)] private float zoomDuration = 0.7f;
        [Header("Room Audio")]
        [SerializeField] private StageAudio stageAudio;
        [Tooltip("비워 두면 스테이지 배경음을 유지합니다.")]
        [SerializeField] private string bossBackgroundMusic;
        [SerializeField] private string roomEnterSound = "BossRoomEnter";
        [SerializeField] private string lightsOnSound = "TurnOn";
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
        private CinemachinePositionComposer positionComposer;
        private float entryCameraDistance;
        private bool presentationActive;
        private bool waitingForScan;
        private SoundManager soundManager;
        private AudioSource entryAudio;
        private AudioSource lightAudio;

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
            IsStarted = false;
        }

        public void Begin() {
            if (IsStarted || roomCts != null || !isActiveAndEnabled) return;
            if (boss == null || lightCycle == null) {
                Debug.LogError("BossRoom의 Boss와 Light Cycle을 연결하세요.", this);
                return;
            }
            if (navigation == null || !navigation.IsReady) {
                Debug.LogError("보스방 NavMesh를 먼저 Bake하세요: Tools > Boss > Bake SecondMap Navigation", this);
                return;
            }

            IsStarted = true;
            waitingForScan = true;
            boss.Navigation = navigation;
            positionComposer = bossCamera != null
                ? bossCamera.GetComponent<CinemachinePositionComposer>() : null;
            if (positionComposer != null)
                entryCameraDistance = positionComposer.CameraDistance;
            else
                Debug.LogWarning("Boss Camera에 CinemachinePositionComposer를 연결하세요.", this);
            presentationActive = true;
            soundManager = FindAnyObjectByType<SoundManager>();
            if (stageAudio == null) {
                foreach (var root in gameObject.scene.GetRootGameObjects()) {
                    stageAudio = root.GetComponentInChildren<StageAudio>();
                    if (stageAudio != null) break;
                }
            }
            if (soundManager != null && !string.IsNullOrWhiteSpace(roomEnterSound))
                entryAudio = soundManager.PlayTrackedSound(roomEnterSound);
            if (stageAudio != null) stageAudio.PauseMusic(this);
            boss.gameObject.SetActive(true);
            roomCts = new CancellationTokenSource();
            Run(roomCts).Forget();
        }

        private async UniTask Run(CancellationTokenSource session) {
            var token = session.Token;
            try {
                await ZoomOut(token);
                token.ThrowIfCancellationRequested();
                foreach (var roomLight in lights)
                    roomLight?.TurnOn();
                
                SetLightBrightness(1f);
                PlayLightsOnSound();
                await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, startDelay)), cancellationToken: token);

                var firstCycle = true;
                while (true) {
                    token.ThrowIfCancellationRequested();
                    SetLightBrightness(1f);
                    
                    await lightCycle.Wait(token);
                    token.ThrowIfCancellationRequested();
                    waitingForScan = true;
                    if (stageAudio != null) stageAudio.PauseMusic(this);
                    await lightCycle.Dim(this, token);
                    
                    var darkUntil = Time.time + lightCycle.DarkHoldDuration;

                    if (firstCycle && timeLine != null)
                        await timeLine.Descend(token);

                    firstCycle = false;
                    await boss.RunPatterns(token);

                    while (Time.time < darkUntil)
                        await UniTask.NextFrame(token);

                    SetLightBrightness(1f);
                    PlayLightsOnSound();
                    await UniTask.NextFrame(token);
                }
            }
            catch (OperationCanceledException) { }
            finally {
                session.Dispose();
                if (ReferenceEquals(roomCts, session)) roomCts = null;
                if (this != null && isActiveAndEnabled) SetLightBrightness(1f);
            }
        }

        private async UniTask ZoomOut(CancellationToken token) {
            if (positionComposer == null) return;
            var elapsed = 0f;
            while (elapsed < zoomDuration) {
                await UniTask.NextFrame(token);
                token.ThrowIfCancellationRequested();
                if (positionComposer == null) return;
                elapsed += Time.deltaTime;
                var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / zoomDuration));
                positionComposer.CameraDistance = Mathf.Lerp(entryCameraDistance, cameraDistance, progress);
            }
            token.ThrowIfCancellationRequested();
            if (positionComposer != null) positionComposer.CameraDistance = cameraDistance;
        }

        public void OnScanCompleted() {
            if (!IsStarted || !presentationActive || !waitingForScan) return;
            waitingForScan = false;
            if (stageAudio != null) stageAudio.ResumeMusic(this);
            if (stageAudio != null && !string.IsNullOrWhiteSpace(bossBackgroundMusic))
                stageAudio.OverrideMusic(bossBackgroundMusic, this);
        }

        public void OnPlayerDetected() {
            if (!IsStarted || !presentationActive || soundManager == null ||
                string.IsNullOrWhiteSpace(roomEnterSound)) return;
            soundManager.StopSound(entryAudio);
            entryAudio = soundManager.PlayTrackedSound(roomEnterSound);
        }

        private void PlayLightsOnSound() {
            if (soundManager == null || string.IsNullOrWhiteSpace(lightsOnSound)) return;
            soundManager.StopSound(lightAudio);
            lightAudio = soundManager.PlayTrackedSound(lightsOnSound);
        }

        private void RestorePresentation() {
            if (!presentationActive) return;
            presentationActive = false;
            if (positionComposer != null) positionComposer.CameraDistance = entryCameraDistance;
            if (stageAudio != null) stageAudio.RestoreMusic(this);
            if (stageAudio != null) stageAudio.ResumeMusic(this);
            if (soundManager != null) {
                soundManager.StopSound(entryAudio);
                soundManager.StopSound(lightAudio);
            }
            entryAudio = null;
            lightAudio = null;
        }

        public void Stop() {
            roomCts?.Cancel();
            RestorePresentation();
        }

        private async UniTask ResetRoom()
        {
            if (!IsStarted) return;
            Stop();
            boss.Stop();
            // Wait until the old pattern's finally blocks have finished before restoring state.
            await UniTask.WaitUntil(() => roomCts == null);
            if (this == null || !isActiveAndEnabled) return;

            boss.ResetForRetry(initialBossPosition, initialBossRotation);

            foreach (var roomLight in lights) {
                if (roomLight == null) continue;
                roomLight.ResetForRetry();
            }
            if (globalLight != null) globalLight.intensity = _normalGlobalIntensity;
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
