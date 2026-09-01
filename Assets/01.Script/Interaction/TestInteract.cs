using _01.Script.Player.Interface;
using UnityEngine;

namespace _01.Script.Interaction {
    public class TestInteract : MonoBehaviour, IInteractable {
        public void Interact(Player.Player owner) {
            Debug.Log("호승아 미소녀 캐릭터 그려줘");
        }
    }
}