using _02._Script.Players;
using UnityEngine;

namespace _02._Script.Interaction {
    public class Light : InteractBase {
        public override void Interact(Player owner) {
            Debug.Log("빛");
        }
    }
}
