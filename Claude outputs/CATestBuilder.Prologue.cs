#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Random = System.Random;

namespace LHS_CATest.EditorTools {
    /// <summary>
    /// 프롤로그 "현실의 방" (x -2402 ~ -2382.5, 바닥 y 0, 천장 y 6.24). 화창한 숲(-1965~)과 멀리 떨어진 곳이라 따로 스트리밍된다.
    /// 배치 숫자는 "설계 좌표"(폭 30, 천장 9.6)이고 CATestWorld.RoomScale(0.65)배로 줄여 놓는다 → 플레이어 키(약 2.5)에 맞는 크기.
    ///
    /// ■ 배치 (왼쪽 → 오른쪽)
    ///   침대(머리판 왼쪽, 베개) · 협탁(탁상시계·휴대폰) · 포스터·가족사진(벽) · 책상(스탠드·노트북·책·머그) + 의자
    ///   창문(새벽 하늘·먼 도시 불빛·초승달) + 커튼 · 선반(책·화분) · 벽시계(5시 47분) · 방문(오른쪽 끝)
    ///   앞쪽(카메라 가까이) 큰 화분 실루엣 · 바닥 러그
    /// ■ 깊이(z): 벽 1.6 / 벽 소품 1.2 / 가구 0.8 / 플레이어 0 / 이불 -0.2 / 앞 화분 -3
    /// ■ 조명: 새벽 푸른 전체광(분위기 구역) + 창문빛(빛줄기 + 먼지) + 스탠드 주황빛 + 문 아래 틈으로 새는 흰 금빛(이상한 기운 암시)
    /// ■ 연출: CATestPrologue(일어나기 → 비틀비틀 → 문 E → 빛에 빨려 들어감 → 오프닝), 비몽사몽 Volume(가중치는 스크립트가 조절)
    /// </summary>
    public static partial class CATestBuilder {
        public const string PrologueScenePath = ScenesDir + "/CATest_Prologue.unity";
        private const string RMA = ArtDir + "/Room/";
        public const float RoomX0 = CATestWorld.RoomX0, RoomX1 = CATestWorld.RoomX1, RoomTop = CATestWorld.RoomTop;
        public static readonly Vector2 PrologueStart = CATestWorld.PrologueStart;
        public const float RoomDoorX = CATestWorld.RoomDoorX;

        public static void BuildPrologueScene() {
            var scene = NewMapScene(PrologueScenePath);
            var rnd = new Random(547);
            var root = new GameObject("CATest_Prologue").transform;
            var solid = Group(root, "10_Collision").transform;
            var room = Group(root, "30_Room").transform;
            var fg = Group(root, "05_Foreground").transform;
            var vfx = Group(root, "70_VFX_Light").transform;
            var gameplay = Group(root, "20_Gameplay").transform;
            var zones = Group(root, "80_Zones").transform;

            // ── 충돌: 바닥 / 양쪽 벽 / 천장 ──
            Box(solid, "Floor", new Rect(RoomX0 - 8f, -3f, RoomX1 - RoomX0 + 16f, 3f), false, LayerGround);
            Box(solid, "WallLeft", new Rect(RoomX0 - 2f, 0f, 2f, RoomTop + 2f), false, LayerGround);
            Box(solid, "WallRight", new Rect(RoomX1, 0f, 2f, RoomTop + 2f), false, LayerGround);
            Box(solid, "Ceiling", new Rect(RoomX0 - 2f, RoomTop, RoomX1 - RoomX0 + 4f, 2f), false, LayerGround);

            var white = Color.white;
            // 설계 좌표(폭 30, 천장 9.6 기준 숫자) → 월드 좌표. 방 전체를 RoomScale 배로 줄여 플레이어 키에 맞춘다.
            const float S = CATestWorld.RoomScale;
            float X(float dx) => CATestWorld.RoomX(dx);
            float Y(float dy) => CATestWorld.RoomY(dy);
            SpriteRenderer P(string file, float x, float y, float z, string layer, int order, Color tint, float scale = 1f, Material mat = null) {
                var s = Sp(RMA + file);
                if (s == null) { Debug.LogWarning("[LHS_CATest] 방 스프라이트 없음: " + file); return null; }
                // x = 가운데, y = 아래쪽 기준(스프라이트 피벗은 중앙이므로 높이의 절반만큼 올림) — 둘 다 설계 좌표
                var sc = scale * S;
                return Put(room, s, new Vector3(X(x), Y(y) + s.bounds.extents.y * sc, z), sc, layer, order, tint, mat: mat);
            }
            const float dcx = -2387f; // 설계 좌표의 방 가운데
            const float doorD = -2376f; // 설계 좌표의 문 x

            // ── 벽 / 바닥 / 방 바깥 검정 ──
            P("RM_Wall.png", dcx, 0f, 1.6f, "BackGround", -100, white);
            P("RM_Floor.png", dcx, -1.6f, 0.2f, "Ground", 0, white);
            var px = Sp(RMA + "RM_Pixel.png");
            if (px != null) {
                // 방 양 끝 두꺼운 어두운 기둥(무대 단면처럼 방 바깥을 자름)
                foreach (var ex in new[] { RoomX0 - 1.5f, RoomX1 + 1.5f }) {
                    var sr = Put(room, px, new Vector3(ex, 4f, 1.0f), 1f, "Default", 60, C(0.05f, 0.04f, 0.06f));
                    if (sr != null) sr.transform.localScale = new Vector3(3f / 0.16f, 16f / 0.16f, 1f);
                }
                var top = Put(room, px, new Vector3((RoomX0 + RoomX1) * 0.5f, RoomTop + 1.5f, 1.0f), 1f, "Default", 60, C(0.05f, 0.04f, 0.06f));
                if (top != null) top.transform.localScale = new Vector3(40f / 0.16f, 3f / 0.16f, 1f);
            }

            // ── 벽 소품 ──
            P("RM_Window.png", -2385.5f, 3.3f, 1.2f, "BackGround", -60, white);
            P("RM_CurtainRod.png", -2385.5f, 6.85f, 1.15f, "BackGround", -55, white);
            P("RM_CurtainL.png", -2388.3f, 2.75f, 1.1f, "BackGround", -50, white);
            P("RM_CurtainR.png", -2382.7f, 2.75f, 1.1f, "BackGround", -50, white);
            P("RM_Poster.png", -2396f, 4.4f, 1.2f, "BackGround", -60, white);
            P("RM_PhotoFrame.png", -2392.6f, 4.9f, 1.2f, "BackGround", -60, white);
            P("RM_Clock.png", doorD, 6.0f, 1.2f, "BackGround", -60, white);
            P("RM_Shelf.png", -2380.5f, 4.0f, 1.2f, "BackGround", -60, white);

            // ── 가구 ──
            P("RM_Rug.png", -2386f, 0f, 0.5f, "Default", 5, white);
            P("RM_Bed.png", -2395.3f, 0f, 0.8f, "Default", 10, white);
            P("RM_Nightstand.png", -2390.95f, 0f, 0.8f, "Default", 10, white);
            P("RM_Desk.png", -2385.5f, 0f, 0.8f, "Default", 10, white);
            P("RM_Chair.png", -2383.1f, 0f, 0.7f, "Default", 12, white);

            // 이불: 플레이어 몸 위를 덮음(플레이어보다 앞 정렬)
            var blanket = P("RM_Blanket.png", -2394.3f, 1.18f, -0.2f, "Architecture", 5, white);

            // ── 방문: 문 너머 빛 → 문짝(경첩 피벗) → 문틀 ──
            var doorLight = P("RM_DoorLight.png", doorD, 0.05f, 0.95f, "Default", 30, C(1f, 1f, 1f, 0f), 1.02f);
            var hinge = new GameObject("DoorHinge").transform;
            hinge.SetParent(room, false);
            var panelSp = Sp(RMA + "RM_DoorPanel.png");
            var panelW = (panelSp != null ? panelSp.bounds.size.x : 2.46f) * S;
            hinge.position = new Vector3(RoomDoorX - panelW * 0.5f, 0f, 0.9f);
            SpriteRenderer panel = null;
            if (panelSp != null)
                panel = Put(hinge, panelSp, new Vector3(RoomDoorX, panelSp.bounds.extents.y * S, 0.9f), S, "Default", 32, white);
            P("RM_DoorFrame.png", doorD, 0f, 0.88f, "Default", 34, white);
            // 문 아래 틈 빛(가산 번짐 띠) + 조명
            var gapGlow = Put(room, Sp(RMA + "RM_SoftGlow.png"), new Vector3(RoomDoorX, 0.06f, 0.85f), 1f, "Default", 35, C(1f, 0.92f, 0.7f, 0.8f), mat: MatAdd);
            if (gapGlow != null) gapGlow.transform.localScale = new Vector3(1.0f * S, 0.06f * S, 1f);
            var gapLight = Light(vfx, "DoorGapLight", new Vector3(RoomDoorX, 0.2f, 0f), C(1f, 0.9f, 0.7f), 0.6f, 4.5f * S, 0.3f * S, 0.7f);
            var doorL2D = Light(vfx, "DoorLight", new Vector3(RoomDoorX, Y(2.6f), 0f), C(1f, 0.95f, 0.85f), 0f, 22f * S, 2f * S, 0.6f);
            var doorGlow = Put(vfx, Glow, new Vector3(RoomDoorX, Y(2.6f), -0.5f), 4.5f * S, "Architecture", 8, C(1f, 0.95f, 0.85f, 0f), mat: MatAdd);

            // 문 상호작용
            var doorGo = new GameObject("RoomDoor") { layer = LayerInteractable };
            doorGo.transform.SetParent(gameplay, false);
            doorGo.transform.position = new Vector3(RoomDoorX, Y(2f), 0f);
            var dbox = doorGo.AddComponent<BoxCollider2D>();
            dbox.size = new Vector2(2.6f, 3.6f) * S;
            var roomDoor = doorGo.AddComponent<CATestRoomDoor>();
            Set(roomDoor, "targetRenderer", panel);
            var lbl = doorGo.AddComponent<CATestInteractLabel>();
            Set(lbl, "label", "문 열기");

            // ── 앞쪽 실루엣(카메라 가까이) ──
            Put(fg, Sp(RMA + "RM_PlantFG.png"), new Vector3(X(-2378.8f), Y(1.0f), -3f), 1.35f * S, "Architecture", 40, C(0.06f, 0.06f, 0.09f));

            // ── 조명 / 입자 ──
            // 창문: 새벽빛(푸른 분홍) + 비스듬한 빛줄기 + 먼지
            Light(vfx, "WindowLight", new Vector3(X(-2385.5f), Y(5f), 0f), C(0.7f, 0.72f, 1f), 0.9f, 10f * S, 1.5f * S, 0.6f);
            GodRay(vfx, "WindowRay", X(-2381.5f), Y(0.2f), 8.5f * S, 3.4f * S, 28f, C(0.85f, 0.8f, 1f), 0.45f, 3.3f, "Default", 40, 0.3f, 0.7f, 3f, 0.6f);
            Motes(vfx, "WindowDust", new Rect(X(-2388f), Y(0.5f), 8f * S, 5.5f * S), -0.1f, MatAddDot, C(1f, 0.95f, 0.9f, 0.55f), C(0.8f, 0.85f, 1f, 0.4f),
                6f, 0.02f, 0.05f, new Vector2(0.03f, -0.02f), 0.08f, 9f, "Player", 30);
            // 스탠드 주황빛
            Light(vfx, "DeskLamp", new Vector3(X(-2386.6f), Y(2.35f), 0f), C(1f, 0.72f, 0.45f), 0.9f, 4.5f * S, 0.4f * S, 0.7f);
            Put(vfx, Glow, new Vector3(X(-2386.55f), Y(2.45f), 0.6f), 0.55f * S, "Default", 20, C(1f, 0.8f, 0.55f, 0.55f), mat: MatAdd);
            // 문 쪽으로 빨려 드는 빛 입자(문 열 때만)
            var pull = Motes(vfx, "PullMotes", new Rect(X(-2392f), Y(0.5f), 14f * S, 7f * S), -0.3f, MatAddDot, C(1f, 0.97f, 0.88f, 0.9f), C(1f, 0.85f, 0.7f, 0.7f),
                60f, 0.03f, 0.09f, new Vector2(7f * S, 0f), 0.4f, 2.2f, "Architecture", 9);
            var pm = pull.main; pm.playOnAwake = false; pm.prewarm = false;
            pull.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // ── 비몽사몽 Volume (가중치 0 에서 시작, CATestPrologue 가 조절) ──
            var groggyProf = Profile("VP_CATest_Groggy", p => {
                var v = p.Add<Vignette>(true); v.intensity.Override(0.55f); v.smoothness.Override(0.9f); v.color.Override(C(0.05f, 0.03f, 0.08f));
                var ca = p.Add<ChromaticAberration>(true); ca.intensity.Override(0.7f);
                var ld = p.Add<LensDistortion>(true); ld.intensity.Override(-0.22f); ld.scale.Override(0.97f);
                var adj = p.Add<ColorAdjustments>(true); adj.saturation.Override(-40f); adj.postExposure.Override(-0.15f);
                var b = p.Add<Bloom>(true); b.intensity.Override(1.6f); b.threshold.Override(0.7f); b.scatter.Override(0.85f);
            });
            var groggy = LocalVolume(zones, "Volume_Groggy", new Rect(RoomX0 - 10f, -10f, RoomX1 - RoomX0 + 20f, 30f), groggyProf, 0f, 60f);
            groggy.weight = 0f;
            // 방 기본 색감: 새벽의 차가운 푸른빛 + 따뜻한 조명이 번지게
            var roomProf = Profile("VP_CATest_Room", p => {
                var b = p.Add<Bloom>(true); b.intensity.Override(0.9f); b.threshold.Override(0.9f); b.scatter.Override(0.7f); b.tint.Override(C(1f, 0.85f, 0.75f));
                var v = p.Add<Vignette>(true); v.intensity.Override(0.32f); v.smoothness.Override(0.6f); v.color.Override(C(0.03f, 0.03f, 0.08f));
                var adj = p.Add<ColorAdjustments>(true); adj.postExposure.Override(0f); adj.contrast.Override(10f); adj.saturation.Override(-5f); adj.colorFilter.Override(C(0.92f, 0.94f, 1f));
            });
            LocalVolume(zones, "Volume_Room", new Rect(RoomX0 - 10f, -10f, RoomX1 - RoomX0 + 20f, 30f), roomProf, 6f);

            // 분위기(전체광) / 카메라 — 방이 작아졌으므로 카메라도 가깝게(거리 40 ≈ 화면 높이 9유닛)
            Zone(zones, "Room", new Rect(RoomX0 - 8f, -6f, RoomX1 - RoomX0 + 16f, 24f), 6f, C(0.6f, 0.66f, 0.92f), 0.6f, C(0.02f, 0.02f, 0.03f), 3);
            CamZone(zones, "Room", new Rect(RoomX0 - 8f, -6f, RoomX1 - RoomX0 + 16f, 24f), 4f, 40f, new Vector3(1.6f, 1.7f, 0f), new Vector3(1.2f, 1.2f, 1f), 3);

            // ── 프롤로그 연출 ──
            var pro = new GameObject("PrologueDirector").AddComponent<CATestPrologue>();
            pro.transform.SetParent(gameplay, false);
            // 누운 몸 중심: 베개(설계 x -2397.2)에 머리 → 몸 길이(약 2.5)의 절반만큼 오른쪽, 매트리스 윗면(설계 y 1.22) + 몸 두께 절반
            Set(pro, "bedCenter", new Vector2(X(-2397.2f) + 1.25f, Y(1.22f) + 0.45f));
            Set(pro, "standPoint", PrologueStart);
            Set(pro, "doorX", RoomDoorX);
            Set(pro, "crevicePoint", CrevicePath[0]);
            Set(pro, "door", roomDoor);
            Set(pro, "doorPanel", hinge);
            Set(pro, "doorPanelSr", panel);
            Set(pro, "doorLight", doorLight);
            Set(pro, "doorGlow", doorGlow);
            Set(pro, "doorLight2D", doorL2D);
            Set(pro, "gapLight", gapLight);
            Set(pro, "pullMotes", pull);
            Set(pro, "blanket", blanket);
            Set(pro, "groggyVolume", groggy);
            Set(roomDoor, "prologue", pro);
            _ = rnd;
            _ = fg;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, PrologueScenePath);
        }
    }
}
#endif
