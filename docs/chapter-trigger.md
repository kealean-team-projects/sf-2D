# 챕터 교체 트리거

1. `Assets/03. Prefabs/ChapterLoadTrigger.prefab`을 현재 맵의 출구에 배치한다.
2. `BoxCollider2D`의 Size와 Offset으로 진입 구역을 조절한다. Is Trigger는 자동으로 켜진다.
3. `ChapterLoadTrigger > Target Chapter`를 지정한다. 기본값은 2다.
4. 빌드에 등록된 LHS의 CoreScene에서 시작해 확인한다.

현재 CoreScene의 챕터 목록은 1=FirstMap, 2=SecondMap, 3=ThirdMap이다. 번호는 Build Settings 전체 씬 인덱스가 아니라 ChapterLoader의 Chapter Scene Lists 순서다.

살아 있는 Player 컴포넌트를 가진 오브젝트 또는 그 자식 Collider2D만 감지한다. Player 태그에 의존하지 않는다. 플레이어 Rigidbody2D와 트리거 레이어 사이의 물리 충돌은 허용되어 있어야 한다.

진입하면 ChapterLoader가 현재 챕터를 언로드하고 대상 챕터를 Additive로 로드한 뒤 활성 씬으로 지정한다. CoreScene은 유지한다. 로딩 중 재진입/여러 콜라이더의 중복 요청은 무시한다. 잘못된 번호나 Scene List에 없는 씬은 현재 챕터를 내리기 전에 거부한다.

새 맵의 StageAudio가 기존 SoundManager에 BGM 교체를 요청하므로 트리거에서 사운드를 별도로 호출하지 않는다. 이번 변경으로 챕터 이름 목록을 직렬화해 빌드에서도 사용할 수 있게 했고, 에디터 전용 SceneAsset 참조는 빌드에서 제외했다.

출구 위치는 지정되지 않았으므로 프리팹만 만들었으며 맵에는 배치하지 않았다.

검증: 전체 코드 컴파일 오류 0개. 격리 Unity 환경에서 실제 프리팹을 불러오고 2D 물리 진입으로 MainMenu→FirstMap, 이어서 SecondMap 교체를 확인했다. 이전 씬 언로드, Core 유지, BGM 전환, 비플레이어/사망 플레이어 거부, 잘못된 대상 거부, 중복 로드 방지 등 챕터 관련 14개 단언이 통과했다. 기존 오디오 검증까지 총 45개 통과. 실제 맵 출구 배치 후 전체 플레이 검증은 별도다.
