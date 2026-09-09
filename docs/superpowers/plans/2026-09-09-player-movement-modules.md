# Player Movement Modules Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `Player`와 거대한 `Mover`에 섞여 있는 이동 구현을, 각자 Inspector 설정과 물리 동작을 소유하는 MonoBehaviour 모듈로 분리한다.

**Architecture:** `Player`는 Sprint/Crouch/Facing/상호작용/HUD를 유지하고 `PlayerMovementController` 하나만 호출한다. Controller는 enum이나 상태 Dictionary 없이 `Move → Air → Climb → WallJump → WallDash` 모듈을 한 번 찾아 초기화하고, 고정 우선순위에 따라 Rigidbody 제어권을 한 모듈에만 준다. `Mover`에는 Rigidbody, 기본 중력, 접지 판정, 외부 힘 API만 남긴다.

**Tech Stack:** Unity 6.0, C#, MonoBehaviour, Rigidbody2D, Input System, 기존 Agent/IAgentModule/Stats 구조

---

## 현재 기준선

- 기준 씬: `Assets/01. Scenes/Test.unity`
- 현재 빌드: `dotnet build "sf-2D.sln" --no-restore` → 오류 0개, 기존 의존성 경고 9개
- Rigidbody2D 중력 배율: `3`
- Mover 설정: 이동 `10`, 점프 `20`, 추가 중력 `15`, 중력 지연 `0.3`, 등반 위 `10`, 아래 `18`, 벽대시 `0`, 벽점프 X `8`, Y `12`, 지속시간 `0.2`
- Player 스태미나 설정: 달리기 `20`, 등반 `20/초`, 벽대시 `20`, 벽점프 `20`
- 기존 `wallDashSpeed`가 `0`이므로 리팩터링 직후에도 벽대시가 눈에 띄게 상승하지 않는 것이 동작 보존 기준이다. 리팩터링 검증 후 별도 튜닝으로만 바꾼다.
- 현재 작업 트리에 다른 수정과 staged 파일이 있으므로 아래 커밋은 항상 `git commit --only ... -- <이번 파일>` 형식으로 실행한다.

## 최종 실행 규칙

```text
WallDash active → WallDash만 Rigidbody 제어
else WallJump active → WallJump만 Rigidbody 제어
else Dash active → Dash만 Rigidbody 제어
else Climb active → Climb만 Rigidbody 제어
else → Move가 Rigidbody 제어

Air는 위 독점 동작이 없을 때만 추가 중력을 적용
```

점프 입력은 다음 순서로 라우팅한다.

```text
벽 접촉 + 좌우 입력 → WallJump
벽 접촉 + 위 입력 → WallDash
벽 접촉 + 아래 입력 → Climb.Cancel
그 외 → Move.TryJump (내부에서 접지 확인)
```

## Task 1: 공통 모듈 기반을 확장한다

**Files:**

- Modify: `Assets/02. Script/Util/Utils.cs`
- Modify: `Assets/02. Script/Player/Interface/IMover.cs`
- Modify: `Assets/02. Script/Player/Components/Mover.cs`
- Modify: `Assets/02. Script/Player/Modules/PlayerMoveModule.cs`
- Create: `Assets/02. Script/Player/Modules/PlayerMovementInput.cs`
- Create: `Assets/02. Script/Player/Modules/PlayerMovementController.cs`

- [ ] **Step 1: `Utils.Get<T>()`가 누락과 중복을 구분하도록 바꾼다**

`Utils.cs`를 다음 내용으로 교체한다.

```csharp
using System.Linq;
using UnityEngine;

namespace _02._Script.Util
{
    public static class Utils
    {
        public static T Get<T>(Component owner) where T : class
        {
            var modules = FindAll<T>(owner);

            if (modules.Length == 1)
                return modules[0];

            if (modules.Length == 0)
                Debug.LogError($"{typeof(T).Name} 모듈을 찾지 못했습니다.", owner);
            else
                Debug.LogError($"{typeof(T).Name} 모듈이 {modules.Length}개 있습니다. 하나만 남겨주세요.", owner);

            return null;
        }

        public static T GetOptional<T>(Component owner) where T : class
        {
            var modules = FindAll<T>(owner);

            if (modules.Length <= 1)
                return modules.FirstOrDefault();

            Debug.LogError($"선택 모듈 {typeof(T).Name}이 {modules.Length}개 있습니다. 하나만 남겨주세요.", owner);
            return null;
        }

        private static T[] FindAll<T>(Component owner) where T : class
        {
            return owner
                .GetComponentsInChildren<MonoBehaviour>(true)
                .OfType<T>()
                .ToArray();
        }
    }
}
```

- [ ] **Step 2: Controller가 사용할 한 프레임 입력 묶음을 만든다**

`PlayerMovementInput.cs`를 만든다.

```csharp
namespace _02._Script.Player.Modules
{
    public readonly struct PlayerMovementInput
    {
        public PlayerMovementInput(float move, float climb)
        {
            Move = move;
            Climb = climb;
        }

        public float Move { get; }
        public float Climb { get; }
    }
}
```

- [ ] **Step 3: `IMover`에 저수준 물리 참조를 추가하되 기존 API는 아직 남긴다**

이 단계에서는 현재 Player가 계속 동작해야 하므로 `SetMoveInput` 이하 기존 메서드를 지우지 않는다.

```csharp
using UnityEngine;

namespace _02._Script.Player.Interface
{
    public interface IMover
    {
        bool IsGround { get; }
        Rigidbody2D Body { get; }
        float DefaultGravityScale { get; }

        void PushForce(Vector2 pushDir, float power, ForceMode2D forceMode);

        // 마이그레이션 완료 전까지만 유지한다.
        void SetMoveInput(float moveInput);
        void ClimbInput(float climbInput);
        void Climb(ICheckClimbWall check);
        void CalculateAirTime(ICheckClimbWall checkClimbWall);
        void Jump(float multiplier = 1);
        void WallJump(float dir);
        void WallDash();
        void CancelClimb();
    }
}
```

현재 `Mover.cs`에 아래 두 속성만 추가한다. 기존 `PushForce()`는 이미 구현돼 있다.

```csharp
public Rigidbody2D Body => rb;
public float DefaultGravityScale => _originGravityScale;
```

- [ ] **Step 4: 다음 모듈들이 매 단계 컴파일되도록 Controller의 최소 껍데기를 만든다**

Task 7에서 이 파일 전체를 실제 Controller 코드로 교체한다. 지금은 공통 타입과 호출 지점만 먼저 고정한다.

```csharp
using UnityEngine;

namespace _02._Script.Player.Modules
{
    public class PlayerMovementController : MonoBehaviour
    {
        public bool BlocksAir => false;
        public bool CanClimbControl => true;

        public void ResetAirTime() { }
    }
}
```

- [ ] **Step 5: `PlayerMoveModule`에 새 생명주기를 추가한다**

현재 Player가 호출하는 `Setup(IMover)`와 `Apply(float)`는 전환 전까지 호환용으로 유지한다.

```csharp
using _02._Script.Player.Interface;
using UnityEngine;

namespace _02._Script.Player.Modules
{
    public abstract class PlayerMoveModule : MonoBehaviour
    {
        protected Player Owner { get; private set; }
        protected PlayerMovementController Controller { get; private set; }
        protected IMover Mover { get; private set; }
        protected Rigidbody2D Body => Mover.Body;

        public virtual bool Setup(
            Player owner,
            PlayerMovementController controller,
            IMover mover)
        {
            Owner = owner;
            Controller = controller;
            Mover = mover;
            return owner != null && controller != null && mover != null;
        }

        public virtual void Tick() { }
        public virtual void FixedTick(PlayerMovementInput input) { }
        public virtual void Release() { }

        // 기존 Player 호환용. Task 10에서 제거한다.
        public virtual void Setup(IMover mover)
        {
            Mover = mover;
        }

        // 기존 MoveModule 호환용. Task 10에서 제거한다.
        public virtual void Apply(float moveInput) { }
    }
}
```

- [ ] **Step 6: 컴파일한다**

Run:

```powershell
dotnet build "sf-2D.sln" --no-restore
```

Expected: `오류 0개`. 현재 존재하는 의존성 경고 9개는 이번 범위가 아니다.

- [ ] **Step 7: 이 단계 파일만 커밋한다**

```powershell
git add -- "Assets/02. Script/Util/Utils.cs" "Assets/02. Script/Player/Interface/IMover.cs" "Assets/02. Script/Player/Components/Mover.cs" "Assets/02. Script/Player/Modules/PlayerMoveModule.cs" "Assets/02. Script/Player/Modules/PlayerMovementInput.cs" "Assets/02. Script/Player/Modules/PlayerMovementInput.cs.meta" "Assets/02. Script/Player/Modules/PlayerMovementController.cs" "Assets/02. Script/Player/Modules/PlayerMovementController.cs.meta"
git commit --only -m "refactor: add movement module lifecycle" -- "Assets/02. Script/Util/Utils.cs" "Assets/02. Script/Player/Interface/IMover.cs" "Assets/02. Script/Player/Components/Mover.cs" "Assets/02. Script/Player/Modules/PlayerMoveModule.cs" "Assets/02. Script/Player/Modules/PlayerMovementInput.cs" "Assets/02. Script/Player/Modules/PlayerMovementInput.cs.meta" "Assets/02. Script/Player/Modules/PlayerMovementController.cs" "Assets/02. Script/Player/Modules/PlayerMovementController.cs.meta"
```

## Task 2: 좌우 이동과 일반 점프를 `MoveModule`에 묶는다

**Files:**

- Modify: `Assets/02. Script/Player/Modules/MoveModule.cs`

- [ ] **Step 1: `MoveModule`이 속도와 점프 힘을 직접 소유하게 한다**

```csharp
using UnityEngine;

namespace _02._Script.Player.Modules
{
    public class MoveModule : PlayerMoveModule
    {
        [SerializeField] private float speed = 10f;
        [SerializeField] private float jumpForce = 20f;

        private bool _jumpRequested;

        public override void FixedTick(PlayerMovementInput input)
        {
            Body.gravityScale = Mover.DefaultGravityScale;
            Body.linearVelocityX = input.Move * speed;

            if (!_jumpRequested)
                return;

            Body.linearVelocityY = 0f;
            Body.AddForceY(jumpForce, ForceMode2D.Impulse);
            _jumpRequested = false;
        }

        public bool TryJump()
        {
            if (!Mover.IsGround)
                return false;

            _jumpRequested = true;
            Controller.ResetAirTime();
            return true;
        }

        // 기존 Player가 전환되기 전까지만 사용한다.
        public override void Apply(float moveInput)
        {
            Mover.SetMoveInput(moveInput);
        }
    }
}
```

`FixedTick()`은 아직 호출되지 않으므로 실제 이동은 기존 Mover가 계속 담당한다. `ResetAirTime()`은 Task 1의 최소 Controller에서는 아무 일도 하지 않고, Task 7에서 실제 Air 초기화로 연결된다.

- [ ] **Step 2: 컴파일하고 현재 좌우 이동 회귀를 확인한다**

Run: `dotnet build "sf-2D.sln" --no-restore`

Expected: 오류 0개.

Unity Play Mode:

1. 좌우 입력 시 기존 속도로 움직인다.
2. 입력을 놓으면 X 속도가 0이 된다.
3. Sprint와 Crouch 배율이 기존처럼 반영된다.

- [ ] **Step 3: 커밋한다**

```powershell
git add -- "Assets/02. Script/Player/Modules/MoveModule.cs"
git commit --only -m "refactor: prepare move and jump module" -- "Assets/02. Script/Player/Modules/MoveModule.cs"
```

## Task 3: 공중 동작을 `AirModule`로 만든다

**Files:**

- Create: `Assets/02. Script/Player/Modules/AirModule.cs`

- [ ] **Step 1: 공중 시간과 추가 중력을 소유하는 모듈을 만든다**

```csharp
using UnityEngine;

namespace _02._Script.Player.Modules
{
    public class AirModule : PlayerMoveModule
    {
        [SerializeField] private float extraGravity = 15f;
        [SerializeField] private float gravityDelay = 0.3f;

        private float _timeInAir;

        public override void Tick()
        {
            if (Mover.IsGround || Controller.BlocksAir)
            {
                ResetAirTime();
                return;
            }

            _timeInAir += Time.deltaTime;
        }

        public override void FixedTick(PlayerMovementInput input)
        {
            if (Mover.IsGround || Controller.BlocksAir)
                return;

            if (_timeInAir > gravityDelay)
                Body.AddForceY(-extraGravity);
        }

        public void ResetAirTime()
        {
            _timeInAir = 0f;
        }
    }
}
```

- [ ] **Step 2: 컴파일한다**

Run: `dotnet build "sf-2D.sln" --no-restore`

Expected: 오류 0개. 이 모듈은 아직 Controller가 실행하지 않으므로 플레이 동작은 그대로다.

- [ ] **Step 3: Unity가 만든 `.meta`와 함께 준비 커밋한다**

```powershell
git add -- "Assets/02. Script/Player/Modules/AirModule.cs" "Assets/02. Script/Player/Modules/AirModule.cs.meta"
git commit --only -m "refactor: add air movement module" -- "Assets/02. Script/Player/Modules/AirModule.cs" "Assets/02. Script/Player/Modules/AirModule.cs.meta"
```

## Task 4: 벽타기를 `ClimbModule`로 만든다

**Files:**

- Create: `Assets/02. Script/Player/Modules/ClimbModule.cs`

- [ ] **Step 1: 등반 속도, 지속 소모, 재부착 제한을 한 모듈에 둔다**

```csharp
using _02._Script.Player.Interface;
using _02._Script.Util;
using UnityEngine;

namespace _02._Script.Player.Modules
{
    public class ClimbModule : PlayerMoveModule
    {
        [SerializeField] private float climbUpSpeed = 10f;
        [SerializeField] private float climbDownSpeed = 18f;
        [SerializeField] private float staminaCostPerSecond = 20f;
        [SerializeField] private float reattachDelay = 2f;

        private ICheckClimbWall _checkClimbWall;
        private IInputReader _inputReader;
        private IStats _stats;
        private float _reattachTimer;

        public bool IsActive =>
            _reattachTimer <= 0f &&
            _checkClimbWall != null &&
            _checkClimbWall.IsClimbed;

        public override bool Setup(
            Player owner,
            PlayerMovementController controller,
            IMover mover)
        {
            if (!base.Setup(owner, controller, mover))
                return false;

            _checkClimbWall = Utils.Get<ICheckClimbWall>(owner);
            _inputReader = Utils.Get<IInputReader>(owner);
            _stats = Utils.Get<IStats>(owner);

            return _checkClimbWall != null && _inputReader != null && _stats != null;
        }

        public override void Tick()
        {
            if (_reattachTimer > 0f)
                _reattachTimer -= Time.deltaTime;

            if (!IsActive || !Controller.CanClimbControl)
                return;

            if (!Mathf.Approximately(_inputReader.ClimbInput, 0f))
                _stats.UseStamina(staminaCostPerSecond, false);

            if (_stats.Stamina <= 0f)
                Cancel();
        }

        public override void FixedTick(PlayerMovementInput input)
        {
            Body.gravityScale = 0f;

            float climbSpeed = 0f;
            if (!Mathf.Approximately(input.Climb, 0f))
                climbSpeed = input.Climb > 0f ? climbUpSpeed : climbDownSpeed;

            Body.linearVelocity = new Vector2(0f, input.Climb * climbSpeed);
        }

        public void Cancel()
        {
            _reattachTimer = reattachDelay;
            Body.gravityScale = Mover.DefaultGravityScale;
        }
    }
}
```

- [ ] **Step 2: 컴파일한다**

Run: `dotnet build "sf-2D.sln" --no-restore`

Expected: 오류 0개. 아직 Player 경로에는 연결되지 않는다.

- [ ] **Step 3: 준비 커밋한다**

```powershell
git add -- "Assets/02. Script/Player/Modules/ClimbModule.cs" "Assets/02. Script/Player/Modules/ClimbModule.cs.meta"
git commit --only -m "refactor: add climb movement module" -- "Assets/02. Script/Player/Modules/ClimbModule.cs" "Assets/02. Script/Player/Modules/ClimbModule.cs.meta"
```

## Task 5: 벽점프를 `WallJumpModule`로 만든다

**Files:**

- Create: `Assets/02. Script/Player/Modules/WallJumpModule.cs`

- [ ] **Step 1: 벽점프의 힘, 시간, 1회 비용을 모듈에 둔다**

```csharp
using _02._Script.Player.Interface;
using _02._Script.Util;
using UnityEngine;

namespace _02._Script.Player.Modules
{
    public class WallJumpModule : PlayerMoveModule
    {
        [SerializeField] private float wallJumpXForce = 8f;
        [SerializeField] private float wallJumpYForce = 12f;
        [SerializeField] private float wallJumpDuration = 0.2f;
        [SerializeField] private float staminaCost = 20f;

        private IStats _stats;
        private float _direction;
        private float _remainingTime;
        private bool _impulsePending;

        public bool IsActive => _impulsePending || _remainingTime > 0f;

        public override bool Setup(
            Player owner,
            PlayerMovementController controller,
            IMover mover)
        {
            if (!base.Setup(owner, controller, mover))
                return false;

            _stats = Utils.Get<IStats>(owner);
            return _stats != null;
        }

        public override void Tick()
        {
            if (_remainingTime > 0f)
                _remainingTime -= Time.deltaTime;
        }

        public override void FixedTick(PlayerMovementInput input)
        {
            if (!_impulsePending)
                return;

            Body.gravityScale = Mover.DefaultGravityScale;
            Body.linearVelocity = new Vector2(
                _direction * wallJumpXForce,
                wallJumpYForce);

            _impulsePending = false;
        }

        public bool TryStart(float direction)
        {
            if (IsActive || _stats.Stamina < staminaCost)
                return false;

            _direction = direction;
            _remainingTime = wallJumpDuration;
            _impulsePending = true;
            _stats.UseStamina(staminaCost, true);
            Controller.ResetAirTime();
            return true;
        }
    }
}
```

- [ ] **Step 2: 컴파일한다**

Run: `dotnet build "sf-2D.sln" --no-restore`

Expected: 오류 0개.

- [ ] **Step 3: 준비 커밋한다**

```powershell
git add -- "Assets/02. Script/Player/Modules/WallJumpModule.cs" "Assets/02. Script/Player/Modules/WallJumpModule.cs.meta"
git commit --only -m "refactor: add wall jump module" -- "Assets/02. Script/Player/Modules/WallJumpModule.cs" "Assets/02. Script/Player/Modules/WallJumpModule.cs.meta"
```

## Task 6: 벽대시와 비활성 Dash 경계를 만든다

**Files:**

- Create: `Assets/02. Script/Player/Modules/WallDashModule.cs`
- Create: `Assets/02. Script/Player/Modules/DashModule.cs`

- [ ] **Step 1: 벽대시가 FixedUpdate마다 비동기 작업을 만들지 않게 한다**

```csharp
using _02._Script.Player.Interface;
using _02._Script.Util;
using UnityEngine;

namespace _02._Script.Player.Modules
{
    public class WallDashModule : PlayerMoveModule
    {
        [SerializeField] private float wallDashSpeed;
        [SerializeField] private float wallDashDuration = 0.2f;
        [SerializeField] private float staminaCost = 20f;

        private IStats _stats;
        private float _remainingTime;
        private bool _impulsePending;

        public bool IsActive => _impulsePending || _remainingTime > 0f;

        public override bool Setup(
            Player owner,
            PlayerMovementController controller,
            IMover mover)
        {
            if (!base.Setup(owner, controller, mover))
                return false;

            _stats = Utils.Get<IStats>(owner);
            return _stats != null;
        }

        public override void Tick()
        {
            if (_remainingTime > 0f)
                _remainingTime -= Time.deltaTime;
        }

        public override void FixedTick(PlayerMovementInput input)
        {
            Body.gravityScale = 0f;
            Body.linearVelocityX = 0f;

            if (!_impulsePending)
                return;

            Body.AddForceY(wallDashSpeed, ForceMode2D.Impulse);
            _impulsePending = false;
        }

        public bool TryStart()
        {
            if (IsActive || _stats.Stamina < staminaCost)
                return false;

            _remainingTime = wallDashDuration;
            _impulsePending = true;
            _stats.UseStamina(staminaCost, true);
            Controller.ResetAirTime();
            return true;
        }
    }
}
```

- [ ] **Step 2: 기존 `MotionType.Dash`에는 동작이 없으므로 빈 확장 경계만 만든다**

```csharp
namespace _02._Script.Player.Modules
{
    public class DashModule : PlayerMoveModule
    {
        public bool IsActive => false;

        public bool TryStart()
        {
            return false;
        }
    }
}
```

Dash 입력, 속도, 쿨타임은 요구사항이 정해지지 않았으므로 추가하지 않는다. 이 컴포넌트는 씬에 붙이지 않아도 된다.

- [ ] **Step 3: 컴파일한다**

Run: `dotnet build "sf-2D.sln" --no-restore`

Expected: 오류 0개.

- [ ] **Step 4: 준비 커밋한다**

```powershell
git add -- "Assets/02. Script/Player/Modules/WallDashModule.cs" "Assets/02. Script/Player/Modules/WallDashModule.cs.meta" "Assets/02. Script/Player/Modules/DashModule.cs" "Assets/02. Script/Player/Modules/DashModule.cs.meta"
git commit --only -m "refactor: add wall dash and dash module boundaries" -- "Assets/02. Script/Player/Modules/WallDashModule.cs" "Assets/02. Script/Player/Modules/WallDashModule.cs.meta" "Assets/02. Script/Player/Modules/DashModule.cs" "Assets/02. Script/Player/Modules/DashModule.cs.meta"
```

## Task 7: 모든 이동 모듈을 관리하는 Controller를 만든다

**Files:**

- Modify: `Assets/02. Script/Player/Modules/PlayerMovementController.cs`
- Modify: `Assets/02. Script/Player/Modules/MoveModule.cs`

- [ ] **Step 1: enum 없이 타입별 모듈을 한 번 찾아 캐시하는 Controller를 만든다**

```csharp
using _02._Script.Player.Interface;
using _02._Script.Util;
using UnityEngine;

namespace _02._Script.Player.Modules
{
    public class PlayerMovementController : MonoBehaviour
    {
        private ICheckClimbWall _checkClimbWall;
        private IFacingController _facingController;
        private IInputReader _inputReader;
        private IMover _mover;

        private MoveModule _move;
        private AirModule _air;
        private ClimbModule _climb;
        private WallJumpModule _wallJump;
        private WallDashModule _wallDash;
        private DashModule _dash;

        private bool _isSetup;

        public bool IsGrounded => _mover != null && _mover.IsGround;

        public bool CanClimbControl =>
            !_wallDash.IsActive &&
            !_wallJump.IsActive &&
            !IsDashActive;

        public bool BlocksAir =>
            _wallDash.IsActive ||
            _wallJump.IsActive ||
            IsDashActive ||
            (CanClimbControl && _climb.IsActive);

        private bool IsDashActive => _dash != null && _dash.IsActive;

        public bool Setup(Player owner)
        {
            if (_isSetup)
                return true;

            _mover = Utils.Get<IMover>(owner);
            _inputReader = Utils.Get<IInputReader>(owner);
            _checkClimbWall = Utils.Get<ICheckClimbWall>(owner);
            _facingController = Utils.Get<IFacingController>(owner);

            _move = Utils.Get<MoveModule>(owner);
            _air = Utils.Get<AirModule>(owner);
            _climb = Utils.Get<ClimbModule>(owner);
            _wallJump = Utils.Get<WallJumpModule>(owner);
            _wallDash = Utils.Get<WallDashModule>(owner);
            _dash = Utils.GetOptional<DashModule>(owner);

            if (_mover == null ||
                _inputReader == null ||
                _checkClimbWall == null ||
                _facingController == null ||
                _move == null ||
                _air == null ||
                _climb == null ||
                _wallJump == null ||
                _wallDash == null)
            {
                enabled = false;
                return false;
            }

            bool modulesReady =
                _move.Setup(owner, this, _mover) &
                _air.Setup(owner, this, _mover) &
                _climb.Setup(owner, this, _mover) &
                _wallJump.Setup(owner, this, _mover) &
                _wallDash.Setup(owner, this, _mover);

            if (_dash != null)
                modulesReady &= _dash.Setup(owner, this, _mover);

            if (!modulesReady)
            {
                enabled = false;
                return false;
            }

            _inputReader.OnJumpPressed += HandleJumpPressed;
            _isSetup = true;
            return true;
        }

        public void Tick()
        {
            if (!_isSetup)
                return;

            _wallDash.Tick();
            _wallJump.Tick();
            _dash?.Tick();
            _climb.Tick();
            _air.Tick();
        }

        public void FixedTick(PlayerMovementInput input)
        {
            if (!_isSetup)
                return;

            if (_wallDash.IsActive)
                _wallDash.FixedTick(input);
            else if (_wallJump.IsActive)
                _wallJump.FixedTick(input);
            else if (IsDashActive)
                _dash.FixedTick(input);
            else if (_climb.IsActive)
                _climb.FixedTick(input);
            else
                _move.FixedTick(input);

            if (!BlocksAir)
                _air.FixedTick(input);
        }

        public void ResetAirTime()
        {
            _air.ResetAirTime();
        }

        public void Release()
        {
            if (!_isSetup)
                return;

            _inputReader.OnJumpPressed -= HandleJumpPressed;

            _move.Release();
            _air.Release();
            _climb.Release();
            _wallJump.Release();
            _wallDash.Release();
            _dash?.Release();

            _isSetup = false;
        }

        private void HandleJumpPressed()
        {
            if (_checkClimbWall.IsClimbed)
            {
                if (!Mathf.Approximately(_inputReader.MoveInput, 0f))
                {
                    float direction = _facingController.IsFacingLeft ? 1f : -1f;
                    _wallJump.TryStart(direction);
                    return;
                }

                if (_inputReader.ClimbInput > 0f)
                {
                    _wallDash.TryStart();
                    return;
                }

                if (_inputReader.ClimbInput < 0f)
                {
                    _climb.Cancel();
                    return;
                }
            }

            _move.TryJump();
        }
    }
}
```

`&`는 오타가 아니다. 모든 필수 모듈의 `Setup()`을 호출해서 누락 의존성을 한 번에 로그로 확인하려는 의도다.

- [ ] **Step 2: MoveModule의 공중 시간 초기화 연결을 확인한다**

`MoveModule.TryJump()`에 이미 있는 다음 줄이 그대로 유지됐는지 확인한다.

```csharp
Controller.ResetAirTime();
```

- [ ] **Step 3: 완성된 Controller와 전체 모듈을 함께 컴파일한다**

Run: `dotnet build "sf-2D.sln" --no-restore`

Expected: 오류 0개, 기존 경고만 존재.

- [ ] **Step 4: Controller를 커밋한다**

```powershell
git add -- "Assets/02. Script/Player/Modules/PlayerMovementController.cs" "Assets/02. Script/Player/Modules/PlayerMovementController.cs.meta" "Assets/02. Script/Player/Modules/MoveModule.cs"
git commit --only -m "refactor: coordinate player movement modules" -- "Assets/02. Script/Player/Modules/PlayerMovementController.cs" "Assets/02. Script/Player/Modules/PlayerMovementController.cs.meta" "Assets/02. Script/Player/Modules/MoveModule.cs"
```

## Task 8: 새 모듈을 Test 씬에 배치하고 기존 값을 복사한다

**Files:**

- Modify through Unity Inspector: `Assets/01. Scenes/Test.unity`

- [ ] **Step 1: `Player` GameObject에 필수 컴포넌트를 하나씩 추가한다**

이미 붙어 있는 `MoveModule`은 다시 추가하지 않는다.

1. `PlayerMovementController`
2. `AirModule`
3. `ClimbModule`
4. `WallJumpModule`
5. `WallDashModule`

`DashModule`은 선택 사항이므로 추가하지 않는다. 각 필수 타입은 정확히 하나여야 한다.

- [ ] **Step 2: 기존 Inspector 값을 새 소유자에게 복사한다**

| 새 컴포넌트 | 필드 | 값 |
| --- | --- | ---: |
| MoveModule | Speed | 10 |
| MoveModule | Jump Force | 20 |
| AirModule | Extra Gravity | 15 |
| AirModule | Gravity Delay | 0.3 |
| ClimbModule | Climb Up Speed | 10 |
| ClimbModule | Climb Down Speed | 18 |
| ClimbModule | Stamina Cost Per Second | 20 |
| ClimbModule | Reattach Delay | 2 |
| WallJumpModule | Wall Jump X Force | 8 |
| WallJumpModule | Wall Jump Y Force | 12 |
| WallJumpModule | Wall Jump Duration | 0.2 |
| WallJumpModule | Stamina Cost | 20 |
| WallDashModule | Wall Dash Speed | 0 |
| WallDashModule | Wall Dash Duration | 0.2 |
| WallDashModule | Stamina Cost | 20 |

- [ ] **Step 3: 씬을 저장하고 Console을 확인한다**

Expected:

- Missing Script 없음
- 같은 이동 모듈 중복 없음
- 아직 Player가 기존 경로를 쓰므로 Play Mode 동작 변화 없음

- [ ] **Step 4: 씬만 커밋한다**

```powershell
git add -- "Assets/01. Scenes/Test.unity"
git commit --only -m "scene: configure player movement modules" -- "Assets/01. Scenes/Test.unity"
```

## Task 9: Player의 이동 진입점을 Controller 하나로 전환한다

**Files:**

- Modify: `Assets/02. Script/Player/Player.cs`
- Modify through Unity Inspector: `Assets/01. Scenes/Test.unity`

- [ ] **Step 1: `Player.cs`를 최종 구조로 바꾼다**

```csharp
using _02._Script.Player.Interface;
using _02._Script.Player.Modules;
using _02._Script.Player.Sprint;
using _02._Script.UI;
using UnityEngine;

namespace _02._Script.Player
{
    public class Player : Agent
    {
        [SerializeField] private float useStaminaInRun;
        [SerializeField] private StaminaHUD staminaHUD;
        [SerializeField] private PlayerMovementController movementController;

        private ICheckClimbWall _checkClimbWall;
        private IInputReader _inputReader;
        private IInteractor _interactor;
        private ICrouchController _crouchController;
        private IFacingController _facingController;
        private IStats _stats;
        private SprintController _sprintController;

        private void Update()
        {
            bool isClimbing = _checkClimbWall.IsClimbed;
            bool isMoving = !Mathf.Approximately(_inputReader.MoveInput, 0f);

            if (!isClimbing)
                _facingController.UpdateFacing(_inputReader.MoveInput);

            _stats.StaminaUpdate(
                movementController.IsGrounded,
                isMoving,
                isClimbing);

            _sprintController.Tick(isMoving);
            movementController.Tick();
        }

        private void FixedUpdate()
        {
            float finalMoveInput =
                _inputReader.MoveInput *
                _sprintController.MoveSpeedMultiplier *
                _crouchController.MoveSpeedMultiplier;

            var movementInput = new PlayerMovementInput(
                finalMoveInput,
                _inputReader.ClimbInput);

            movementController.FixedTick(movementInput);
        }

        private void LateUpdate()
        {
            staminaHUD?.UpdateStamina(_stats.Stamina);
        }

        protected override void AfterInitialize()
        {
            base.AfterInitialize();

            GetModules();
            _sprintController = new SprintController(_stats, useStaminaInRun);
            SubscribeInputEvents();

            if (movementController == null || !movementController.Setup(this))
            {
                Debug.LogError("PlayerMovementController 초기화에 실패했습니다.", this);
                enabled = false;
            }
        }

        protected override void OnDispose()
        {
            movementController?.Release();
            UnsubscribeInputEvents();
            base.OnDispose();
        }

        private void GetModules()
        {
            _inputReader = GetModule<IInputReader>();
            _interactor = GetModule<IInteractor>();
            _stats = GetModule<IStats>();
            _checkClimbWall = GetModule<ICheckClimbWall>();
            _crouchController = GetModule<ICrouchController>();
            _facingController = GetModule<IFacingController>();
        }

        private void SubscribeInputEvents()
        {
            _inputReader.OnInteractPressed += HandleInteractInput;
            _inputReader.OnSprintPressed += HandleSprintInput;
            _inputReader.OnSprintReleased += HandleSprintRelease;
            _inputReader.OnCrouchPressed += HandleCrouchPressed;
            _inputReader.OnCrouchReleased += HandleCrouchRelease;
        }

        private void UnsubscribeInputEvents()
        {
            _inputReader.OnInteractPressed -= HandleInteractInput;
            _inputReader.OnSprintPressed -= HandleSprintInput;
            _inputReader.OnSprintReleased -= HandleSprintRelease;
            _inputReader.OnCrouchPressed -= HandleCrouchPressed;
            _inputReader.OnCrouchReleased -= HandleCrouchRelease;
        }

        private void HandleInteractInput()
        {
            _interactor.Interact(this);
        }

        private void HandleSprintInput()
        {
            _sprintController.StartSprint();
        }

        private void HandleSprintRelease()
        {
            _sprintController.StopSprint();
        }

        private void HandleCrouchPressed()
        {
            _crouchController.Crouch();
        }

        private void HandleCrouchRelease()
        {
            _crouchController.Stand();
        }
    }
}
```

확인할 제거 항목:

- `useStaminaInWall`, `useStaminaInWallDash`, `useStaminaInWallJump`
- `_mover`, `moveModule`
- `HandleJumpInput()`, `HandleClimbingJumpInput()`
- Player의 `OnJumpPressed` 구독/해제

- [ ] **Step 2: 컴파일한다**

Run: `dotnet build "sf-2D.sln" --no-restore`

Expected: 오류 0개.

- [ ] **Step 3: Unity Inspector에서 새 참조를 연결한다**

1. Test 씬의 Player를 선택한다.
2. Player 컴포넌트의 `Movement Controller` 칸에 같은 GameObject의 `PlayerMovementController`를 드래그한다.
3. Play Mode에 들어간다.

Expected: `PlayerMovementController 초기화에 실패했습니다` 및 모듈 누락/중복 오류가 없어야 한다.

- [ ] **Step 4: 핵심 전환 검증을 한다**

1. 좌우 이동과 정지
2. 지상 점프 및 공중 연속 점프 방지
3. 벽 접촉 시 중력 0과 위/아래 이동
4. 벽에서 좌우+점프로 벽점프
5. 벽에서 위+점프로 벽대시 비용 1회

- [ ] **Step 5: Player와 씬만 커밋한다**

```powershell
git add -- "Assets/02. Script/Player/Player.cs" "Assets/01. Scenes/Test.unity"
git commit --only -m "refactor: route player movement through controller" -- "Assets/02. Script/Player/Player.cs" "Assets/01. Scenes/Test.unity"
```

## Task 10: Mover를 저수준 물리 컴포넌트로 축소한다

**Files:**

- Modify: `Assets/02. Script/Player/Components/Mover.cs`
- Modify: `Assets/02. Script/Player/Interface/IMover.cs`
- Modify: `Assets/02. Script/Player/Modules/PlayerMoveModule.cs`
- Modify: `Assets/02. Script/Player/Modules/MoveModule.cs`

- [ ] **Step 1: `IMover`에서 이전 고수준 이동 API를 제거한다**

```csharp
using UnityEngine;

namespace _02._Script.Player.Interface
{
    public interface IMover
    {
        bool IsGround { get; }
        Rigidbody2D Body { get; }
        float DefaultGravityScale { get; }
        void PushForce(Vector2 pushDir, float power, ForceMode2D forceMode);
    }
}
```

- [ ] **Step 2: `Mover.cs`에서 enum, switch, 이동 튜닝값, UniTask를 모두 제거한다**

```csharp
using System;
using _02._Script.Player.Interface;
using UnityEngine;

namespace _02._Script.Player.Components
{
    public class Mover : MonoBehaviour, IAgentModule, IMover
    {
        [SerializeField] private Rigidbody2D rb;

        [Header("CheckGround")]
        [SerializeField] private Vector3 checker;
        [SerializeField] private Vector2 checkerSize;
        [SerializeField] private LayerMask whatIsGround;

        private float _defaultGravityScale;

        [field: SerializeField]
        public bool IsGround { get; private set; }

        public Rigidbody2D Body => rb;
        public float DefaultGravityScale => _defaultGravityScale;
        public Type Type => typeof(IMover);

        private void Awake()
        {
            if (rb == null)
                rb = transform.root.GetComponent<Rigidbody2D>();

            _defaultGravityScale = rb.gravityScale;
        }

        private void Reset()
        {
            rb = transform.root.GetComponent<Rigidbody2D>();

            if (rb != null)
                _defaultGravityScale = rb.gravityScale;
        }

        private void FixedUpdate()
        {
            IsGround = CheckGround();
        }

        public void Initialize(Agent owner) { }

        public void PushForce(
            Vector2 pushDir,
            float power,
            ForceMode2D forceMode)
        {
            rb.AddForce(pushDir * power, forceMode);
        }

        private bool CheckGround()
        {
            var col = Physics2D.OverlapBox(
                transform.position + checker,
                checkerSize,
                0f,
                whatIsGround);

            return col != null;
        }
    }
}
```

삭제되는 것:

- `MotionType` enum 전체
- `speed`, `jumpForce`, `extraGravity`, `gravityDelay`
- 등반/벽점프/벽대시 설정과 런타임 필드
- `FixedUpdate()`의 switch
- `SetMoveInput`, `Jump`, `CalculateAirTime`, `Climb`, `ClimbInput`, `CancelClimb`, `WallJump`, `WallDash`
- 모든 이동 관련 UniTask

- [ ] **Step 3: 공통 기반에서 호환 API를 제거한다**

`PlayerMoveModule.cs`의 아래 메서드를 삭제한다.

```csharp
public virtual void Setup(IMover mover)
public virtual void Apply(float moveInput)
```

`MoveModule.cs`의 아래 호환 override도 삭제한다.

```csharp
public override void Apply(float moveInput)
```

- [ ] **Step 4: 컴파일하고 남은 이전 API 참조를 검색한다**

Run:

```powershell
dotnet build "sf-2D.sln" --no-restore
rg -n "MotionType|SetMoveInput|CalculateAirTime|ClimbInput|HandleClimbingJumpInput|moveModule" "Assets/02. Script/Player" --glob "*.cs"
```

Expected:

- 빌드 오류 0개
- `rg` 결과 0건

- [ ] **Step 5: 축소 작업을 커밋한다**

```powershell
git add -- "Assets/02. Script/Player/Components/Mover.cs" "Assets/02. Script/Player/Interface/IMover.cs" "Assets/02. Script/Player/Modules/PlayerMoveModule.cs" "Assets/02. Script/Player/Modules/MoveModule.cs"
git commit --only -m "refactor: reduce mover to shared physics" -- "Assets/02. Script/Player/Components/Mover.cs" "Assets/02. Script/Player/Interface/IMover.cs" "Assets/02. Script/Player/Modules/PlayerMoveModule.cs" "Assets/02. Script/Player/Modules/MoveModule.cs"
```

## Task 11: 전체 회귀 검증과 최종 정리를 한다

**Files:**

- Verify: `Assets/01. Scenes/Test.unity`
- Verify: `Assets/02. Script/Player/**/*.cs`

- [ ] **Step 1: 최종 빌드를 실행한다**

```powershell
dotnet build "sf-2D.sln" --no-restore
```

Expected: 오류 0개, 기준선과 같은 기존 의존성 경고만 존재.

- [ ] **Step 2: Unity Console을 비우고 Test 씬을 Play한다**

Expected:

- Missing Script 없음
- NullReferenceException 없음
- 모듈 누락/중복 오류 없음
- 입력 이벤트 중복 실행 없음

- [ ] **Step 3: 일반 이동을 검증한다**

1. 좌/우 속도와 정지가 기존과 동일하다.
2. Sprint 속도 배율과 스태미나 소모가 유지된다.
3. Crouch 속도 배율과 Stand가 유지된다.
4. 진행 방향에 따라 Facing이 유지된다.

- [ ] **Step 4: 점프와 공중 동작을 검증한다**

1. 지상에서만 일반 점프가 시작된다.
2. 점프 시작 시 Y 속도가 정리되고 힘 `20`이 한 번 적용된다.
3. 공중 `0.3초` 이후 추가 하강력 `15`가 적용된다.
4. 착지하면 공중 시간이 초기화된다.

- [ ] **Step 5: 벽 동작을 검증한다**

1. 벽 접촉 시 중력이 0이고 위 속도 `10`, 아래 속도 `18`이 적용된다.
2. 등반 입력 중 스태미나는 초당 `20` 소모된다.
3. 스태미나 0 또는 아래+점프 취소 후 2초간 재부착하지 않는다.
4. 좌우+점프 벽점프는 X `8`, Y `12`, 비용 `20`을 한 번 적용한다.
5. 벽점프 0.2초 동안 Move/Climb이 속도를 덮어쓰지 않는다.
6. 위+점프 벽대시는 비용 `20`을 한 번만 적용한다.
7. 벽대시는 현재 속도 `0`이므로 상승하지 않아도 정상이며, 0.2초 뒤 Climb 제어로 돌아간다.

- [ ] **Step 6: 비이동 회귀를 검증한다**

1. 상호작용 입력이 동작한다.
2. StaminaHUD가 갱신된다.
3. 씬 재시작 후 입력 이벤트가 중복되지 않는다.

- [ ] **Step 7: 변경 범위를 확인한다**

```powershell
git status --short
git diff --check
```

Expected:

- `git diff --check` 출력 없음
- 이번 작업과 무관한 기존 사용자 변경은 그대로 보존됨

## 완료 조건

- `MoveModule`이 좌우 이동과 일반 점프의 설정/물리를 직접 가진다.
- `AirModule`, `ClimbModule`, `WallJumpModule`, `WallDashModule`이 자기 설정과 동작을 직접 가진다.
- 새 게임플레이가 없는 `DashModule`은 선택적 비활성 경계다.
- `PlayerMovementController`만 점프 라우팅과 독점 우선순위를 관리한다.
- `Player`에는 벽 이동 비용/벽점프/벽대시 구현이 없다.
- `Mover`에는 Rigidbody, 기본 중력, 접지 판정, `PushForce()`만 남는다.
- Checker, Sprint, Crouch, Facing은 기존 구조를 유지한다.
- `MotionType` enum과 이동 switch가 제거된다.
- 빌드 오류 0개이며 Test 씬의 기존 이동과 비이동 기능이 회귀하지 않는다.
