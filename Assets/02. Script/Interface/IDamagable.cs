using _02._Script.Component;
using UnityEngine;

namespace _02._Script.Interface
{
    public class IDamagable : MonoBehaviour
    {
        [field: SerializeField] public DamageModule DamageCompo { get; private set; }
    }
}