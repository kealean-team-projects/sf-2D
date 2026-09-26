using _00._Member.LHS.Script;
using _02._Script._01_Players;
using UnityEngine;

namespace _02._Script {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ChapterLoadTrigger : MonoBehaviour {
        [Tooltip("ChapterLoader의 챕터 목록 순서. 1부터 시작합니다.")]
        [SerializeField, Min(1)] private int targetChapter = 2;

        private void Reset() {
            var area = GetComponent<BoxCollider2D>();
            area.isTrigger = true;
            area.size = new Vector2(2f, 3f);
        }

        private void Awake() {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other) {
            var player = other.GetComponentInParent<Player>();
            if (player == null || player.IsDead) return;
            var loader = ChapterLoader.Instance;
            if (loader == null) {
                Debug.LogWarning("챕터 교체에는 CoreScene의 ChapterLoader가 필요합니다.", this);
                return;
            }
            // The loader acquires its loading guard before the first await, including
            // when several player colliders enter this trigger in the same physics step.
            loader.TryLoadChapter(targetChapter);
        }

        private void OnDrawGizmos() {
            var area = GetComponent<BoxCollider2D>();
            if (area == null) return;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.15f, 0.8f, 1f, 0.25f);
            Gizmos.DrawCube(area.offset, area.size);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(area.offset, area.size);
        }
    }
}
