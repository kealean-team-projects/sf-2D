using _02._Script._01_Players;
using _02._Script._04_Interaction;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 프롤로그 방의 방문. E(상호작용)를 누르면 CATestPrologue 에 "문을 열었다"고 알린다.
    /// 원본 InteractBase 를 상속 → 원본 Interactor 가 가까이 가면 대상으로 잡고, CATestHUD 가 [E] 안내를 띄운다.
    /// 한 번 열리면 다시 상호작용되지 않도록 콜라이더를 끈다.
    /// </summary>
    public sealed class CATestRoomDoor : InteractBase {
        [SerializeField] private CATestPrologue prologue;
        public bool Opened { get; private set; }

        public override void Interact(Player owner) {
            if (Opened) return;
            if (prologue != null && !prologue.CanOpenDoor) return; // 일어나는 컷신 도중에는 무시
            Opened = true;
            SetHighlight(false);
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            if (prologue != null) prologue.OnDoorOpened(owner);
        }

        public void ResetDoor() {
            Opened = false;
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = true;
        }
    }
}
