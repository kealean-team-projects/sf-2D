# Player Movement Consolidation Design

## Goal

현재 `Priority` 기반 이동 모듈 계층을 제거하고, 플레이어 이동 전체를 하나의 `PlayerMovementController`로 응집한다. `Player`는 이동 구현을 알지 않고 Controller만 호출하며, 기존 `Mover`, `CheckClimbWall`, 접지 감지는 그대로 재사용한다.

## Why the Current Structure Is Removed

현재 구조는 이동 동작마다 MonoBehaviour와 스크립트를 하나씩 만들고, Controller가 `Priority`, `CanRun`, `BlocksAir`, `AfterApply`, `TryHandleJump`로 충돌을 중재한다. 새 동작 하나가 모든 중재 규칙을 알아야 하므로 기능 추가가 쉬운 모듈 구조가 아니라 클래스 기반 FSM에 가깝다.

`Move`, `Jump`, `Air`, `Ground`는 하나의 기본 이동 알고리즘이며 독립 배포되는 기능이 아니다. `Climb`, `WallJump`, `WallDash`도 같은 벽 감지, 입력, 스태미나, 타이머를 공유하므로 한 컴포넌트 안에서 명시적인 순서로 처리하는 편이 더 응집도가 높다.

## Final Structure

```text
Player.cs
└─ PlayerMovementController.cs
   ├─ 일반 좌우 이동
   ├─ 지상 점프
   ├─ 공중 추가 중력
   ├─ 벽타기
   ├─ 벽점프
   └─ 벽 대시

Shared components
├─ Mover.cs             Rigidbody2D 조작, 기본 중력, 접지 감지
└─ CheckClimbWall.cs    벽 감지
```

Sprint, Crouch, Facing, 상호작용, Stats, HUD 구조는 이번 변경에서 유지한다.

## PlayerMovementController Responsibilities

Controller는 다음 설정을 직접 소유한다.

- 이동/점프: `speed`, `jumpForce`
- 공중: `extraGravity`, `gravityDelay`
- 벽타기: `climbUpSpeed`, `climbDownSpeed`, `staminaCostPerSecond`, `reattachDelay`
- 벽점프: `wallJumpXForce`, `wallJumpYForce`, `wallJumpDuration`, `wallJumpStaminaCost`
- 벽 대시: `wallDashSpeed`, `wallDashDuration`, `wallDashStaminaCost`

Inspector에서는 `[Header]`로 설정을 묶되 별도 설정 클래스나 ScriptableObject는 만들지 않는다.

Controller가 제공하는 공개 생명주기는 기존과 동일하게 `Setup(Player)`, `Tick()`, `FixedTick(float moveInput, float climbInput)`, `Release()` 네 개뿐이다. 세부 동작은 `HandleMove`, `HandleJump`, `ApplyAir`, `ApplyClimb`, `StartWallJump`, `StartWallDash` 같은 private 메서드로 나눈다.

## Execution Rules

한 FixedUpdate에서 물리 제어 순서는 다음처럼 코드에 직접 드러낸다.

```text
벽점프 진행 중 → 기본 이동과 추가 중력을 잠시 막음
벽 대시 진행 중 → 기본 이동과 추가 중력을 잠시 막음
등반 가능       → 벽타기 적용
그 외           → 일반 이동/대기 중인 지상 점프 적용

독점 벽 동작이 아닐 때만 공중 추가 중력 적용
```

숫자 `Priority`, 범용 `CanRun`, 범용 상태 Dictionary는 사용하지 않는다. 여러 기능이 같은 Rigidbody를 제어할 때 필요한 순서는 Controller의 조건문으로 명시한다.

점프 입력은 Controller 한 곳에서 다음 순서로 처리한다.

1. 벽 접촉 중 좌우 입력이 있으면 벽점프
2. 벽 접촉 중 위 입력이 있으면 벽 대시
3. 벽 접촉 중 아래 입력이 있으면 벽타기 취소
4. 그 외 접지 상태면 일반 점프 예약

## Preserved Behavior

- Sprint와 Crouch 배율이 반영된 최종 X 입력을 사용한다.
- 일반 점프는 지상에서만 한 번 실행한다.
- `gravityDelay` 이후 공중 추가 중력을 적용한다.
- 등반 중 중력을 0으로 만들고 입력 방향별 속도를 사용한다.
- 등반 이동 중 기존 방식대로 스태미나를 소모한다.
- 스태미나가 없거나 등반을 취소하면 일정 시간 재부착을 막는다.
- 벽점프와 벽 대시는 시작 시 비용과 힘을 한 번만 적용한다.
- 벽 동작 지속시간에는 일반 이동이 속도를 덮어쓰지 않는다.

기존 씬에서 `wallDashSpeed`가 0인 값도 동작 보존을 위해 그대로 이관한다.

## Files

수정:

- `Assets/02. Script/Player/Modules/PlayerMovementController.cs`
- `Assets/02. Script/Player/Player.cs`
- `Assets/01. Scenes/Test.unity`

삭제:

- `Assets/02. Script/Player/Modules/PlayerMoveModule.cs`
- `Assets/02. Script/Player/Modules/State/`
- `Assets/02. Script/Player/Modules/Struct/`

기존 `Mover`, `IMover`, Checker 파일은 현재 저수준 API를 보존하며 이번 통합 때문에 다시 확장하지 않는다.

## Scene Migration

`PlayerMovementController.cs.meta`는 유지하여 Controller 컴포넌트 참조를 보존한다. `Test.unity`에서 삭제되는 여섯 모듈 컴포넌트와 GameObject component 항목을 제거하고, 각 모듈의 직렬화된 튜닝값을 Controller 컴포넌트로 옮긴다. 이를 통해 Missing Script를 남기지 않는다.

## Verification

자동 확인:

- `dotnet build sf-2D.sln --no-restore` 오류 0개
- 삭제한 모듈 타입과 GUID가 C# 및 씬에 남지 않음
- `PlayerMovementController` 컴포넌트가 `Test.unity`에 한 개 존재

Unity Play Mode 수동 확인:

- 좌우 이동, 정지, Sprint/Crouch 배율
- 지상 점프와 공중 연속 점프 방지
- 공중 추가 중력
- 벽타기 위/아래 이동과 스태미나
- 벽점프 방향과 지속시간
- 벽 대시
- 벽 이탈 후 재부착 지연

## Acceptance Criteria

- 이동 계층에 동작별 MonoBehaviour 스크립트가 남지 않는다.
- `Priority`, `CanRun`, `BlocksAir`, `AfterApply`, `TryHandleJump`가 제거된다.
- `Player`는 Controller 생명주기와 입력 전달만 담당한다.
- 감지기와 `Mover`는 유지된다.
- 씬에 삭제된 모듈의 Missing Script가 남지 않는다.
- 기존 이동 수치와 입력 규칙이 보존된다.
