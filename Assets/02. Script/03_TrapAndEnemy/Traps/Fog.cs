using _02._Script._01_Players;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace _02._Script._03_TrapAndEnemy.Traps {
    public class Fog : TrapBase {
        [SerializeField] private Image fogImage;
        [SerializeField] private Transform teleport;
        [SerializeField] private float durationIn;
        [SerializeField] private float durationOut;
        
        protected override void OnPlayerEnter(Player player) {
            Sequence.Create()
                .ChainDelay(1f)
                .ChainCallback(() => player.LockMovement())
                .Chain(Tween.Alpha(fogImage, 1f, durationIn, Ease.InCubic))
                .ChainCallback(() => player.RestoreState(teleport.position, 100f))
                .Chain(Tween.Alpha(fogImage, 0f, durationOut, Ease.InExpo));
        }
    }
}