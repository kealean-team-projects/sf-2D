using System.Collections.Generic;

namespace LHS_CATest {
    /// <summary>
    /// ★ 대사 모음 ★ — 플레이어 머리 위에 뜨는 모든 대사를 이 파일 한 곳에서 관리한다.
    ///
    /// 고치는 방법
    ///  - 문장만 바꾸려면 아래 문자열만 수정하면 된다(키 이름은 그대로 둘 것).
    ///  - 한 항목에 여러 줄을 넣으면 순서대로 한 줄씩 말한다.
    ///  - 맵에 배치된 CATestSayZone 은 "키 이름"으로 대사를 찾는다. 키를 바꾸면 맵 빌더(CATestBuilder.*.cs)의 SayZone 호출도 같이 바꿔야 한다.
    ///
    /// 설정: 주인공은 현실 세계에서 갑자기 이 세계로 떨어졌다. 아직 현실 파트는 넣지 않았으므로
    ///       "원래 있던 곳"은 구체적으로 말하지 않는다.
    /// </summary>
    public static class CATestLines {
        public static readonly Dictionary<string, string[]> All = new() {
            // ── 오프닝 컷신 ──
            ["intro_fall"] = new[] { "……으윽." },
            ["intro_land"] = new[] { "……여긴 어디지..?", "분명 방금 전까지… 다른 곳에 있었는데.", "일단… 움직여 보자." },

            // ── 화창한 숲 (튜토리얼) ──
            ["sunny_gap"] = new[] { "꽤 먼데… 달리면서 뛰면 닿을까?" },
            ["sunny_crouch"] = new[] { "몸을 숙이면 지나갈 수 있겠어." },
            ["sunny_vine"] = new[] { "덩굴이 튼튼해 보여. 잡고 올라갈 수 있을지도." },
            ["sunny_chimney"] = new[] { "벽 사이가 좁아… 벽을 차고 오르면 되겠다." },
            ["sunny_lever"] = new[] { "레버…? 누가 만들어 둔 걸까." },
            ["sunny_crate"] = new[] { "이 상자를 밀면 발판이 되겠어." },
            ["sunny_river"] = new[] { "강이다. 물살이 세 보여… 빠지지 않게 조심하자." },
            ["sunny_river_fall"] = new[] { "푸핫…! 떠내려갈 뻔했어." },
            ["sunny_river_done"] = new[] { "휴… 건넜다." },
            ["sunny_puzzle"] = new[] { "문이 닫혀 있어. 뭔가 방법이 있을 텐데." },
            ["sunny_exit"] = new[] { "숲이 점점 울창해지고 있어." },

            // ── 울창한 숲 / 안개 ──
            ["lush_enter"] = new[] { "나무가 하늘을 다 가렸어…" },
            ["lush_canopy"] = new[] { "높다… 발밑 조심하자." },
            ["fog_enter"] = new[] { "안개가… 앞이 잘 안 보여." },
            ["fog_lamp"] = new[] { "불빛이 닿는 곳에만 발판이 보여." },

            // ── 몽환의 숲 / 산 ──
            ["dream_enter"] = new[] { "이상해… 공기가 반짝이는 것 같아." },
            ["mountain_enter"] = new[] { "바람이 거세. 여기서 떨어지면 끝이야." },
            ["summit_pit"] = new[] { "저 아래… 깊은 구멍이 있어." },

            // ── 심해 ──
            ["sea_land"] = new[] { "물속인데… 숨이 쉬어져?" },
            ["sea_passage"] = new[] { "몸이 천천히 가라앉아. 멀리 뛸 수 있겠어." },
            ["boss_intro"] = new[] { "…!", "저건… 눈?", "저 빛에 들키면 안 될 것 같아." },
            ["boss_hide"] = new[] { "바위 밑이나 해초 속에 숨자." },
            ["boss_rage"] = new[] { "……!!", "화났어… 도망쳐야 해!" },
            ["boss_tunnel_hint"] = new[] { "저기 틈…! 웅크리면 들어갈 수 있어!" },
            ["boss_escape"] = new[] { "여기라면… 못 들어올 거야." },
            ["boss_gone"] = new[] { "……갔나?", "살았다…" },
            ["ascent"] = new[] { "위쪽이 밝아. 올라가 보자." },
            ["ascent_top"] = new[] { "빛이다…!" },

            // ── 마지막 챕터: 황혼의 성역 ──
            ["realm_enter"] = new[] { "빛이… 나를 끌어당기고 있어." },
            ["realm_arrive"] = new[] { "……여긴…", "하늘에 검은 해가 떠 있어." },
            ["canyon_enter"] = new[] { "전부 하얀 돌이야. 누가 깎아 놓은 것 같아." },
            ["canyon_needles"] = new[] { "바닥의 가시… 절대 떨어지면 안 돼." },
            ["canyon_crumble"] = new[] { "발판에 금이 가 있어. 오래 서 있으면 무너질 거야." },
            ["canyon_wind"] = new[] { "모래바람이야. 몸을 숙이고 버티자." },
            ["sanctum_enter"] = new[] { "사원…? 여기가 성역인가 봐." },
            ["sanctum_mural"] = new[] { "문 위의 그림… 순서대로 빛나고 있어." },
            ["sanctum_rune_wrong"] = new[] { "틀렸나 봐. 처음부터 다시…" },
            ["sanctum_rune_solved"] = new[] { "문이 열린다…!" },
            ["sanctum_beam"] = new[] { "하늘에서 빛이 떨어져… 바닥에 원이 생기면 피해야 해." },
            ["sanctum_float"] = new[] { "떠 있는 석판… 타고 건너야겠어." },
            ["sanctum_top"] = new[] { "저 너머에서… 무언가 기다리고 있어." },
            ["nest_arrive"] = new[] { "여기가… 끝인가." },

            // ── 엔딩: 수호룡 ──
            ["dragon_intro"] = new[] {
                "……멈춰라, 작은 것이여.",
                "여기는 성역… 인간이 함부로 발을 들일 수 없는 곳.",
                "무엇을 위해 이곳까지 온 것인가. 다른 세계에서 온 이방인이여.",
                "네가 떨어져 내린 그 틈을, 나는 알고 있다. 저 검은 해가 네 세계와 이곳을 잇는 문이니.",
                "선택하라. 돌아갈 것인가, 머물 것인가.",
            },
            ["dragon_choices"] = new[] {
                "원래 세계로… 돌아가고 싶어요.",
                "…이곳에 남을래요.",
                "(용에게 맞선다)",
            },
            ["ending_a_dragon"] = new[] {
                "좋다. 문을 열어 주마.",
                "이곳에서 본 것들은 꿈처럼 흐려지겠지. …그래도 잊지 마라, 작은 것이여.",
            },
            ["ending_a_player"] = new[] { "……고마워요." },
            ["ending_a_epilogue"] = new[] {
                "눈을 뜨자, 익숙한 천장이 보였다.",
                "창밖에는 평범한 저녁노을이 지고 있었다.",
                "…주머니 속에, 하얀 돌 조각 하나가 남아 있었다.",
            },
            ["ending_b_dragon"] = new[] {
                "……스스로 문을 닫겠다는 것인가.",
                "좋다. 황혼이 끝나지 않는 이 땅이, 이제 너의 집이다.",
            },
            ["ending_b_player"] = new[] { "…이상하게, 무섭지 않아." },
            ["ending_b_epilogue"] = new[] {
                "그날 이후, 성역에는 작은 발자국 하나가 늘었다.",
                "돌아갈 문은 다시 열리지 않았지만—",
                "그녀는 더 이상 길을 잃었다고 생각하지 않았다.",
            },
            ["ending_c_player"] = new[] { "…비켜. 난 내 힘으로 나갈 거야." },
            ["ending_c_dragon"] = new[] { "……어리석은." },
            ["ending_c_epilogue"] = new[] {
                "신에게 맞선 자의 이야기는,",
                "언제나 같은 문장으로 끝난다.",
                "— 그리고, 아무도 그녀를 기억하지 못했다.",
            },
        };

        public static string[] Get(string key) =>
            key != null && All.TryGetValue(key, out var lines) ? lines : new[] { key ?? string.Empty };
    }
}
