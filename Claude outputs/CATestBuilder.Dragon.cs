#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace LHS_CATest.EditorTools {
    /// <summary>
    /// 수호룡 (LHS_Test/DragonGuardian_Layered_SegmentedTail.psb, 뼈 35개 리그) 배치 + 스켈레탈 애니메이션 클립 생성.
    ///
    /// ■ 뼈 구조 (Skinning Editor 에서 만든 리그, 용은 왼쪽을 바라봄)
    ///   body_root ─ body_01 … body_05 ─ tail_01_root … tail_tip   (몸통 → 꼬리, 오른쪽으로)
    ///            └ neck_01 ─ neck_02 ─ head_01 ─ head_02           (목 → 머리, 왼쪽으로)
    ///   다리: front_far/near_(upper·elbow·wrist·claw), rear_far/near_(upper·lower·claw)
    ///
    /// ■ 클립 만드는 원리 (키프레임을 코드로 찍음 → Animation 창에서 열어 손으로 고칠 수 있음)
    ///   각 뼈의 "기본 각도(바인드 포즈)" + 시간에 따른 추가 각도 를 1/30초마다 키로 기록한다.
    ///   - 물결(서펀타인): 몸통~꼬리 뼈 i 번째에 A·sin(ωt − i·φ) → 파도가 머리에서 꼬리로 전해지는 동양 용의 헤엄치는 몸짓
    ///     자식 뼈는 부모 회전을 이어받으므로 뼈마다 작은 각도만 줘도 꼬리 끝으로 갈수록 크게 휘어진다.
    ///   - 목/머리: 음수 z 회전 = 고개를 듦(용이 왼쪽을 보고 있어서), 양수 = 숙임
    ///   - 다리: 윗다리 양수 z = 다리를 뒤(꼬리 쪽)로 접음
    ///   Idle(4초 반복) / Fly(1.6초 반복) / Speak(1.4초 반복) / Roar(1.8초 1회) / Strike(0.9초 1회)
    /// ■ Animator Controller: 상태 5개(전환 화살표 없음) → CATestDragon.Play("상태이름") 이 CrossFade 로 부드럽게 바꿈.
    /// </summary>
    public static partial class CATestBuilder {
        private const string DragonPsbPath = "Assets/00. Member/LHS/LHS_Test/DragonGuardian_Layered_SegmentedTail.psb";
        private const string DragonAnimDir = Root + "/Animations/Dragon";
        // PSB 원본은 약 21×3.7 유닛(100PPU). 9배 → 길이 약 190, 높이 약 33 유닛(플레이어 키의 15배 이상). 머리만 약 22 유닛.
        // 머리~앞다리만 화면 오른쪽 절반을 채우고 몸통·꼬리는 화면 밖으로 뻗어 "다 보이지 않을 만큼 거대한" 느낌.
        private const float DragonScale = 9f;

        private static readonly string[] BodyChain = {
            "body_01", "body_02", "body_03", "body_04", "body_05", "tail_01_root", "tail_01_mid", "tail_02_root", "tail_02_mid",
            "tail_03_root", "tail_03_mid", "tail_04_root", "tail_04_mid", "tail_05_root", "tail_05_mid", "tail_tip"
        };
        private static readonly string[] NeckChain = { "neck_01", "neck_02", "head_01", "head_02" };
        private static readonly string[] Uppers = { "front_far_upper", "front_near_upper", "rear_far_upper", "rear_near_upper" };
        private static readonly string[] Lowers = { "front_far_elbow", "front_near_elbow", "rear_far_lower", "rear_near_lower" };

        /// <summary>둥지 씬에 수호룡을 놓고 애니메이션을 연결한다. 루트에 CATestDragon.</summary>
        public static CATestDragon BuildDragonInstance(Transform parent, Vector3 pos) {
            var psb = AssetDatabase.LoadAssetAtPath<GameObject>(DragonPsbPath);
            if (psb == null) {
                Debug.LogWarning("[LHS_CATest] 용 PSB 를 찾지 못했습니다: " + DragonPsbPath);
                return null;
            }
            var root = new GameObject("GuardianDragon");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(psb, root.scene);
            inst.transform.SetParent(root.transform, false);
            inst.transform.localScale = Vector3.one * DragonScale;

            // 그림 전체의 중심이 루트(0,0)에 오도록 옮김 (PSB 원점이 문서 모서리일 수 있어서)
            var rs = inst.GetComponentsInChildren<SpriteRenderer>(true);
            if (rs.Length > 0) {
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                var center = b.center;
                inst.transform.position += new Vector3(pos.x - center.x, pos.y - center.y, 0f);
            }
            // 정렬: Item 레이어(뒤 배경 바위보다 앞, 땅보다 뒤), 원래 부위 순서는 유지
            foreach (var r in rs) {
                r.sortingLayerID = SL("Item");
                r.sortingOrder = 20 + r.sortingOrder;
            }

            // 뼈 찾기
            var bones = new Dictionary<string, Transform>();
            foreach (var t in inst.GetComponentsInChildren<Transform>(true)) if (!bones.ContainsKey(t.name)) bones[t.name] = t;
            if (!bones.ContainsKey("body_root")) Debug.LogWarning("[LHS_CATest] 용 뼈(body_root)를 찾지 못했습니다. 리그 이름을 확인하세요.");

            // 애니메이션 클립 + 컨트롤러
            var controller = BuildDragonAnimator(inst.transform, bones);
            var anim = inst.GetComponent<Animator>();
            if (anim == null) anim = inst.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // 눈빛(머리 뼈에 붙여 함께 움직임) + 몸 테두리 빛
            Light2D eye = null;
            if (bones.TryGetValue("head_02", out var head)) {
                eye = Light(head, "EyeGlow", head.position, C(1f, 0.8f, 0.45f), 1.4f, 18f, 2f);
                var sr = Put(head, Glow, head.position + new Vector3(0f, 0.8f, -0.1f), 2.2f, "Item", 90, C(1f, 0.85f, 0.6f, 0.5f), mat: MatAdd);
                _ = sr;
            }
            var rim = Light(root.transform, "BodyRim", pos + new Vector3(-6f, 10f, 0f), C(1f, 0.55f, 0.5f), 0.8f, 100f, 15f);

            var dragon = root.AddComponent<CATestDragon>();
            Set(dragon, "animator", anim);
            Set(dragon, "eyeGlow", eye);
            Set(dragon, "bodyRim", rim);
            if (head != null) Set(dragon, "head", head);
            // 날아오는 동안 바람/먼지
            Motes(root.transform, "DragonDust", new Rect(-60f, -14f, 120f, 28f), -0.5f, MatAddDot, C(1f, 0.85f, 0.8f, 0.4f), C(0.8f, 0.8f, 1f, 0.3f),
                60f, 0.08f, 0.24f, new Vector2(-1f, 0.2f), 0.6f, 3f, "Player", 30).transform.localPosition = new Vector3(0f, 0f, -0.5f);
            return dragon;
        }

        // ───────────────────────── 클립 생성 ─────────────────────────
        private delegate void Pose(float t, Dictionary<string, float> rot, ref float rootY);

        private static AnimatorController BuildDragonAnimator(Transform animRoot, Dictionary<string, Transform> bones) {
            EnsureFolder(DragonAnimDir);
            var idle = MakeClip("Dragon_Idle", animRoot, bones, 4f, true, (float t, Dictionary<string, float> r, ref float y) => {
                var w = Mathf.PI * 2f / 4f;
                Wave(r, t, w, 2.2f, 3.6f, 0.55f);
                r["neck_01"] = -3f + 3f * Mathf.Sin(w * t + 1f);
                r["neck_02"] = 2f * Mathf.Sin(w * t + 1.6f);
                r["head_01"] = -2f * Mathf.Sin(w * t + 2.2f);
                Legs(r, t, w, 8f, 0f);
                y = 0.15f * Mathf.Sin(w * t);
            });
            var fly = MakeClip("Dragon_Fly", animRoot, bones, 1.6f, true, (float t, Dictionary<string, float> r, ref float y) => {
                var w = Mathf.PI * 2f / 1.6f;
                Wave(r, t, w, 4.5f, 7f, 0.6f);
                r["neck_01"] = -4f + 4f * Mathf.Sin(w * t + 0.8f);
                r["neck_02"] = 3f * Mathf.Sin(w * t + 1.4f);
                r["head_01"] = -3f * Mathf.Sin(w * t + 2f);
                Legs(r, t, w, 6f, 32f); // 다리를 뒤로 접고 살짝만 움직임
                y = 0.3f * Mathf.Sin(w * t);
            });
            var speak = MakeClip("Dragon_Speak", animRoot, bones, 1.4f, true, (float t, Dictionary<string, float> r, ref float y) => {
                var w = Mathf.PI * 2f / 1.4f;
                Wave(r, t, w, 1.2f, 2.4f, 0.5f);
                r["neck_01"] = -7f + 2f * Mathf.Sin(w * t);                               // 고개를 든 채
                r["neck_02"] = -2f;
                r["head_01"] = -3f * Mathf.Abs(Mathf.Sin(w * 2f * t));                    // 말하듯 짧게 까딱
                r["head_02"] = 2f * Mathf.Sin(w * 2f * t + 0.5f);
                Legs(r, t, w, 4f, 0f);
                y = 0.1f * Mathf.Sin(w * t);
            });
            var roar = MakeClip("Dragon_Roar", animRoot, bones, 1.8f, false, (float t, Dictionary<string, float> r, ref float y) => {
                var up = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.45f)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 1.35f) / 0.45f)));
                var w = Mathf.PI * 2f / 0.6f;
                Wave(r, t, w, 2f * up + 0.8f, 8f * up + 1.5f, 0.7f);                       // 꼬리를 세차게 휘두름
                r["body_root"] = 3f * up;
                r["neck_01"] = -18f * up;
                r["neck_02"] = -6f * up;
                r["head_01"] = -12f * up + 2f * Mathf.Sin(t * 40f) * up;                  // 포효하며 떨림
                r["head_02"] = -4f * up;
                Legs(r, t, w, 12f * up, -10f * up);                                         // 앞발을 들어 올림
                y = 0.6f * up;
            });
            var strike = MakeClip("Dragon_Strike", animRoot, bones, 0.9f, false, (float t, Dictionary<string, float> r, ref float y) => {
                var hit = t < 0.2f ? Mathf.SmoothStep(0f, 1f, t / 0.2f) : 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.35f) / 0.55f));
                Wave(r, t, Mathf.PI * 2f, 1f, 3f + 3f * hit, 0.6f);
                r["body_root"] = -4f * hit;
                r["neck_01"] = 22f * hit;
                r["neck_02"] = 8f * hit;
                r["head_01"] = 10f * hit;
                Legs(r, t, 6f, 4f, 20f * hit);
                y = -0.5f * hit;
            });

            var path = DragonAnimDir + "/AC_GuardianDragon.controller";
            AssetDatabase.DeleteAsset(path);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(path);
            var sm = ac.layers[0].stateMachine;
            AnimatorState first = null;
            foreach (var (name, clip) in new[] { ("Idle", idle), ("Fly", fly), ("Speak", speak), ("Roar", roar), ("Strike", strike) }) {
                var st = sm.AddState(name);
                st.motion = clip;
                first ??= st;
            }
            sm.defaultState = first;
            AssetDatabase.SaveAssets();
            return ac;
        }

        /// <summary>몸통/꼬리 물결: 몸통 뼈는 aBody, 꼬리 뼈는 aTail 진폭, 뼈마다 phase 만큼 늦게.</summary>
        private static void Wave(Dictionary<string, float> r, float t, float w, float aBody, float aTail, float phase) {
            for (var i = 0; i < BodyChain.Length; i++) {
                var a = BodyChain[i].StartsWith("tail") ? aTail : aBody;
                r[BodyChain[i]] = a * Mathf.Sin(w * t - i * phase);
            }
        }

        private static void Legs(Dictionary<string, float> r, float t, float w, float amp, float tuck) {
            for (var i = 0; i < Uppers.Length; i++) {
                var ph = i * 1.3f;
                r[Uppers[i]] = tuck + amp * Mathf.Sin(w * t + ph);
                r[Lowers[i]] = tuck * 0.5f + amp * 0.7f * Mathf.Sin(w * t + ph + 0.8f);
            }
        }

        private static AnimationClip MakeClip(string name, Transform animRoot, Dictionary<string, Transform> bones, float length, bool loop, Pose pose) {
            var clip = new AnimationClip { name = name, frameRate = 30f };
            const float step = 1f / 30f;
            var keys = new Dictionary<string, List<Keyframe>>();
            var rootKeys = new List<Keyframe>();
            var rest = new Dictionary<string, Vector3>();
            foreach (var kv in bones) rest[kv.Key] = kv.Value.localEulerAngles;
            var restRootPos = bones.TryGetValue("body_root", out var br) ? br.localPosition : Vector3.zero;
            var frames = Mathf.RoundToInt(length / step);
            for (var f = 0; f <= frames; f++) {
                var t = f * step;
                var rot = new Dictionary<string, float>();
                var y = 0f;
                pose(loop ? t % length : t, rot, ref y);
                foreach (var kv in rot) {
                    if (!keys.TryGetValue(kv.Key, out var list)) keys[kv.Key] = list = new List<Keyframe>();
                    list.Add(new Keyframe(t, kv.Value));
                }
                rootKeys.Add(new Keyframe(t, y));
            }
            foreach (var kv in keys) {
                if (!bones.TryGetValue(kv.Key, out var bone)) continue;
                var p = AnimationUtility.CalculateTransformPath(bone, animRoot);
                var r0 = rest[kv.Key];
                var z = new AnimationCurve(kv.Value.ConvertAll(k => new Keyframe(k.time, r0.z + k.value)).ToArray());
                for (var i = 0; i < z.length; i++) z.SmoothTangents(i, 0f);
                clip.SetCurve(p, typeof(Transform), "localEulerAnglesRaw.x", AnimationCurve.Constant(0f, length, r0.x));
                clip.SetCurve(p, typeof(Transform), "localEulerAnglesRaw.y", AnimationCurve.Constant(0f, length, r0.y));
                clip.SetCurve(p, typeof(Transform), "localEulerAnglesRaw.z", z);
            }
            if (br != null) {
                var p = AnimationUtility.CalculateTransformPath(br, animRoot);
                // 뼈의 로컬 좌표는 부모(루트) 크기 기준 → 월드 0.x 유닛만큼 오르내리도록 배율을 나눠 줌
                var scale = Mathf.Max(0.0001f, animRoot.lossyScale.y);
                var yc = new AnimationCurve(rootKeys.ConvertAll(k => new Keyframe(k.time, restRootPos.y + k.value / scale)).ToArray());
                clip.SetCurve(p, typeof(Transform), "m_LocalPosition.x", AnimationCurve.Constant(0f, length, restRootPos.x));
                clip.SetCurve(p, typeof(Transform), "m_LocalPosition.y", yc);
                clip.SetCurve(p, typeof(Transform), "m_LocalPosition.z", AnimationCurve.Constant(0f, length, restRootPos.z));
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            var path = $"{DragonAnimDir}/{name}.anim";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }
    }
}
#endif
