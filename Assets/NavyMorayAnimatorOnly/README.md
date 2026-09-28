# 남색 곰치 접촉 공격 패키지

이 패키지는 기존 Animator와 원화를 유지하면서, 별도 발동 구역에서 플레이어가 감지된 뒤 곰치 공격 애니메이션을 반복 재생하고 공격 중 접촉한 플레이어에게 기존 `DamageModule.TakeDamage()` 사망 처리를 호출합니다.

## 사용

1. 패키지를 Unity 프로젝트에 임포트하고 `Prefabs/NavyMorayAnimatorOnly.prefab`을 씬에 배치합니다.
2. 프리팹 자식 `ActivationTrigger`의 Box Collider 2D를 이동하고 크기를 조절해 첫 발동 구역을 정합니다. 이 구역 자체는 피해를 주지 않습니다.
3. 루트의 `NavyMorayAttackCycle`에서 `Initial Delay`와 `Cooldown After Attack`을 조절합니다. 기본값은 첫 발동 후 0.5초, 공격 종료 후 4초입니다.
4. 플레이어 오브젝트 또는 상위 오브젝트에 프로젝트의 `DamageModule`이 있어야 하며, 플레이어 Rigidbody 2D와 충돌 가능한 레이어여야 합니다.

## 동작

- ActivationTrigger에 플레이어가 처음 들어오면 초기 지연 후 `Start_Head → Loop_Body → End_Tail` 애니메이션이 재생됩니다.
- Animator의 `HoldBody`와 `Repeat`를 코드에서 false로 설정해 한 번의 공격 애니메이션으로 끝나게 한 뒤, 설정한 쿨타임 후 다시 재생합니다. 별도 종료 트리거 없이 루프를 계속합니다.
- 공격 구간 동안만 각 활성 SpriteRenderer의 Sprite physics shape를 따라 생성한 Polygon Collider 2D가 trigger로 켜집니다. 애니메이션 그룹이 비활성화된 몸통은 자동으로 접촉 판정에서 빠집니다.
- 같은 공격 주기 안에서 플레이어당 사망 호출은 한 번으로 제한합니다. 다음 주기에는 다시 판정합니다.
- ActivationTrigger는 별도 자식이므로 위치와 크기를 공격 접촉 판정과 독립적으로 조절할 수 있습니다.

애니메이션의 실제 재생 시간은 기존 Animator Clip의 길이를 따릅니다. Unity Editor에서 프리팹을 임포트한 뒤 Trigger 배치와 플레이어 접촉 동작을 확인하세요.
