using _02._Script._01_Players;
using PrimeTween;
using UnityEngine;

namespace _02._Script._03_TrapAndEnemy.Traps {
    public class Octopus : TrapBase {
        [SerializeField] private SpriteRenderer sr;
        [SerializeField] private Transform trm;
        [SerializeField] private Transform ghostTrm;

        [SerializeField] private float checkRange = 5f;
        [SerializeField] private LayerMask whatIsPlayer;

        private bool _isCollider;
        

        private void Update() {
                /*Sequence.Create()
                    .Group(Tween.Scale(trm, 1f, 1f, Ease.OutExpo))
                    .ChainCallback(() => _isCollider = true);*/
        }

        private void FixedUpdate()
        {
            if (_isCollider) return;
            var col = Physics2D.OverlapCircle(transform.position, checkRange,  whatIsPlayer);
            
            if (col == null) return;
            Activate();
        }


        protected override void OnPlayerEnter(Player player)
        {
            if (!_isCollider) return;
            
            Sequence.Create()
                .Group(Tween.Color(sr, new Color(0, 0, 0), 1f, Ease.OutExpo))
                .Group(Tween.Scale(ghostTrm, 40f, 1f, Ease.InBack));
        }

        private void Activate()
        {
            Sequence.Create()
                .Group(Tween.Scale(trm, 1f, 1f, Ease.OutExpo))
                .ChainCallback(() => _isCollider = true);
        }
        
        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, checkRange);
        }
        #endif
    }
}