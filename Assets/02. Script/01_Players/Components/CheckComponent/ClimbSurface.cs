using UnityEngine;

namespace _02._Script._01_Players.Components.CheckComponent {
    /// <summary>
    /// 등반 면(ClimbWall 레이어 콜라이더)에 붙이는 선택 설정.
    /// twoSided = true : 공중에 매달린 덩굴/밧줄처럼 양쪽 어디로든 벽점프할 수 있는 면.
    ///                   (일반 벽은 항상 벽 반대쪽으로 점프하지만, 덩굴에서는 누르는 방향으로 뛴다)
    /// </summary>
    public sealed class ClimbSurface : MonoBehaviour {
        [SerializeField] private bool twoSided = true;
        public bool TwoSided => twoSided;
    }
}
