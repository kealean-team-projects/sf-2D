using System;
using _02._Script._01_Players;
using _02._Script._05_Managers;
using UnityEngine;

namespace _02._Script
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class CheckPoint : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            SaveManager.Instance.RequestCapture();
        }
    }
}