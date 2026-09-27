# 빛 반사 없는 반복 수중 타일

FantasyWaterDepthV2.unitypackage를 임포트합니다. Assets/FantasyWaterDepthV2/Prefabs/WaterDepthRepeat.prefab이 아래로 계속 반복하는 일반 깊이 타일입니다. 기본 크기는 가로 12, 깊이 6이며 피벗은 위쪽 중앙입니다. Scale=1 기준 Y를 6씩 내려 배치하세요. 예: -9, -15, -21. 가로는 12씩 이동해 연결할 수 있습니다. 루트의 Sorting Group에서 순서를 조절합니다.

FantasyWaterDeepV2.prefab은 기존 수면과 첫 연결용 깊이 타일을 합쳤습니다. 이 프리팹을 Y=0에 배치했다면, 그 아래 반복 타일의 Y 위치는 -9부터 시작합니다. 첫 연결 부분을 따로 쓰려면 WaterDepthConnector.prefab을 사용합니다. 기존 물 Y=0, Scale=1 기준 Connector Y=-3이며, 기존 물 재질 Bottom Fade=0, 양쪽 Opacity 동일 설정이 필요합니다.

일반 반복 타일에 Surface Connection을 켜면 층마다 경계가 생기므로 Repeat 재질의 Surface Connection은 0으로 유지하세요. 아래쪽을 점점 어둡게 하면 타일마다 밝기가 되돌아오므로 반복용 타일은 균일한 남색으로 제작했습니다. 깊이에 따른 어둠을 추가하려면 여러 층을 함께 덮는 별도 그라데이션을 사용하세요.

수중 원화에서 빛줄기와 반사광을 제거했습니다. 기존 수면 원화는 보존했습니다. 셰이더의 좌우 및 위아래 반전 반복으로 경계 샘플이 일치합니다. 같은 Animator 시작 시점과 같은 크기로 배치해야 움직임도 연결됩니다. 4초 반복, 런타임 C# 및 충돌 없음. PNG는 불투명 원화이며 재질에서 투명도를 조절합니다.

원화는 내장 이미지 생성 도구로 수정했고 generation_prompts.txt에 프롬프트를 저장했습니다. Unity에서 3개 깊이 구간을 렌더해 연결 모습을 확인했습니다. Play Mode 검증은 별도로 수행하지 않았습니다.
