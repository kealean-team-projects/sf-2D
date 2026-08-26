using System;
using UnityEngine;

namespace _01.Script.Player.Components
{
    public class Interactor : MonoBehaviour
    {
        [SerializeField] private float radius;
        [SerializeField] private Vector2 offset;
        [SerializeField] private ContactFilter2D target;


        private Vector2 Offset => offset + (Vector2)transform.position;
        private Collider2D[] _results;

        private void Awake()
        {
            _results = new Collider2D[10];
        }

        public void Interact(Player owner)
        {
            int count = Physics2D.OverlapCircle(Offset, radius, target, _results);
            if (count <= 0) return;
            
        }
        
    }
    
}