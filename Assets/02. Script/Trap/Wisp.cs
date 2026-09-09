using PrimeTween;
using UnityEngine;

namespace _02._Script.Trap {
    public class Wisp : MonoBehaviour {
        [SerializeField] private Transform ghostTrm;
        [SerializeField] private float size;
        
        private bool _isWorked;
        
        private void OnTriggerEnter2D(Collider2D other) {
            if(_isWorked) return;
            _isWorked = true;
            Sequence.Create()
                .Chain(Tween.Scale(ghostTrm, size, 1f, Ease.OutExpo))
                .ChainCallback(() => Destroy(other.gameObject))
                .Chain(Tween.Scale(ghostTrm, 0f, 0.4f, Ease.OutExpo));
        }
    }
}