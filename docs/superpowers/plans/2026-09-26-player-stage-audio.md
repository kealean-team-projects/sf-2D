# Player and Stage Audio Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** 등록된 Walk/Run/Landing 및 _2 변형과 BG_1/BG_2를 실제 플레이에 연결한다.

**Architecture:** 기존 SoundManager와 SoundSo를 재사용하며 별도 AudioManager를 만들지 않는다. SoundManager가 BGM 하나의 재생·교체·정지를 관리하고 모든 효과음을 재생한다. PlayerAudio는 플레이어 상태에 따라 효과음을 요청하고, 각 맵의 StageAudio는 로드 시 지정된 BGM으로 교체를 요청한다.

**Tech Stack:** Unity, C#, AudioSource, AudioMixer, 기존 CSILib SoundManager.

**확정된 배치 방식:** 각 맵에 StageAudio 컴포넌트를 배치한다. ChapterLoader에는 오디오 호출을 추가하지 않는다. FirstMap은 BG_1, SecondMap은 BG_2를 Inspector에서 지정한다. 맵 로드 시 SoundManager에 교체를 요청하고, 맵 종료 시 자신이 요청한 BGM이 여전히 현재 곡일 때만 정지를 요청한다. StageAudio는 별도의 AudioSource나 재생 시스템을 만들지 않는다.

## Evidence and estimate

- Walk/Run 및 _2는 loop=true, Landing 계열은 false, BG 계열은 true이며 클립이 연결되어 있다.
- SoundManager에는 재생 메서드만 있고 반환 핸들/정지가 없다.
- 저장된 씬/프리팹 검색에서 SoundManager 컴포넌트는 패키지 Demo에서만 확인되었다. 저장 전 Unity 상태와 실행 중 생성 여부는 구현 시 확인한다.
- ChapterLoader는 CoreScene을 유지하고 맵을 Additive 로드/언로드한다. 활성 씬 이름만 조회하는 초기화는 피한다.
- FirstMap, SoundListSO, 음원 파일에 사용자 변경이 있으므로 보존한다.
- 잠정 작업 추정: 코드와 연결 50~85분, 플레이 검증/조정 20~35분, 총 70~120분. 담당은 구현 개발자. 실제 속도 측정이 아닌 코드 조사 기반이며 신뢰도 중간 이하. 맵 콜라이더 구성 및 Unity 검증 접근성 확인 후 갱신한다.
- 돌바닥이 별도 Collider/Tilemap으로 구분되어 있다는 가정. 한 Tilemap에 재질이 섞이면 타일별 판별 추가로 약 20~40분 증가할 수 있다.

## Task 1: Existing playback lifecycle

Modify: Assets/csiimnida/CSILib/SoundManager/RunTime/SoundManager.cs

- [x] 기존 PlaySound(string) 호출 호환성을 유지하면서 호출자가 AudioSource 핸들을 받는 별도 재생 경로를 추가한다.
- [x] BGM 전용 교체 요청을 추가한다. 기존 BGM을 정리하고 새 BGM 하나만 반복 재생한다. 같은 소유자의 같은 곡 요청은 중복 재생하지 않는다.
- [x] BGM 요청 소유자를 기록해 이전 맵의 종료 요청이 새 맵의 BGM을 끄지 않게 한다. BGM 교체는 플레이어 효과음에 영향을 주지 않는다.
- [x] 각 호출자는 자신이 시작한 소리만 정지/제거한다. 같은 이름 전체 정지는 사용하지 않는다.
- [x] 이름/목록/클립 누락은 경고 후 재생을 생략하고 빈 객체가 남지 않게 한다.
- [x] 믹서 그룹 누락은 예외 없이 기본 출력으로 재생한다.
- [x] 랜덤 피치는 SoundSo를 변경하지 않고 해당 AudioSource에 적용한다. 비반복 소리 수명은 실제 재생 종료를 따른다.
- [x] 두 소리를 함께 재생하고 하나만 정지했을 때 다른 소리는 유지되는지 검증한다.

## Task 2: Ground material

Create: Assets/02. Script/01_Players/Components/Audio/FootstepSurface.cs
Modify: Assets/02. Script/01_Players/Components/Mover.cs

- [x] Default/Stone 재질을 지정하는 컴포넌트를 추가한다.
- [x] 기존 CheckGround에서 접촉 콜라이더 정보를 노출한다. 이동/점프 수치는 변경하지 않는다.
- [x] 복수 접촉에서는 발 아래 지지면을 우선하며 트리거를 제외한다. 재질 표식이 없으면 Default를 사용한다.
- [x] 돌 표식이 붙은 바닥 및 기본 바닥 경계에서 재질 선택을 확인한다.

## Task 3: Player audio

Create: Assets/02. Script/01_Players/Components/Audio/PlayerAudio.cs
Modify: Assets/03. Prefabs/Player.prefab

- [x] Player, Mover, Rigidbody2D 참조를 연결한다.
- [x] 접지 + 실제 수평 이동 + 생존/이동 가능 상태에서만 발소리를 재생한다. 입력만 있고 벽에 막힌 상태는 제외한다.
- [x] SprintControl.IsSprinting과 바닥 재질로 Walk/Run/Walk_2/Run_2를 선택한다.
- [x] 선택한 소리가 바뀔 때만 이전 반복음을 정지하고 새 반복음을 시작한다.
- [x] 정지, 공중, 등반, 사망, 비활성화 시 발소리를 종료한다.
- [x] 이전 비접지에서 접지로 전환될 때 Landing 또는 Landing_2를 한 번 재생한다. 초기 생성 및 리스폰은 착지로 처리하지 않는다.
- [x] 기존 복귀 경로에서 오디오 상태를 재설정할 필요가 있으면 Assets/02. Script/01_Players/Player.cs의 RestoreState에 최소한의 통지만 추가한다.

## Task 4: Stage music and scene wiring

Create: Assets/02. Script/05_Managers/StageAudio.cs
Modify: Assets/00. Member/LHS/Scene/CoreScene.unity
Modify: Assets/00. Member/tyu/FirstMap.unity
Modify: Assets/00. Member/tyu/SecondMap.unity

- [x] CoreScene의 공용 SoundManager 존재 여부를 Unity에서 재확인하고 하나만 배치한다. SoundListSO와 AudioMixer를 연결한다.
- [x] StageAudio에 곡 이름을 지정한다. FirstMap=BG_1, SecondMap=BG_2.
- [x] StageAudio는 맵 로드 후 Start에서 SoundManager에 지정한 곡으로 BGM 교체를 요청한다. 비활성화/언로드 시 자신을 소유자로 전달해 정지를 요청한다. 실제 재생과 정리는 SoundManager가 담당한다.
- [x] TempSoundPlayer가 같은 배경음을 재생하는지 확인하고 중복 호출을 제거한다. 기존 데모 동작은 유지한다.
- [x] 각 맵의 돌 지지면에 Stone 표식을 배치한다. _2를 맵 번호로 고정하지 않는다.
- [x] 새 Unity 파일의 .meta를 포함하고 사용자의 음원/씬 수정 내용을 보존한다.

## Task 5: Verification and completion

- [x] 전체 변경 코드를 기존 Unity 참조로 컴파일: 오류 0개. 격리 Unity 프로젝트에서도 실제 소스 컴파일 및 실행 통과.
- [ ] 기본 바닥 걷기/달리기, 돌바닥 걷기/달리기에서 각 소리가 하나씩만 재생되는지 확인한다.
- [ ] 이동 중 정지/점프/벽 막힘/등반에서 발소리가 멈추는지 확인한다.
- [ ] 착지는 재질에 맞춰 한 번만 들리고 생성/리스폰에서는 발생하지 않는지 확인한다.
- [ ] 사망/재시작 반복 후 AudioSource 객체 수가 계속 늘지 않는지 확인한다.
- [ ] FirstMap→SecondMap→메뉴에서 이전 BGM이 남지 않는지 확인한다.
- [ ] 같은 맵 재로드 및 반복 교체 요청에서 BGM이 중복되지 않고, 이전 맵 정리 호출이 새 BGM을 끄지 않는지 확인한다.
- [ ] 개별 SoundSo 볼륨과 기존 VolumeSlider/믹서 설정을 함께 확인한다.
- [ ] 음량 청감 조정은 실제 청취 결과로 기록한다. 실행/청취가 불가능하면 확인하지 못한 항목을 명시한다.

## Scope boundary

이번 범위는 1·2 스테이지 BGM, 바닥별 걷기/달리기/착지다. ThirdMap 음악, 새 음원 제작, 크로스페이드, 추가 행동 효과음은 포함하지 않는다. 기존 이동 물리와 UI 볼륨 조절 구조는 유지한다.

## 실행 결과 (2026-09-26)

- 구현과 직렬화 연결 완료. ChapterLoader는 수정하지 않았다.
- 기존 FirstMap 수정 보존을 적용 전 백업과 비교 확인했다.
- CoreScene 공용 SoundManager 1개, 각 맵 StageAudio, 공통 PlayerAudio를 배치했다.
- SecondMap의 이름이 지정된 지면/동굴 지면 50개에 Stone을 지정했다.
- 격리 Unity 런타임 검증 25개 통과: BGM 교체·소유권·중복 방지, 기본/돌 발소리, 착지, 리스폰, 정지, 일시정지, 재생 완료 정리. 플레이어 상태는 최소 대역으로 입력했다.
- 전체 컴파일 오류 0개, 기존 외부 패키지 참조/폐기 API 관련 경고 15개.
- 전체 맵 수동 플레이 및 실제 음원 청취·볼륨 밸런스 조정은 수행하지 않았다. Task 5의 수동 플레이 항목은 후속 확인용으로 남긴다.
- 자세한 사용법: docs/audio-setup.md. 검증 결과: docs/audio-validation.md.
