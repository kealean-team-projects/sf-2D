using _02._Script._01_Players;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 샛길 끝에 두는 보상(고대 유물 조각). 닿으면 빛나며 사라지고 획득 개수를 센다.
    /// 프로젝트에 인벤토리/수집 시스템이 없어서 우선 개수만 기록(Debug.Log)한다. 나중에 UI와 연결하면 된다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class CATestCollectible : MonoBehaviour {
        [SerializeField] private string id = "relic";
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private ParticleSystem pickupBurst;

        private bool _taken;
        public static int Collected { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Collected = 0;

        private void Awake() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other) {
            if (_taken || other.GetComponentInParent<Player>() == null) return;
            _taken = true;
            Collected++;
            CATestAudio.PlaySfx("collectible", transform.position);
            if (pickupBurst != null) {
                pickupBurst.transform.SetParent(null, true);
                pickupBurst.Play(true);
                Destroy(pickupBurst.gameObject, 4f);
            }
            if (visualRoot != null) visualRoot.SetActive(false);
            Debug.Log($"[CATest] 유물 획득: {id} (총 {Collected}개)");
            enabled = false;
        }
    }
}
