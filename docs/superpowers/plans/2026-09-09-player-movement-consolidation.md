# Player Movement Consolidation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 동작마다 분리된 이동 MonoBehaviour와 `Priority` 중재를 제거하고 기존 플레이어 이동을 하나의 `PlayerMovementController`로 통합한다.

**Architecture:** `PlayerMovementController`가 일반 이동, 점프, 추가 중력, 등반, 벽점프, 벽 대시의 설정과 런타임 상태를 소유한다. `Player`는 Sprint/Crouch가 반영된 입력을 전달하고 실제 이동 여부만 조회하며, Rigidbody와 감지는 기존 `IMover`, `ICheckClimbWall`을 통해 처리한다.

**Tech Stack:** Unity 6, C#, MonoBehaviour, Rigidbody2D, Unity Input System

---

## Baseline

- 기준 씬: `Assets/01. Scenes/Test.unity`
- 구현 전 `dotnet build sf-2D.sln --no-restore`: 오류 0개, 기존 패키지 경고 9개
- 삭제할 모듈 GUID는 `Test.unity`에만 존재하며 다른 씬과 프리팹에는 없다.
- 현재 작업 트리의 다른 수정은 사용자 작업이므로 아래 명시된 파일 외에는 변경하지 않는다.

### Task 1: 이동 동작을 Controller 하나로 통합한다

**Files:**

- Modify: `Assets/02. Script/Player/Modules/PlayerMovementController.cs`

- [ ] **Step 1: 모듈 검색·정렬·순회 코드를 제거한다**

`PlayerMoveModule[]`, LINQ 정렬, `Priority`, `CanRun`, `BlocksAir`, `AfterApply`, 모듈별 `TryHandleJump` 호출을 모두 제거한다.

- [ ] **Step 2: Controller에 실제 이동 설정과 상태를 둔다**

다음 설정을 `[Header]`로 구분하여 직렬화한다.

```csharp
[Header("Move & Jump")]
[SerializeField] private float speed = 10f;
[SerializeField] private float jumpForce = 20f;

[Header("Air")]
[SerializeField] private float extraGravity = 15f;
[SerializeField] private float gravityDelay = 0.3f;

[Header("Climb")]
[SerializeField] private float climbUpSpeed = 10f;
[SerializeField] private float climbDownSpeed = 18f;
[SerializeField] private float climbStaminaCostPerSecond = 20f;
[SerializeField] private float reattachDelay = 2f;

[Header("Wall Jump")]
[SerializeField] private float wallJumpXForce = 8f;
[SerializeField] private float wallJumpYForce = 12f;
[SerializeField] private float wallJumpDuration = 0.2f;
[SerializeField] private float wallJumpStaminaCost = 20f;

[Header("Wall Dash")]
[SerializeField] private float wallDashSpeed;
[SerializeField] private float wallDashDuration = 0.2f;
[SerializeField] private float wallDashStaminaCost = 20f;
```

런타임 상태는 일반 점프 예약, 공중 시간, 재부착 시간, 벽점프/벽 대시 지속시간과 1회 충격 여부만 둔다.

- [ ] **Step 3: 생명주기와 입력 라우팅을 구현한다**

`Setup(Player)`에서 `IMover`, `IInputReader`, `IStats`, `ICheckClimbWall`, `IFacingController`를 한 번 조회하고 `OnJumpPressed`를 구독한다. `Release()`에서 같은 이벤트를 해제하고 기본 중력을 복원한다.

공개 API는 다음으로 제한한다.

```csharp
public bool IsClimbing { get; }
public bool Setup(Player owner);
public void Tick();
public void FixedTick(float moveInput, float climbInput);
public void Release();
```

점프 입력은 벽점프 → 벽 대시 → 벽타기 취소 → 일반 지상 점프 순으로 처리한다.

- [ ] **Step 4: 한 프레임의 Rigidbody 제어 순서를 명시한다**

`FixedTick`은 다음 조건문 하나로 제어권을 결정한다.

```csharp
using _02._Script.Player.Interface;
using _02._Script.Util;
using UnityEngine;

namespace _02._Script.Player.Modules
{
    public sealed class PlayerMovementController : MonoBehaviour
    {
        [Header("Move & Jump")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float jumpForce = 20f;

        [Header("Air")]
        [SerializeField] private float extraGravity = 15f;
        [SerializeField] private float gravityDelay = 0.3f;

        [Header("Climb")]
        [SerializeField] private float climbUpSpeed = 10f;
        [SerializeField] private float climbDownSpeed = 18f;
        [SerializeField] private float climbStaminaCostPerSecond = 20f;
        [SerializeField] private float reattachDelay = 2f;

        [Header("Wall Jump")]
        [SerializeField] private float wallJumpXForce = 8f;
        [SerializeField] private float wallJumpYForce = 12f;
        [SerializeField] private float wallJumpDuration = 0.2f;
        [SerializeField] private float wallJumpStaminaCost = 20f;

        [Header("Wall Dash")]
        [SerializeField] private float wallDashSpeed;
        [SerializeField] private float wallDashDuration = 0.2f;
        [SerializeField] private float wallDashStaminaCost = 20f;

        private IMover _mover;
        private IInputReader _inputReader;
        private IStats _stats;
        private ICheckClimbWall _climbWall;
        private IFacingController _facing;

        private bool _isSetup;
        private bool _jumpRequested;
        private bool _wallJumpImpulsePending;
        private bool _wallDashImpulsePending;
        private float _timeInAir;
        private float _reattachTimer;
        private float _wallJumpTimer;
        private float _wallDashTimer;
        private float _wallJumpDirection;

        private bool IsWallActionActive =>
            _wallJumpTimer > 0f ||
            _wallDashTimer > 0f;

        private bool CanClimb =>
            _reattachTimer <= 0f &&
            _climbWall != null &&
            _climbWall.IsClimbed;

        public bool IsClimbing =>
            _isSetup &&
            !IsWallActionActive &&
            CanClimb &&
            _stats.Stamina > 0f;

        public bool Setup(Player owner)
        {
            if (_isSetup)
                return true;

            _mover = Utils.Get<IMover>(owner);
            _inputReader = Utils.Get<IInputReader>(owner);
            _stats = Utils.Get<IStats>(owner);
            _climbWall = Utils.Get<ICheckClimbWall>(owner);
            _facing = Utils.Get<IFacingController>(owner);

            if (_mover == null ||
                _inputReader == null ||
                _stats == null ||
                _climbWall == null ||
                _facing == null)
            {
                Debug.LogError("플레이어 이동에 필요한 컴포넌트를 찾지 못했습니다.", owner);
                enabled = false;
                return false;
            }

            ResetRuntimeState();
            _inputReader.OnJumpPressed += HandleJumpPressed;
            _isSetup = true;
            enabled = true;
            return true;
        }

        public void Tick()
        {
            if (!_isSetup)
                return;

            _reattachTimer = ReduceTimer(_reattachTimer);
            _wallJumpTimer = ReduceTimer(_wallJumpTimer);
            _wallDashTimer = ReduceTimer(_wallDashTimer);
        }

        public void FixedTick(float moveInput, float climbInput)
        {
            if (!_isSetup)
                return;

            bool blocksAir = true;

            if (_wallJumpTimer > 0f)
                ApplyWallJump();
            else if (_wallDashTimer > 0f)
                ApplyWallDash();
            else if (CanClimb)
                ApplyClimb(climbInput);
            else
            {
                blocksAir = false;
                ApplyMove(moveInput);
            }

            ApplyAir(blocksAir);
        }

        public void Release()
        {
            if (!_isSetup)
                return;

            _inputReader.OnJumpPressed -= HandleJumpPressed;
            _mover.RbCompo.gravityScale = _mover.GravityScale;
            ResetRuntimeState();
            _isSetup = false;
        }

        private void ApplyMove(float moveInput)
        {
            Rigidbody2D body = _mover.RbCompo;
            body.gravityScale = _mover.GravityScale;
            body.linearVelocityX = moveInput * speed;

            if (!_jumpRequested)
                return;

            _jumpRequested = false;

            if (!_mover.IsGround)
                return;

            _mover.StopImmediately(false, true);
            _mover.PushForce(Vector2.up, jumpForce, ForceMode2D.Impulse);
        }

        private void ApplyAir(bool isBlocked)
        {
            if (_mover.IsGround || isBlocked)
            {
                _timeInAir = 0f;
                return;
            }

            _timeInAir += Time.fixedDeltaTime;

            if (_timeInAir > gravityDelay)
                _mover.RbCompo.AddForceY(-extraGravity);
        }

        private void ApplyClimb(float climbInput)
        {
            if (!Mathf.Approximately(climbInput, 0f))
                _stats.UseStamina(climbStaminaCostPerSecond, false);

            if (_stats.Stamina <= 0f)
            {
                CancelClimb();
                return;
            }

            float climbSpeed = climbInput > 0f
                ? climbUpSpeed
                : climbDownSpeed;

            Rigidbody2D body = _mover.RbCompo;
            body.gravityScale = 0f;
            body.linearVelocity = new Vector2(0f, climbInput * climbSpeed);
        }

        private void ApplyWallJump()
        {
            if (!_wallJumpImpulsePending)
                return;

            Rigidbody2D body = _mover.RbCompo;
            body.gravityScale = _mover.GravityScale;
            body.linearVelocity = new Vector2(
                _wallJumpDirection * wallJumpXForce,
                wallJumpYForce);

            _wallJumpImpulsePending = false;
        }

        private void ApplyWallDash()
        {
            Rigidbody2D body = _mover.RbCompo;
            body.gravityScale = 0f;
            body.linearVelocityX = 0f;

            if (!_wallDashImpulsePending)
                return;

            body.AddForceY(wallDashSpeed, ForceMode2D.Impulse);
            _wallDashImpulsePending = false;
        }

        private void HandleJumpPressed()
        {
            if (_climbWall.IsClimbed)
            {
                if (!Mathf.Approximately(_inputReader.MoveInput, 0f))
                    StartWallJump();
                else if (_inputReader.ClimbInput > 0f)
                    StartWallDash();
                else if (_inputReader.ClimbInput < 0f)
                    CancelClimb();

                return;
            }

            if (_mover.IsGround)
                _jumpRequested = true;
        }

        private void StartWallJump()
        {
            if (IsWallActionActive || _stats.Stamina < wallJumpStaminaCost)
                return;

            _wallJumpDirection = _facing.IsFacingLeft ? 1f : -1f;
            _wallJumpTimer = wallJumpDuration;
            _wallJumpImpulsePending = true;
            _stats.UseStamina(wallJumpStaminaCost, true);
        }

        private void StartWallDash()
        {
            if (IsWallActionActive || _stats.Stamina < wallDashStaminaCost)
                return;

            _wallDashTimer = wallDashDuration;
            _wallDashImpulsePending = true;
            _stats.UseStamina(wallDashStaminaCost, true);
        }

        private void CancelClimb()
        {
            _reattachTimer = reattachDelay;
            _mover.RbCompo.gravityScale = _mover.GravityScale;
        }

        private void ResetRuntimeState()
        {
            _jumpRequested = false;
            _wallJumpImpulsePending = false;
            _wallDashImpulsePending = false;
            _timeInAir = 0f;
            _reattachTimer = 0f;
            _wallJumpTimer = 0f;
            _wallDashTimer = 0f;
            _wallJumpDirection = 0f;
        }

        private static float ReduceTimer(float timer)
        {
            return timer <= 0f
                ? 0f
                : Mathf.Max(0f, timer - Time.deltaTime);
        }
    }
}
```

벽점프, 벽 대시, 등반이 제어 중일 때는 추가 중력을 막고, 그 외 공중 상태에서만 `ApplyAir()`를 실행한다.

- [ ] **Step 5: 통합 직후 C# 참조 검사를 한다**

Run:

```powershell
rg -n "PlayerMoveModule|Priority|CanRun|BlocksAir|AfterApply|TryHandleJump" "Assets/02. Script/Player/Modules/PlayerMovementController.cs"
```

Expected: 검색 결과 없음.

### Task 2: Player와 Test 씬을 새 Controller에 연결한다

**Files:**

- Modify: `Assets/02. Script/Player/Player.cs`
- Modify: `Assets/01. Scenes/Test.unity`

- [ ] **Step 1: Player가 입력 두 값만 전달하도록 단순화한다**

`PlayerMovementInput` 생성을 제거하고 다음처럼 호출한다.

```csharp
movementController.FixedTick(finalMoveInput, _inputReader.ClimbInput);
```

`Player`의 `_checkClimbWall` 필드와 조회를 제거하고 실제 등반 여부는 다음처럼 Controller에서 읽는다.

```csharp
bool isClimbing = movementController.IsClimbing;
```

- [ ] **Step 2: 씬의 Player가 Controller를 직접 참조하게 한다**

`Test.unity`의 Player 필드를 다음처럼 이관한다.

```yaml
movementController: {fileID: 1999535690}
```

- [ ] **Step 3: Player GameObject에서 삭제할 모듈 컴포넌트 여섯 개를 제거한다**

Player의 `m_Component`에는 `1999535690` Controller만 남기고 다음 항목을 제거한다.

```yaml
- component: {fileID: 1999535689}
- component: {fileID: 1999535688}
- component: {fileID: 1999535687}
- component: {fileID: 1999535686}
- component: {fileID: 1999535685}
- component: {fileID: 1999535684}
```

- [ ] **Step 4: 여섯 모듈 YAML 블록을 없애고 설정을 Controller로 이관한다**

Controller 블록은 다음 필드를 가진다.

```yaml
speed: 10
jumpForce: 20
extraGravity: 15
gravityDelay: 0.3
climbUpSpeed: 10
climbDownSpeed: 18
climbStaminaCostPerSecond: 20
reattachDelay: 2
wallJumpXForce: 8
wallJumpYForce: 12
wallJumpDuration: 0.2
wallJumpStaminaCost: 20
wallDashSpeed: 0
wallDashDuration: 0.2
wallDashStaminaCost: 20
```

- [ ] **Step 5: 씬 참조를 검사한다**

Run:

```powershell
rg -n "1999535684|1999535685|1999535686|1999535687|1999535688|1999535689|Modules.State" "Assets/01. Scenes/Test.unity"
```

Expected: 검색 결과 없음.

### Task 3: 불필요한 이동 스크립트를 삭제하고 검증한다

**Files:**

- Delete: `Assets/02. Script/Player/Modules/PlayerMoveModule.cs`
- Delete: `Assets/02. Script/Player/Modules/PlayerMoveModule.cs.meta`
- Delete: `Assets/02. Script/Player/Modules/State.meta`
- Delete: `Assets/02. Script/Player/Modules/State/*.cs`
- Delete: `Assets/02. Script/Player/Modules/State/*.cs.meta`
- Delete: `Assets/02. Script/Player/Modules/Struct.meta`
- Delete: `Assets/02. Script/Player/Modules/Struct/PlayerMovementInput.cs`
- Delete: `Assets/02. Script/Player/Modules/Struct/PlayerMovementInput.cs.meta`

- [ ] **Step 1: 삭제 전 GUID 사용처가 Test 씬뿐임을 다시 확인한다**

Run:

```powershell
rg -l --glob '*.unity' --glob '*.prefab' --glob '*.asset' "f3933b834aaa4ee088c9fdf387b8e7e9|f5fe572b8678457eaf0ae90ef01db474|cc441f5a42e3452f8283687dfd7a7575|0ed172495df24dfd886cffe502a16b47|4cbe28e182a64098b12d8d74e468fb79|0028960ad014430f965a1768b09592b6" Assets
```

Expected: Task 2가 끝난 뒤 검색 결과 없음.

- [ ] **Step 2: 동작별 모듈과 메타 파일을 삭제한다**

위 Files 목록의 파일만 삭제한다. `PlayerMovementController.cs`, 해당 `.meta`, `Modules.meta`는 유지한다.

- [ ] **Step 3: Unity가 아직 갱신하지 않은 로컬 csproj에서 삭제 항목을 제거한다**

`Assembly-CSharp.csproj`는 git에서 무시되는 Unity 생성 파일이다. 삭제된 여덟 C# 파일의 `<Compile Include="..." />` 항목만 제거하여 CLI 빌드를 가능하게 한다.

- [ ] **Step 4: 전체 빌드를 실행한다**

Run:

```powershell
dotnet build "sf-2D.sln" --no-restore
```

Expected: 오류 0개. 기존 Unity 패키지 의존성 경고는 허용한다.

- [ ] **Step 5: 삭제 타입과 Missing Script 후보를 정적으로 검사한다**

Run:

```powershell
rg -n "PlayerMoveModule|AirModule|ClimbModule|DashModule|MoveModule|WallDashModule|WallJumpModule|PlayerMovementInput|Priority|CanRun|BlocksAir|AfterApply|TryHandleJump" "Assets/02. Script/Player" "Assets/01. Scenes/Test.unity"
```

Expected: 검색 결과 없음.

- [ ] **Step 6: Unity Play Mode 체크리스트를 전달한다**

사용자가 `Test.unity`에서 좌우 이동, 점프, 공중 낙하, 벽타기, 벽점프, 벽 대시, Sprint/Crouch 배율을 확인한다. CLI 빌드는 Unity 런타임 물리 감지까지 증명하지 않으므로 Play Mode 검증을 완료했다고 주장하지 않는다.
