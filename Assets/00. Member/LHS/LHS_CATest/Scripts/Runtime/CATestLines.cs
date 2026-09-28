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
            ["intro_land"] = new[] { "……여긴 어디지..?", "숲..? …분명 방금 전까지… 학교에 있었는데.", "일단… 주변을 살펴보자." },

            // ── 화창한 숲 (튜토리얼) ──
            ["sunny_gap"] = new[] { "꽤 먼데… 달리면서 뛰면 닿을까?" },
            ["sunny_crouch"] = new[] { "몸을 숙이면 지나갈 수 있겠어." },
            ["sunny_vine"] = new[] { "덩굴이 튼튼해 보여. 잡고 올라갈 수 있을지도?" },
            ["sunny_chimney"] = new[] { "벽 사이가 좁아… 벽을 차고 올라가야 할 것 같아.." },
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
            ["sea_land"] = new[] { "여긴.. 물 속..?" },
            ["sea_passage"] = new[] { "몸이 천천히 가라앉아. 멀리 뛸 수 있겠어." },
            ["boss_intro"] = new[] { "…!", "저건… 눈?", "저 빛에 들키면 안 될 것 같아." },
            ["boss_hide"] = new[] { "바위 밑이나 해초 속에 숨자." },
            ["boss_rage"] = new[] { "……!!", "화났어… 도망쳐야 해!" },
            ["boss_tunnel_hint"] = new[] { "저기 틈…! 웅크리면 들어갈 수 있어!" },
            ["boss_escape"] = new[] { "여기라면… 못 들어올 거야." },
            ["boss_gone"] = new[] { "……갔나?", "살았다…" },
            ["ascent"] = new[] { "위쪽이 밝아. 올라가 보자." },
            ["ascent_top"] = new[] { "빛이다…!" },
        };

        public static string[] Get(string key) =>
            key != null && All.TryGetValue(key, out var lines) ? lines : new[] { key ?? string.Empty };
    }
}
