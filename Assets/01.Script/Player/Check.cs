using UnityEngine;

namespace _01.Script.Player
{
    public abstract class Check : MonoBehaviour
    {
        public bool isGizmos;
        public Color gizmosColor = Color.red;
        
        [SerializeField] private Vector2 checkSize = new Vector2(0.2f, 1f);
        [SerializeField] private LayerMask whatIsLayer;
        
        protected Collider2D CheckCol()
        {
            return Physics2D.OverlapBox(transform.position, checkSize, 0f, whatIsLayer);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!isGizmos) return;
            Gizmos.color = gizmosColor;
            Gizmos.DrawWireCube(transform.position, checkSize);
        }
#endif
    }
}