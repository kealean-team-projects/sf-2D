#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LHS_CATest.EditorTools {
    /// <summary>
    /// 플레이어 Animator(LHS/Character/Player.controller) 등반 애니메이션 수정.
    ///
    /// ■ 문제: 벽에 붙어도 PlayerFall 만 재생됨
    ///   ClimbBT(등반 블렌드 트리)로 가는 전환이 GroundBT(지상)에서만 있었다.
    ///   벽에는 보통 점프해서 붙기 때문에 그 순간 Animator 는 PlayerJump/PlayerFall 상태이고,
    ///   그 상태들에는 ClimbBT 로 가는 전환이 없어 IsClimbing=true 가 되어도 계속 Fall 에 머문다.
    ///   (벽점프/벽대쉬 트리거도 ClimbBT 에서만 받으므로 함께 재생되지 않음)
    ///
    /// ■ 해결: Any State → ClimbBT (IsClimbing == true) 전환 추가
    ///   - Any State 전환은 "현재 어떤 상태든" 조건만 맞으면 바로 넘어간다.
    ///   - canTransitionToSelf = false : 이미 ClimbBT 이면 매 프레임 다시 들어가며 애니메이션이 처음으로 되돌아가는 것 방지
    ///   - hasExitTime = false, duration 0.05 : 붙는 즉시 거의 끊김 없이 전환
    ///   추가로 Any State → PlayerWallJump / PlayerWallDash (트리거) 도 넣어, 어떤 상태에서 발동해도 재생되게 한다.
    ///   이미 같은 전환이 있으면 다시 추가하지 않는다(여러 번 실행해도 안전).
    /// </summary>
    public static class CATestAnimatorFix {
        public const string ControllerPath = "Assets/00. Member/LHS/Character/Player.controller";

        [MenuItem("Tools/LHS_CATest/Fix Player Animator (등반 애니메이션)", priority = 60)]
        public static void Fix() {
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (ac == null) {
                Debug.LogWarning($"[LHS_CATest] {ControllerPath} 를 찾을 수 없어 Animator 수정을 건너뜁니다.");
                return;
            }
            var sm = ac.layers[0].stateMachine;
            var climb = Find(sm, "ClimbBT");
            var wallJump = Find(sm, "PlayerWallJump");
            var wallDash = Find(sm, "PlayerWallDash");
            var changed = false;
            changed |= AddAny(sm, climb, AnimatorConditionMode.If, "IsClimbing", 0.05f);
            changed |= AddAny(sm, wallJump, AnimatorConditionMode.If, "WallJump", 0.02f);
            changed |= AddAny(sm, wallDash, AnimatorConditionMode.If, "WallDash", 0.02f);
            if (changed) {
                EditorUtility.SetDirty(ac);
                AssetDatabase.SaveAssets();
                Debug.Log("[LHS_CATest] Player.controller: Any State → ClimbBT / WallJump / WallDash 전환 추가 완료");
            }
        }

        private static AnimatorState Find(AnimatorStateMachine sm, string name) =>
            sm.states.Select(s => s.state).FirstOrDefault(s => s.name == name);

        private static bool AddAny(AnimatorStateMachine sm, AnimatorState target, AnimatorConditionMode mode, string param, float duration) {
            if (target == null) return false;
            if (sm.anyStateTransitions.Any(t => t.destinationState == target)) return false;
            var t = sm.AddAnyStateTransition(target);
            t.AddCondition(mode, 0f, param);
            t.hasExitTime = false;
            t.duration = duration;
            t.canTransitionToSelf = false;
            return true;
        }
    }
}
#endif
