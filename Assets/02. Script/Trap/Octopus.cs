using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _02._Script.Trap {
    public class Octopus : MonoBehaviour {
        [SerializeField] private Collider2D coll;
        [SerializeField] private SpriteRenderer sr;
        [SerializeField] private Transform trm;
        [SerializeField] private Transform ghostTrm;

        private bool _isCollider;
        
        private void Update() {
            if (Keyboard.current.tKey.wasPressedThisFrame) {
                Sequence.Create()
                    .Group(Tween.Scale(trm, 1f,  1f, Ease.OutExpo))
                    .ChainCallback(()=>_isCollider = true);
            }
        }

        private void OnTriggerEnter2D(Collider2D other) {
            if(!_isCollider) return;
            Sequence.Create()
                .Group(Tween.Color(sr, new Color(0, 0, 0), 1f, Ease.OutExpo))
                .Group(Tween.Scale(ghostTrm, 40f, 1f, Ease.InBack));
        }
    }
}