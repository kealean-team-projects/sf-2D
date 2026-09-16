using _02._Script.Component;
using _02._Script.Players;
using PrimeTween;
using UnityEngine;

namespace _02._Script.Trap {
    public class Wisp : TrapBase {
        [SerializeField] private Transform ghostTrm;
        [SerializeField] private float size;

        private bool _isWorked;

        protected override void OnPlayerEnter(Player player)
        {
            if (_isWorked) return;
            _isWorked = true;
            Sequence.Create()
                .Chain(Tween.Scale(ghostTrm, size, 1f, Ease.OutExpo))
                .ChainCallback(() => player.GetComponent<DamageModule>().TakeDamage())
                .Chain(Tween.Scale(ghostTrm, 0f, 0.4f, Ease.OutExpo));
        }
    }
}