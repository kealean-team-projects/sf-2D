# 사운드 연결과 확인 방법

## 적용된 구성

- `Assets/00. Member/LHS/Scene/CoreScene.unity`의 `SoundManager`가 기존 SoundListSO와 AudioMixer로 모든 소리를 재생한다.
- `FirstMap`의 `StageAudio`는 `BG_1`, `SecondMap`의 `StageAudio`는 `BG_2`를 요청한다. `ChapterLoader`는 변경하지 않았다.
- 공통 `Player.prefab`의 `PlayerAudio`가 걷기·달리기·착지를 처리한다.
- `SecondMap`의 이름이 지정된 지면과 동굴 지면 50개에 `FootstepSurface = Stone`을 지정했다. 다른 지면은 기본 재질이다. 실제 아트 의도와 다른 지면은 해당 컴포넌트에서 변경한다.

## Unity에서 조정하기

걷기·달리기의 바닥 감지 범위는 `PlayerAudio > Footsteps > Footstep Detection Distance`에서 별도로 조절한다. 현재 값은 0.1 Unity 단위다. 값을 키우면 발밑의 작은 틈에서도 이동음을 재생하고, 0이면 기존 접지 판정으로 돌아간다. 상승 중에는 확장 감지를 사용하지 않으며 실제 이동·입력 조건은 유지한다. 실제 충돌·착지음 감지 거리·최소 추락 시간에는 영향을 주지 않는다. 이 설정은 음원 자체나 장치의 출력 지연을 바꾸지 않는다.

1. 빌드에 등록된 LHS의 `CoreScene`에서 실행하고 메뉴를 통해 맵에 진입한다. 다른 이름의 CoreScene이나 맵 단독 실행에는 공용 매니저가 없을 수 있다.
2. 맵 Hierarchy의 `StageAudio`에서 `Background Music`을 등록된 사운드 이름으로 지정한다.
3. 돌바닥 Collider2D 오브젝트 또는 그 부모에 `FootstepSurface`를 붙이고 `Stone`을 선택한다. 기존 표식을 `Default`로 바꾸면 기본 소리로 돌아간다. 맵 번호로 재질을 결정하지 않는다.
4. 사운드 창은 `Window > CSILib > SoundManager`다. 개별 사운드 설정의 `Volume`을 조절한다. Walk/Run 계열은 Loop를 켜고 Landing 계열은 끈다.
5. 전체 BGM/SFX 음량은 기존 AudioMixer와 VolumeSlider를 사용한다.
6. `PlayerAudio > Landing > Landing Detection Distance`로 착지음 감지 범위를 아래로 늘릴 수 있다. 기본값은 0.15 Unity 단위이며, 값이 클수록 먼저 소리가 난다. 0이면 기존 착지 시점으로 돌아간다. 최소 추락 시간 조건은 유지하며, 미리 재생하면 실제 착지에서 다시 재생하지 않는다. 실제 충돌·접지·발소리 판정에는 영향을 주지 않는다.
7. Player의 `PlayerAudio > Landing > Minimum Fall Time`에서 착지음에 필요한 최소 연속 추락 시간(초)을 조절한다. 현재 Player 프리팹의 사용자 설정은 0.5초로 유지했다. 0이면 추락 시간 제한이 없고, 상승·등반 시간은 제외하며 사망·리스폰 시 누적 시간을 초기화한다. Landing과 Landing_2에 동일하게 적용된다.

## 기대 동작

| 상황 | 소리 |
| --- | --- |
| 기본 바닥 걷기 / 달리기 | Walk / Run |
| 돌바닥 걷기 / 달리기 | Walk_2 / Run_2 |
| 기본 / 돌바닥 착지 | 최소 추락 시간 이상일 때 Landing / Landing_2 한 번 |
| 정지·벽에 막힘·공중·사망 | 발소리 정지 |
| 초기 생성·리스폰 | 착지음 생략 |
| 1번 → 2번 스테이지 | BG_1 정지 후 BG_2 반복 |
| 맵 종료 | 해당 맵의 BGM 정지 |

## 내부 재생 계약

기존 `PlaySound(string)` 호출은 유지된다. 반복 효과음은 `PlayTrackedSound`로 받은 핸들을 `StopSound`에 전달해 종료한다. BGM은 `PlayBgm(name, owner)` / `StopBgm(owner)`를 사용한다. 이전 맵이 종료되더라도 새 맵 소유의 음악은 정지하지 않는다.

반환된 AudioSource에 직접 Pause/Stop/Destroy를 호출하지 말고 매니저를 통해 종료한다. 개별 일회성 소리의 Pause/UnPause는 이번 범위에 포함하지 않는다. 전역 AudioListener.pause 동안은 일회성 소리의 정리를 보류한다.

## 검증 범위

전체 변경 코드를 기존 프로젝트 참조와 함께 별도 검증용 프로젝트로 컴파일한다. 실제 Unity 엔진에서 실행하는 격리 검증은 운영 사운드/접지/플레이어 오디오 코드를 사용하며, 플레이어 상태 입력만 최소 대역으로 제공한다. 이 검증은 실제 두 맵 전체의 수동 플레이나 음원 청취를 대체하지 않는다.
