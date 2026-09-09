# Player Movement Modules Design

## Goal

`Player`와 현재의 거대한 `Mover`에 섞여 있는 플레이어 이동 로직을 MonoBehaviour 기반 모듈로 분리한다. 각 모듈은 자신의 튜닝 변수와 물리 동작을 직접 소유하며, `PlayerMovementController`는 모듈의 초기화, 실행 순서, 상호 배제만 담당한다.

## Confirmed Scope

이번 리팩터링에서 만드는 이동 모듈은 다음 여섯 개다.

1. `MoveModule`: 일반 좌우 이동과 일반 지상 점프
2. `AirModule`: 체공 시간과 추가 낙하 중력
3. `ClimbModule`: 벽타기와 벽에서 내려오기/이탈하기
4. `WallJumpModule`: 벽점프
5. `WallDashModule`: 벽 위쪽 대시
6. `DashModule`: 기존 `MotionType.Dash`의 독립 경계

`MoveModule`과 일반 `JumpModule`은 분리하지 않는다. 일반 점프는 `MoveModule`이 함께 소유한다.

다음 기능은 이번 이동 모듈 목록에 포함하지 않는다.

- 접지/벽 Checker: 기존 감지 방식을 유지하고 모듈은 결과만 읽는다.
- `SprintController`, `CrouchController`, `FacingController`: 현재 분리 구조를 유지한다.
- `PushForce()`: 피격과 외부 힘에서도 쓰일 수 있으므로 `Mover`의 저수준 물리 기능으로 유지한다.
- 상호작용과 `StaminaHUD`: `Player`에 유지한다.

## Current Code Mapping

현재 `MotionType` 및 `Mover` 로직은 아래와 같이 이동한다.

| 현재 코드 | 새 소유자 |
| --- | --- |
| `MotionType.ManualMove`, `_moveInput`, `speed` | `MoveModule` |
| `Jump()`, `jumpForce` | `MoveModule` |
| `MotionType.Fall`, `_timeInAir`, `extraGravity`, `gravityDelay` | `AirModule` |
| `MotionType.Climb`, `climbUpSpeed`, `climbDownSpeed`, `_climbInput` | `ClimbModule` |
| `_climbable`, `CancelClimb()`, 2초 재부착 제한 | `ClimbModule` |
| `MotionType.WallJump`, X/Y 힘, 지속시간 | `WallJumpModule` |
| `MotionType.WallDash`, 상승 힘, 지속시간 | `WallDashModule` |
| `MotionType.Dash` | `DashModule` |
| Rigidbody 참조, 기본 중력값, 접지 판정, `PushForce()` | 축소된 `Mover` |

`Player`의 벽 관련 스태미나 설정도 해당 기능으로 이동한다.

| 현재 Player 필드 | 새 소유자 |
| --- | --- |
| `useStaminaInWall` | `ClimbModule` |
| `useStaminaInWallJump` | `WallJumpModule` |
| `useStaminaInWallDash` | `WallDashModule` |
| `useStaminaInRun` | 기존 Sprint 구조에 유지 |

## Architecture

```text
Player
└─ PlayerMovementController
   ├─ MoveModule          (좌우 이동 + 일반 점프)
   ├─ AirModule           (체공 시간 + 추가 중력)
   ├─ ClimbModule         (벽타기 + 이탈 제한)
   ├─ WallJumpModule      (벽점프)
   ├─ WallDashModule      (벽 위쪽 대시)
   └─ DashModule          (현재는 비활성 경계)

Shared components
├─ Mover                  (Rigidbody + 기본 중력 + 접지 판정 + PushForce)
├─ InputReader
├─ Stats
├─ CheckClimbWall
├─ SprintController       (기존 유지)
├─ CrouchController       (기존 유지)
└─ FacingController       (기존 유지)
```

### PlayerMovementController

`Player`가 참조하는 단 하나의 이동 진입점이다. `Setup`, `Tick`, `FixedTick`, `Release`를 제공하고 내부 이동 모듈을 실행한다.

- `Utils.Get<T>()`를 사용해 필요한 모듈을 Player 계층에서 한 번만 찾는다.
- 프레임마다 `Utils.Get<T>()`를 호출하지 않는다.
- 점프 입력을 한 곳에서 받아 일반 점프, 벽점프, 벽 대시, 벽 이탈 중 하나로 라우팅한다.
- 서로 동시에 Rigidbody를 덮어쓰는 이동을 실행하지 않는다.
- 모듈 등록 키로 enum을 사용하지 않는다. 기존 `MotionType`은 마이그레이션 완료 후 제거한다.

### PlayerMoveModule

모든 이동 모듈의 공통 MonoBehaviour 기반 클래스다.

- `Setup`: 공유 의존성과 Controller를 한 번 연결한다.
- `Tick`: 일반 프레임 로직을 처리한다.
- `FixedTick`: Rigidbody 변경을 처리한다.
- `Release`: 입력 이벤트 구독을 해제한다.
- 각 파생 모듈은 필요한 `[SerializeField]` 튜닝값을 직접 소유한다.

공유 참조를 Setup에서 캐시하는 것은 허용하지만, 다른 모듈의 튜닝값을 직접 수정하지 않는다. 모듈 간 전환 요청은 Controller를 통한다.

## Module Responsibilities

### MoveModule

일반 이동과 일반 지상 점프를 한 모듈로 묶는다.

- 소유 값: `speed`, `jumpForce`
- 최종 이동 입력에 `speed`를 곱해 Rigidbody X 속도를 설정한다.
- 최종 이동 입력은 기존 Sprint/Crouch 배율이 적용된 상태로 Controller에서 전달받는다.
- 일반 점프 요청 시 접지 상태를 확인하고 Y 속도를 정리한 뒤 점프 충격을 한 번 적용한다.
- 벽타기, 벽점프, 벽 대시가 활성화된 동안에는 물리값을 덮어쓰지 않는다.

### AirModule

기존 `Fall`과 `ApplyExtraGravity()` 책임을 가진다.

- 소유 값: `extraGravity`, `gravityDelay`
- 런타임 값: `timeInAir`
- 접지 중이거나 벽타기 중이면 공중 시간을 0으로 초기화한다.
- 공중 시간이 `gravityDelay`를 넘으면 추가 하강력을 적용한다.
- 일반 좌우 공중 제어는 `MoveModule`이 계속 담당한다.

### ClimbModule

벽 접촉 중의 등반과 강제 이탈 제한을 담당한다.

- 소유 값: `climbUpSpeed`, `climbDownSpeed`, `staminaCostPerSecond`, `reattachDelay`
- 런타임 값: `climbInput`, `canClimb`, `isActive`
- 활성 중 Rigidbody 중력을 0으로 만들고 Y 속도를 등반 입력으로 결정한다.
- 이동 중 스태미나를 초당 소모한다.
- 스태미나가 0이면 등반을 취소한다.
- 아래 입력으로 취소할 때 `reattachDelay` 동안 재부착을 막는다.
- 벽점프와 벽 대시가 활성화되면 제어권을 넘긴다.

### WallJumpModule

- 소유 값: `wallJumpXForce`, `wallJumpYForce`, `wallJumpDuration`, `staminaCost`
- 이동 입력 방향을 기준으로 벽 반대 방향을 결정한다.
- 충분한 스태미나가 있을 때 한 번만 비용을 차감한다.
- 시작 시 Rigidbody 속도를 벽점프 속도로 설정한다.
- `wallJumpDuration` 동안 일반 이동/등반의 물리 덮어쓰기를 막는다.
- 종료 후 일반 이동과 Air 동작으로 복귀한다.

### WallDashModule

- 소유 값: `wallDashSpeed`, `wallDashDuration`, `staminaCost`
- 벽에 붙은 상태에서 위 입력과 점프 요청이 함께 들어왔을 때 시작한다.
- 충분한 스태미나가 있을 때 한 번만 비용을 차감한다.
- 시작 프레임에만 위쪽 충격을 적용한다. FixedUpdate마다 새 비동기 작업을 생성하지 않는다.
- 지속시간이 끝나면 등반 가능 상태로 복귀한다.

### DashModule

현재 `MotionType.Dash`에는 동작, 입력, 튜닝값이 없다. 이번 리팩터링에서는 모듈 경계만 분리하되 새 게임플레이를 임의로 만들지 않는다.

- 기본 상태는 비활성이다.
- Player Prefab에 필수로 연결하지 않는다.
- 새 Dash 입력이나 임의의 속도/쿨타임을 추가하지 않는다.
- 실제 대시 규칙이 정해지면 별도 기능 설계 후 활성화한다.

## Execution and Priority Rules

Rigidbody를 독점하는 기능의 우선순위는 다음과 같다.

1. 활성 `WallDashModule`
2. 활성 `WallJumpModule`
3. 활성 `DashModule`
4. 활성 `ClimbModule`
5. 기본 `MoveModule`

`AirModule`은 독점 모듈이 아니다. 기본 이동 또는 벽점프 이후 공중 상태에서 추가 중력만 적용한다. Climb과 WallDash가 중력을 직접 제어하는 동안에는 실행하지 않는다.

점프 입력 라우팅은 현재 동작을 보존한다.

1. 벽 접촉 중이고 좌우 입력이 있으면 WallJump를 요청한다.
2. 벽 접촉 중이고 위 입력이 있으면 WallDash를 요청한다.
3. 벽 접촉 중이고 아래 입력이 있으면 Climb 취소를 요청한다.
4. 그 외 접지 중이면 MoveModule의 일반 점프를 요청한다.
5. 어느 조건에도 맞지 않으면 요청을 무시한다.

## Data Flow

초기화:

```text
Player.AfterInitialize
→ PlayerMovementController.Setup(Player)
→ Utils.Get<T>()로 공유 컴포넌트 조회
→ 각 이동 모듈 Setup
→ 이동 입력 이벤트 구독
```

프레임 업데이트:

```text
Player.Update
→ 기존 Sprint/Crouch/Facing/Stats 처리
→ PlayerMovementController.Tick
→ Air/Climb 및 활성 시간 갱신
```

물리 업데이트:

```text
Player.FixedUpdate
→ Sprint/Crouch가 반영된 최종 MoveInput 계산
→ PlayerMovementController.FixedTick(finalMoveInput, climbInput)
→ 우선순위에 맞는 독점 모듈 한 개 실행
→ 조건이 맞으면 AirModule 추가 중력 실행
```

해제:

```text
Player.OnDispose
→ PlayerMovementController.Release
→ 각 모듈 입력 이벤트 구독 해제
```

## Error Handling

- 필수 모듈 또는 공유 컴포넌트가 없으면 Setup에서 정확한 타입 이름을 포함한 오류를 한 번 출력하고 Controller를 비활성화한다.
- 필수 모듈은 `MoveModule`, `AirModule`, `ClimbModule`, `WallJumpModule`, `WallDashModule`이다.
- `DashModule`은 선택 사항이다.
- 동일 타입 모듈이 여러 개 발견되면 첫 번째를 임의 선택하지 않고 중복 오류를 출력한다.
- 입력 이벤트는 Setup과 Release가 같은 모듈에서 대칭으로 관리한다.
- 스태미나 비용은 동작 시작 시 한 번 또는 등반 중 초당 소모 중 하나로 명확히 구분한다.

## Migration Strategy

항상 플레이 가능한 상태를 유지하며 한 기능씩 옮긴다.

1. 공통 모듈 생명주기와 `PlayerMovementController`를 만든다.
2. 현재 일반 이동을 `MoveModule`로 옮기고 좌우 이동을 검증한다.
3. 일반 점프를 같은 `MoveModule`로 옮기고 점프를 검증한다.
4. 추가 중력과 공중 시간을 `AirModule`로 옮긴다.
5. 등반을 `ClimbModule`로 옮긴다.
6. 벽점프를 `WallJumpModule`로 옮긴다.
7. 벽 대시를 `WallDashModule`로 옮긴다.
8. 비활성 `DashModule` 경계를 추가한다.
9. 옮겨진 필드와 메서드를 기존 `Mover`와 `Player`에서 제거한다.
10. `MotionType` switch와 enum을 제거한다.

## Verification

각 모듈 이동 후 C# 빌드 오류가 0개인지 확인하고 Unity Play Mode에서 해당 기능을 검증한다.

- Move: 좌우 입력, 정지, Sprint/Crouch 배율 유지
- Jump: 지상에서만 점프, 공중 연속 점프 방지
- Air: 지연 이후 추가 중력, 착지 시 공중 시간 초기화
- Climb: 위/아래 속도, 중력 0, 스태미나 0일 때 취소, 재부착 지연
- WallJump: 방향, X/Y 힘, 비용 1회, 지속시간 종료 후 제어 복귀
- WallDash: 비용 1회, 충격 1회, 종료 후 Climb 복귀
- Regression: 상호작용, StaminaHUD, Sprint, Crouch, Facing이 기존처럼 동작
- Scene validation: Player의 필수 Controller/모듈 참조가 모두 연결됨

## Acceptance Criteria

- 각 이동 모듈의 튜닝 필드가 해당 모듈 Inspector에만 존재한다.
- `Player`는 개별 이동 구현 메서드를 가지지 않고 Controller 생명주기만 호출한다.
- `Mover`에는 Rigidbody 공유, 기본 중력, 접지 판정, `PushForce()`만 남는다.
- Checker, Sprint, Crouch, Facing 구조는 이번 작업에서 불필요하게 변경하지 않는다.
- 한 프레임에 서로 다른 독점 모듈이 Rigidbody 속도를 동시에 덮어쓰지 않는다.
- 기존 구현된 이동 동작은 모듈화 전과 동일하게 작동한다.
