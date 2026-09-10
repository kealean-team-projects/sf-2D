using _02._Script.Players.Interface;
using UnityEngine;

namespace _02._Script.Interaction {
    public class Light : MonoBehaviour, IInteractable {
        public void Interact(Players.Player owner) {
            Debug.Log("빛");
        }
    }
}