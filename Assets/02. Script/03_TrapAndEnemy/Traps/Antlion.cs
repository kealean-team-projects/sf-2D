using _02._Script._01_Players;
using PrimeTween;
using UnityEngine;

namespace _02._Script._03_TrapAndEnemy.Traps {
    public class Antlion : MonoBehaviour {
        [SerializeField] private Transform teleport;
        [SerializeField] private float durationIn;
        [SerializeField] private Ease InEase = Ease.OutExpo;

        [SerializeField] private float durationOut;
        [SerializeField] private Ease OutEase = Ease.OutExpo;

        private bool _isWorked;

        private void OnTriggerEnter2D(Collider2D collision) {
            if (_isWorked) return;
            _isWorked = true;
            var player = collision.gameObject.GetComponent<Player>();
            if (player == null) return;
            Sequence.Create()
                .ChainCallback(() => player.LockMovement())
                .Group(Tween.Position(transform,
                    new Vector3(transform.position.x, transform.position.y + 1, transform.position.z), durationIn,
                    InEase))
                .Chain(Tween.Position(player.transform,
                    new Vector3(player.transform.position.x, player.transform.position.y - 2,
                        player.transform.position.y), durationOut - 0.1f, Ease.OutExpo))
                .Group(Tween.Position(transform,
                    new Vector3(transform.position.x, transform.position.y, transform.position.z), durationOut,
                    OutEase))
                .ChainCallback(() => player.RestoreState(teleport.position, 100f));
        }
    }
}