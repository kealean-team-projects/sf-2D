using _02._Script.Player.Interface;
using UnityEngine;

namespace _02._Script.Interaction {
    public class Light : MonoBehaviour, IInteractable {
        public void Interact(Player.Player owner) {
            Debug.Log("빛");
        }
    }
}