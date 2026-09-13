using _02._Script.Players;
using _02._Script.Players.Interface;
using UnityEngine;

namespace _02._Script.Interaction {
    public class TestInteract : MonoBehaviour, IInteractable {
        public void Interact(Player owner) {
            Debug.Log("호승아 미소녀 캐릭터 그려줘");
        }
    }
}