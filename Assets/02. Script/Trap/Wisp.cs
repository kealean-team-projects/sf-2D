using PrimeTween;
using UnityEngine;

namespace _02._Script.Trap {
    public class Wisp : MonoBehaviour {
        [SerializeField] private Transform ghostTrm;
        private bool _isWorked;
        
        private void OnTriggerEnter2D(Collider2D other) {
            if(_isWorked) return;
            other.GetComponent<Player.Player>().ApplyDontMove();
            Sequence.Create()
                .Chain(Tween.Scale(ghostTrm, 4f, 1f, Ease.OutExpo))
                .ChainCallback(() => {
                    Destroy(other.gameObject); 
                    _isWorked = true;
                })
                .Chain(Tween.Scale(ghostTrm, 0f, 1f, Ease.OutBack));
        }
    }
}