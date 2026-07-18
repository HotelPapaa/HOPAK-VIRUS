# HOPAK VIRUS Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Unity 기본 프리미티브와 Legacy Input만으로, A/D 호팍 이동과 짧은 사거리 감염 전투를 중심으로 한 3~4분짜리 완결된 3인칭 3D 액션 게임을 만든다.

**Architecture:** 순수 C# 리듬 상태 객체가 입력 타이밍과 속도 증가를 계산하고 MonoBehaviour들이 이동, 전투, 적, 방 진행, UI를 각각 담당한다. 세 개의 연결된 방은 Editor 빌더가 기본 프리미티브로 한 씬에 생성하며, EditMode 단위 테스트와 PlayMode 연동 테스트를 거친다.

**Tech Stack:** Unity `6000.3.20f1`, Universal Render Pipeline 17.3, C# MonoBehaviour, Legacy Input Manager, Unity Test Framework 1.6, uGUI

## Global Constraints

- Unity 버전은 `6000.3.20f1`이다.
- Universal 3D 템플릿과 한 개의 게임플레이 씬을 사용한다.
- 모든 C# 파일명과 클래스명은 정확히 `h980220_`로 시작한다.
- 입력은 Legacy Input Manager만 사용하며 `Input.GetKey`, `Input.GetKeyDown`, `KeyCode`를 사용한다.
- 외부 아트 애셋, 모델, 텍스처, 애니메이션, 아이콘, 음원과 파티클을 사용하지 않는다.
- 캐릭터, 적, 맵, 투사체는 Unity 기본 3D 프리미티브와 uGUI로만 만든다.
- 소리, 원형 파동, 화면 플래시, 공격 사거리와 조준선을 표시하지 않는다.
- 플레이어는 머리·몸통·양쪽 허벅지·양쪽 종아리의 육면체 6개로만 표현한다.
- 예상 플레이 시간은 초보자 기준 3~4분이며 하드 타이머는 두지 않는다.
- 제출 압축 파일은 `Library`, `Temp`, `Logs`, `obj`, IDE 캐시와 심볼을 제외해 100MB 미만으로 유지한다.

## File Map

### 프로젝트 설정과 저장소

- `.gitignore`: Unity 생성물, IDE 파일, 빌드 결과, 테스트 결과 제외
- `Packages/manifest.json`: 새 Input System 의존성 제거
- `ProjectSettings/ProjectSettings.asset`: `activeInputHandler: 0`
- `ProjectSettings/EditorBuildSettings.asset`: 생성된 게임 씬 한 개 등록

### Runtime

- `Assets/HOPAK VIRUS/Runtime/h980220_HopakVirus.Runtime.asmdef`: 런타임 어셈블리
- `Assets/HOPAK VIRUS/Runtime/h980220_RhythmState.cs`: 발 상태, 타이밍 판정, 연속 성공, 속도 공식
- `Assets/HOPAK VIRUS/Runtime/h980220_HopakPose.cs`: 네 다리 육면체의 절차적 포즈 값
- `Assets/HOPAK VIRUS/Runtime/h980220_PlayerRhythmController.cs`: Legacy 입력, 전진, 회전, 다리 Transform 적용
- `Assets/HOPAK VIRUS/Runtime/h980220_PlayerCombat.cs`: Space 발사와 재사용 대기
- `Assets/HOPAK VIRUS/Runtime/h980220_PlayerInfection.cs`: 감염도 3칸, 피격 무적, 넉백, 색상, 패배
- `Assets/HOPAK VIRUS/Runtime/h980220_Projectile.cs`: 바이러스·치료제 Sphere 이동과 충돌
- `Assets/HOPAK VIRUS/Runtime/h980220_EnemyController.cs`: 일반·원거리·엘리트 행동과 감염 춤
- `Assets/HOPAK VIRUS/Runtime/h980220_RoomController.cs`: 미감염 적 추적과 문 개방
- `Assets/HOPAK VIRUS/Runtime/h980220_FollowCamera.cs`: 높은 3인칭 자동 추적
- `Assets/HOPAK VIRUS/Runtime/h980220_GameManager.cs`: 시작, 방 전환, 승리, 패배, 재시작, UI

### Editor

- `Assets/HOPAK VIRUS/Editor/h980220_HopakVirus.Editor.asmdef`: Editor 전용 어셈블리
- `Assets/HOPAK VIRUS/Editor/h980220_GameSceneBuilder.cs`: 프리미티브, Material, UI와 세 방 생성
- `Assets/HOPAK VIRUS/Editor/h980220_BuildAutomation.cs`: 씬 재생성 및 Windows 빌드

### Tests

- `Assets/HOPAK VIRUS/Tests/EditMode/h980220_HopakVirus.EditModeTests.asmdef`
- `Assets/HOPAK VIRUS/Tests/EditMode/h980220_ProjectComplianceTests.cs`
- `Assets/HOPAK VIRUS/Tests/EditMode/h980220_RhythmStateTests.cs`
- `Assets/HOPAK VIRUS/Tests/EditMode/h980220_HopakPoseTests.cs`
- `Assets/HOPAK VIRUS/Tests/EditMode/h980220_PlayerInfectionTests.cs`
- `Assets/HOPAK VIRUS/Tests/EditMode/h980220_EnemyAndRoomTests.cs`
- `Assets/HOPAK VIRUS/Tests/PlayMode/h980220_HopakVirus.PlayModeTests.asmdef`
- `Assets/HOPAK VIRUS/Tests/PlayMode/h980220_GameplaySmokeTests.cs`

### Generated game content

- `Assets/HOPAK VIRUS/Scenes/HOPAK VIRUS.unity`
- `Assets/HOPAK VIRUS/Materials/*.mat`
- `Assets/HOPAK VIRUS/Prefabs/VirusProjectile.prefab`
- `Assets/HOPAK VIRUS/Prefabs/CureProjectile.prefab`

---

### Task 1: Legacy Input 및 프로젝트 규정 고정

**Files:**
- Create: `.gitignore`
- Create: `Assets/HOPAK VIRUS/Runtime/h980220_HopakVirus.Runtime.asmdef`
- Create: `Assets/HOPAK VIRUS/Tests/EditMode/h980220_HopakVirus.EditModeTests.asmdef`
- Create: `Assets/HOPAK VIRUS/Tests/EditMode/h980220_ProjectComplianceTests.cs`
- Modify: `Packages/manifest.json`
- Modify: `ProjectSettings/ProjectSettings.asset`
- Delete: `Assets/InputSystem_Actions.inputactions`
- Delete: `Assets/InputSystem_Actions.inputactions.meta`
- Delete: `Assets/TutorialInfo/`
- Delete: `Assets/TutorialInfo.meta`
- Delete: `Assets/Readme.asset`
- Delete: `Assets/Readme.asset.meta`
- Delete: `Assets/Scenes/`
- Delete: `Assets/Scenes.meta`

**Interfaces:**
- Consumes: 현재 Unity `6000.3.20f1` Universal 3D 프로젝트
- Produces: Legacy Input 전용의 깨끗한 프로젝트와 `h980220_HopakVirus.Runtime` 어셈블리

- [ ] **Step 1: Unity 생성물을 제외하는 `.gitignore` 작성**

```gitignore
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
TestResults/
.vs/
.vscode/
*.csproj
*.sln
*.slnx
*.user
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
sysinfo.txt
```

- [ ] **Step 2: 런타임과 EditMode 테스트 asmdef 생성**

`Assets/HOPAK VIRUS/Runtime/h980220_HopakVirus.Runtime.asmdef`:

```json
{
  "name": "h980220_HopakVirus.Runtime",
  "rootNamespace": "",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

`Assets/HOPAK VIRUS/Tests/EditMode/h980220_HopakVirus.EditModeTests.asmdef`:

```json
{
  "name": "h980220_HopakVirus.EditModeTests",
  "rootNamespace": "",
  "references": ["h980220_HopakVirus.Runtime"],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "versionDefines": [],
  "noEngineReferences": false,
  "optionalUnityReferences": ["TestAssemblies"]
}
```

- [ ] **Step 3: 규정 위반을 잡는 실패 테스트 작성**

`Assets/HOPAK VIRUS/Tests/EditMode/h980220_ProjectComplianceTests.cs`:

```csharp
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class h980220_ProjectComplianceTests
{
    private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

    [Test]
    public void EveryAssetScriptUsesRequiredPrefix()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            Assert.That(fileName, Does.StartWith("h980220_"), path);
        }
    }

    [Test]
    public void LegacyInputIsExclusive()
    {
        string settings = File.ReadAllText(Path.Combine(ProjectRoot, "ProjectSettings", "ProjectSettings.asset"));
        string manifest = File.ReadAllText(Path.Combine(ProjectRoot, "Packages", "manifest.json"));
        Assert.That(settings, Does.Contain("activeInputHandler: 0"));
        Assert.That(manifest, Does.Not.Contain("com.unity.inputsystem"));
        Assert.That(File.Exists(Path.Combine(Application.dataPath, "InputSystem_Actions.inputactions")), Is.False);
    }
}
```

- [ ] **Step 4: EditMode 테스트를 실행해 규정 테스트가 실패하는지 확인**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\Documents\Unity\HOPAK VIRUS' -runTests -testPlatform EditMode -testResults 'D:\Documents\Unity\HOPAK VIRUS\TestResults\editmode.xml' -logFile 'D:\Documents\Unity\HOPAK VIRUS\Logs\editmode-tests.log'
```

Expected: `EveryAssetScriptUsesRequiredPrefix`가 `Readme.cs`에서 실패하고 `LegacyInputIsExclusive`가 `activeInputHandler: 2`와 Input System 의존성 때문에 실패한다.

- [ ] **Step 5: 템플릿 예제와 Input Actions를 제거하고 Legacy Input으로 전환**

`Packages/manifest.json`에서 다음 한 줄을 제거한다.

```json
"com.unity.inputsystem": "1.19.0",
```

`ProjectSettings/ProjectSettings.asset`을 다음 값으로 수정한다.

```yaml
activeInputHandler: 0
```

위 Files 목록의 `Assets/TutorialInfo`, `Assets/Readme.asset`, `Assets/InputSystem_Actions.inputactions`, 기본 `Assets/Scenes`와 각 `.meta`를 제거한 뒤 Unity를 한 번 batchmode로 열어 `Packages/packages-lock.json`을 다시 해석시킨다.

- [ ] **Step 6: 규정 테스트가 통과하는지 확인**

Run: Step 4의 Unity 명령

Expected: `EveryAssetScriptUsesRequiredPrefix`와 `LegacyInputIsExclusive` 모두 PASS, 컴파일 오류 0개.

- [ ] **Step 7: 커밋**

```powershell
git add .gitignore Packages/manifest.json Packages/packages-lock.json ProjectSettings/ProjectSettings.asset 'Assets/HOPAK VIRUS' Assets
git commit -m 'chore: enforce legacy input and project rules'
```

---

### Task 2: 리듬 판정과 속도 증가 모델

**Files:**
- Create: `Assets/HOPAK VIRUS/Runtime/h980220_RhythmState.cs`
- Create: `Assets/HOPAK VIRUS/Tests/EditMode/h980220_RhythmStateTests.cs`

**Interfaces:**
- Consumes: `h980220_Leg` 입력과 경과 시간
- Produces: `h980220_RhythmState.RegisterInput(h980220_Leg)`, `Tick(float)`, `CurrentSpeed`, `SuccessStreak`, `IsMoving`

- [ ] **Step 1: 리듬 성공·실패·속도 제한 테스트 작성**

```csharp
using NUnit.Framework;

public sealed class h980220_RhythmStateTests
{
    private h980220_RhythmState state;

    [SetUp]
    public void SetUp() => state = new h980220_RhythmState(0.5f, 0.2f, 2f, 6f, 4);

    [Test]
    public void FirstInputStartsWithoutMovement()
    {
        Assert.That(state.RegisterInput(h980220_Leg.Left), Is.EqualTo(h980220_RhythmInputResult.Started));
        Assert.That(state.IsMoving, Is.False);
        Assert.That(state.SuccessStreak, Is.Zero);
    }

    [Test]
    public void AlternatingInsideLandingWindowRaisesSpeed()
    {
        state.RegisterInput(h980220_Leg.Left);
        state.Tick(0.35f);
        Assert.That(state.RegisterInput(h980220_Leg.Right), Is.EqualTo(h980220_RhythmInputResult.Success));
        Assert.That(state.SuccessStreak, Is.EqualTo(1));
        Assert.That(state.CurrentSpeed, Is.EqualTo(3f).Within(0.001f));
    }

    [Test]
    public void FourSuccessesReachConfiguredMaximum()
    {
        state.RegisterInput(h980220_Leg.Left);
        h980220_Leg next = h980220_Leg.Right;
        for (int i = 0; i < 4; i++)
        {
            state.Tick(0.35f);
            state.RegisterInput(next);
            next = next == h980220_Leg.Left ? h980220_Leg.Right : h980220_Leg.Left;
        }
        Assert.That(state.CurrentSpeed, Is.EqualTo(6f).Within(0.001f));
    }

    [Test]
    public void EarlyOrRepeatedFootResetsStreak()
    {
        state.RegisterInput(h980220_Leg.Left);
        state.Tick(0.35f);
        state.RegisterInput(h980220_Leg.Right);
        state.Tick(0.1f);
        Assert.That(state.RegisterInput(h980220_Leg.Left), Is.EqualTo(h980220_RhythmInputResult.Failed));
        Assert.That(state.SuccessStreak, Is.Zero);
        Assert.That(state.IsMoving, Is.False);
    }

    [Test]
    public void MissingLandingWindowStopsMovement()
    {
        state.RegisterInput(h980220_Leg.Left);
        state.Tick(0.51f);
        Assert.That(state.ActiveLeg, Is.EqualTo(h980220_Leg.None));
        Assert.That(state.IsMoving, Is.False);
    }
}
```

- [ ] **Step 2: 테스트를 실행해 타입 미정의 실패를 확인**

Run: Task 1 Step 4의 Unity 명령

Expected: `h980220_RhythmState`, `h980220_Leg`, `h980220_RhythmInputResult` 미정의 컴파일 실패.

- [ ] **Step 3: 순수 리듬 상태 구현**

`Assets/HOPAK VIRUS/Runtime/h980220_RhythmState.cs`:

```csharp
using System;
using UnityEngine;

public enum h980220_Leg { None, Left, Right }
public enum h980220_RhythmInputResult { Started, Success, Failed }

[Serializable]
public sealed class h980220_RhythmState
{
    public h980220_Leg ActiveLeg { get; private set; }
    public int SuccessStreak { get; private set; }
    public float StepElapsed { get; private set; }
    public bool IsMoving { get; private set; }
    public float CurrentSpeed => Mathf.Lerp(baseSpeed, maxSpeed,
        Mathf.Clamp01((float)SuccessStreak / successesToMaxSpeed));

    private readonly float stepDuration;
    private readonly float successWindow;
    private readonly float baseSpeed;
    private readonly float maxSpeed;
    private readonly int successesToMaxSpeed;

    public h980220_RhythmState(float stepDuration, float successWindow,
        float baseSpeed, float maxSpeed, int successesToMaxSpeed)
    {
        this.stepDuration = Mathf.Max(0.05f, stepDuration);
        this.successWindow = Mathf.Clamp(successWindow, 0.01f, this.stepDuration);
        this.baseSpeed = Mathf.Max(0f, baseSpeed);
        this.maxSpeed = Mathf.Max(this.baseSpeed, maxSpeed);
        this.successesToMaxSpeed = Mathf.Max(1, successesToMaxSpeed);
        Reset();
    }

    public h980220_RhythmInputResult RegisterInput(h980220_Leg leg)
    {
        if (leg == h980220_Leg.None)
            throw new ArgumentOutOfRangeException(nameof(leg));

        if (ActiveLeg == h980220_Leg.None)
        {
            ActiveLeg = leg;
            StepElapsed = 0f;
            IsMoving = false;
            return h980220_RhythmInputResult.Started;
        }

        bool opposite = ActiveLeg != leg;
        bool inWindow = StepElapsed >= stepDuration - successWindow && StepElapsed <= stepDuration;
        if (!opposite || !inWindow)
        {
            Reset();
            return h980220_RhythmInputResult.Failed;
        }

        ActiveLeg = leg;
        StepElapsed = 0f;
        SuccessStreak++;
        IsMoving = true;
        return h980220_RhythmInputResult.Success;
    }

    public void Tick(float deltaTime)
    {
        if (ActiveLeg == h980220_Leg.None)
            return;
        StepElapsed += Mathf.Max(0f, deltaTime);
        if (StepElapsed > stepDuration)
            Reset();
    }

    public float NormalizedStep => ActiveLeg == h980220_Leg.None
        ? 0f : Mathf.Clamp01(StepElapsed / stepDuration);

    public void Reset()
    {
        ActiveLeg = h980220_Leg.None;
        SuccessStreak = 0;
        StepElapsed = 0f;
        IsMoving = false;
    }
}
```

- [ ] **Step 4: 리듬 테스트가 통과하는지 확인**

Run: Task 1 Step 4의 Unity 명령

Expected: 리듬 테스트 5개 PASS.

- [ ] **Step 5: 커밋**

```powershell
git add 'Assets/HOPAK VIRUS/Runtime/h980220_RhythmState.cs' 'Assets/HOPAK VIRUS/Tests/EditMode/h980220_RhythmStateTests.cs'
git commit -m 'feat: add hopak rhythm state and speed curve'
```

---

### Task 3: 육면체 다리 포즈와 플레이어 이동

**Files:**
- Create: `Assets/HOPAK VIRUS/Runtime/h980220_HopakPose.cs`
- Create: `Assets/HOPAK VIRUS/Runtime/h980220_PlayerRhythmController.cs`
- Create: `Assets/HOPAK VIRUS/Tests/EditMode/h980220_HopakPoseTests.cs`

**Interfaces:**
- Consumes: Task 2의 `h980220_RhythmState`
- Produces: `h980220_HopakPose.Evaluate`, `h980220_PlayerRhythmController.SetInputEnabled(bool)`, `CurrentSpeed`, `SuccessStreak`

- [ ] **Step 1: 다리 포즈 테스트 작성**

```csharp
using NUnit.Framework;

public sealed class h980220_HopakPoseTests
{
    [Test]
    public void LeftLegAtMidStepRaisesOnlyLeftSegments()
    {
        h980220_LegPose pose = h980220_HopakPose.Evaluate(h980220_Leg.Left, 0.5f);
        Assert.That(pose.LeftThighX, Is.LessThan(-50f));
        Assert.That(pose.LeftShinX, Is.GreaterThan(60f));
        Assert.That(pose.RightThighX, Is.EqualTo(0f).Within(0.01f));
        Assert.That(pose.RightShinX, Is.EqualTo(0f).Within(0.01f));
    }

    [Test]
    public void NeutralPoseHasStraightLegs()
    {
        h980220_LegPose pose = h980220_HopakPose.Evaluate(h980220_Leg.None, 0f);
        Assert.That(pose.LeftThighX, Is.Zero);
        Assert.That(pose.LeftShinX, Is.Zero);
        Assert.That(pose.RightThighX, Is.Zero);
        Assert.That(pose.RightShinX, Is.Zero);
    }
}
```

- [ ] **Step 2: 테스트를 실행해 포즈 타입 미정의 실패 확인**

Run: Task 1 Step 4의 Unity 명령

Expected: `h980220_HopakPose`와 `h980220_LegPose` 미정의 컴파일 실패.

- [ ] **Step 3: 포즈 계산 구현**

```csharp
using UnityEngine;

public readonly struct h980220_LegPose
{
    public readonly float LeftThighX, LeftShinX, RightThighX, RightShinX;
    public h980220_LegPose(float lt, float ls, float rt, float rs)
    {
        LeftThighX = lt; LeftShinX = ls; RightThighX = rt; RightShinX = rs;
    }
}

public static class h980220_HopakPose
{
    public static h980220_LegPose Evaluate(h980220_Leg activeLeg, float normalizedStep)
    {
        if (activeLeg == h980220_Leg.None)
            return new h980220_LegPose(0f, 0f, 0f, 0f);
        float lift = Mathf.Sin(Mathf.Clamp01(normalizedStep) * Mathf.PI);
        float thigh = -70f * lift;
        float shin = 90f * lift;
        return activeLeg == h980220_Leg.Left
            ? new h980220_LegPose(thigh, shin, 0f, 0f)
            : new h980220_LegPose(0f, 0f, thigh, shin);
    }
}
```

- [ ] **Step 4: 플레이어 리듬 컨트롤러 구현**

`h980220_PlayerRhythmController`는 Inspector 필드 `baseMoveSpeed`, `maxMoveSpeed`, `successesToMaxSpeed`, `stepDuration`, `successWindow`, `turnSpeed`와 네 다리 Transform을 가진다. `Awake`에서 `h980220_RhythmState`를 만들고, `Update`에서 아래 순서로 처리한다.

```csharp
private void Update()
{
    if (!inputEnabled) return;
    rhythm.Tick(Time.deltaTime);
    if (Input.GetKeyDown(KeyCode.A)) HandleLeg(h980220_Leg.Left);
    if (Input.GetKeyDown(KeyCode.D)) HandleLeg(h980220_Leg.Right);

    float turn = 0f;
    if (Input.GetKey(KeyCode.LeftArrow)) turn -= 1f;
    if (Input.GetKey(KeyCode.RightArrow)) turn += 1f;
    transform.Rotate(0f, turn * turnSpeed * Time.deltaTime, 0f);

    if (rhythm.IsMoving)
        characterController.Move(transform.forward * rhythm.CurrentSpeed * Time.deltaTime);

    ApplyPose(h980220_HopakPose.Evaluate(rhythm.ActiveLeg, rhythm.NormalizedStep));
}
```

`ApplyPose`는 각 육면체의 `localRotation`을 `Quaternion.Euler(pose 값, 0, 0)`으로 설정한다. `OnValidate`에서 `stepDuration >= 0.05`, `successWindow <= stepDuration`, `maxMoveSpeed >= baseMoveSpeed`, `successesToMaxSpeed >= 1`로 보정한다. 공개 읽기 전용 프로퍼티 `CurrentSpeed`, `SuccessStreak`와 공개 메서드 `SetInputEnabled(bool)`를 제공한다.

- [ ] **Step 5: EditMode 테스트와 Unity 컴파일 확인**

Run: Task 1 Step 4의 Unity 명령

Expected: 포즈 테스트 2개와 기존 테스트 모두 PASS, 컴파일 오류 0개.

- [ ] **Step 6: 커밋**

```powershell
git add 'Assets/HOPAK VIRUS/Runtime/h980220_HopakPose.cs' 'Assets/HOPAK VIRUS/Runtime/h980220_PlayerRhythmController.cs' 'Assets/HOPAK VIRUS/Tests/EditMode/h980220_HopakPoseTests.cs'
git commit -m 'feat: add cube-leg hopak movement'
```

---

### Task 4: 바이러스 공격과 플레이어 감염도

**Files:**
- Create: `Assets/HOPAK VIRUS/Runtime/h980220_Projectile.cs`
- Create: `Assets/HOPAK VIRUS/Runtime/h980220_PlayerCombat.cs`
- Create: `Assets/HOPAK VIRUS/Runtime/h980220_PlayerInfection.cs`
- Create: `Assets/HOPAK VIRUS/Tests/EditMode/h980220_PlayerInfectionTests.cs`

**Interfaces:**
- Consumes: GameManager의 `Lose()` 콜백, Enemy의 `ReceiveVirusHit()`
- Produces: `TryReceiveCure(Vector3)`, `ResetInfection()`, `Fire()`, `Initialize(kind, direction, speed, range)`

- [ ] **Step 1: 치료제 세 번과 중복 피격 방지 테스트 작성**

```csharp
using NUnit.Framework;
using UnityEngine;

public sealed class h980220_PlayerInfectionTests
{
    [Test]
    public void ThirdAcceptedCureHitRemovesInfection()
    {
        var go = new GameObject("Player");
        var infection = go.AddComponent<h980220_PlayerInfection>();
        infection.ResetInfection();
        Assert.That(infection.ReceiveCureAtTime(Vector3.back, 0f), Is.True);
        Assert.That(infection.ReceiveCureAtTime(Vector3.back, 1.1f), Is.True);
        Assert.That(infection.ReceiveCureAtTime(Vector3.back, 2.2f), Is.True);
        Assert.That(infection.RemainingInfection, Is.Zero);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void HitDuringInvulnerabilityIsIgnored()
    {
        var go = new GameObject("Player");
        var infection = go.AddComponent<h980220_PlayerInfection>();
        infection.ResetInfection();
        Assert.That(infection.ReceiveCureAtTime(Vector3.back, 0f), Is.True);
        Assert.That(infection.ReceiveCureAtTime(Vector3.back, 0.2f), Is.False);
        Assert.That(infection.RemainingInfection, Is.EqualTo(2));
        Object.DestroyImmediate(go);
    }
}
```

- [ ] **Step 2: 테스트를 실행해 클래스 미정의 실패 확인**

Run: Task 1 Step 4의 Unity 명령

Expected: `h980220_PlayerInfection` 미정의 컴파일 실패.

- [ ] **Step 3: 플레이어 감염 상태 구현**

`h980220_PlayerInfection`에 `maxInfection = 3`, `invulnerabilitySeconds = 1f`, `knockbackDistance = 1.5f`, 플레이어 Renderer 배열, 보라색·일반인 Color, HUD Image 배열과 `Action Cured`를 둔다. 핵심 메서드는 다음 계약을 지킨다.

```csharp
public bool ReceiveCureAtTime(Vector3 sourcePosition, float now)
{
    if (RemainingInfection <= 0 || now < invulnerableUntil) return false;
    invulnerableUntil = now + invulnerabilitySeconds;
    RemainingInfection--;
    Vector3 away = transform.position - sourcePosition;
    away.y = 0f;
    if (away.sqrMagnitude > 0.001f && characterController != null)
        characterController.Move(away.normalized * knockbackDistance);
    RefreshVisuals();
    if (RemainingInfection == 0) Cured?.Invoke();
    return true;
}

public bool TryReceiveCure(Vector3 sourcePosition) => ReceiveCureAtTime(sourcePosition, Time.time);
```

`RefreshVisuals`는 `RemainingInfection / 3f`로 보라색과 일반인 색을 보간하고, HUD Image는 인덱스가 남은 감염도보다 작은 경우에만 활성화한다. `ResetInfection`은 3칸과 강한 보라색을 복구한다.

- [ ] **Step 4: 투사체와 플레이어 공격 구현**

`h980220_ProjectileKind`는 `Virus`, `Cure` 두 값이다. `h980220_Projectile.Initialize`은 종류, 방향, 속도, 최대 거리를 저장한다. `Update`에서 직선 이동하고 출발점과의 거리가 최대 거리 이상이면 제거한다. `OnTriggerEnter`에서 Virus는 `h980220_EnemyController.ReceiveVirusHit()`, Cure는 `h980220_PlayerInfection.TryReceiveCure(transform.position)`를 호출하고 유효 대상 또는 벽과 충돌하면 제거한다.

`h980220_PlayerCombat`은 `Update`에서 `inputEnabled && Input.GetKeyDown(KeyCode.Space)`일 때만 `Fire()`를 호출한다. `Fire()`는 재사용 대기시간을 확인하고, 보라색 Sphere prefab을 fire point 정면으로 생성해 `Virus`, 속도 `10`, 최대 거리 `4`로 초기화한다. 리듬 상태는 참조하거나 검사하지 않는다.

- [ ] **Step 5: 감염도 테스트와 컴파일 확인**

Run: Task 1 Step 4의 Unity 명령

Expected: 감염도 테스트 2개와 기존 테스트 모두 PASS.

- [ ] **Step 6: 커밋**

```powershell
git add 'Assets/HOPAK VIRUS/Runtime/h980220_Projectile.cs' 'Assets/HOPAK VIRUS/Runtime/h980220_PlayerCombat.cs' 'Assets/HOPAK VIRUS/Runtime/h980220_PlayerInfection.cs' 'Assets/HOPAK VIRUS/Tests/EditMode/h980220_PlayerInfectionTests.cs'
git commit -m 'feat: add virus projectiles and cure state'
```

---

### Task 5: 적 감염과 방 완료

**Files:**
- Create: `Assets/HOPAK VIRUS/Runtime/h980220_EnemyController.cs`
- Create: `Assets/HOPAK VIRUS/Runtime/h980220_RoomController.cs`
- Create: `Assets/HOPAK VIRUS/Tests/EditMode/h980220_EnemyAndRoomTests.cs`

**Interfaces:**
- Consumes: Task 4의 Projectile과 PlayerInfection
- Produces: `EnemyType`, `Configure`, `ReceiveVirusHit`, `SetCombatEnabled(bool)`, `Infected` 이벤트, `RoomController.Initialize`, `RoomController.SetCombatEnabled(bool)`, `Completed` 이벤트

- [ ] **Step 1: 적 필요 명중 수와 방 완료 테스트 작성**

```csharp
using NUnit.Framework;
using UnityEngine;

public sealed class h980220_EnemyAndRoomTests
{
    [Test]
    public void BasicEnemyInfectsInOneHitAndEliteInThree()
    {
        var basicGo = new GameObject("Basic");
        var eliteGo = new GameObject("Elite");
        var basic = basicGo.AddComponent<h980220_EnemyController>();
        var elite = eliteGo.AddComponent<h980220_EnemyController>();
        basic.Configure(h980220_EnemyType.Basic, 1);
        elite.Configure(h980220_EnemyType.Elite, 3);
        basic.ReceiveVirusHit();
        elite.ReceiveVirusHit(); elite.ReceiveVirusHit();
        Assert.That(basic.IsInfected, Is.True);
        Assert.That(elite.IsInfected, Is.False);
        elite.ReceiveVirusHit();
        Assert.That(elite.IsInfected, Is.True);
        Object.DestroyImmediate(basicGo);
        Object.DestroyImmediate(eliteGo);
    }

    [Test]
    public void RoomCompletesOnceAfterEveryEnemyIsInfected()
    {
        var roomGo = new GameObject("Room");
        var first = new GameObject("First").AddComponent<h980220_EnemyController>();
        var second = new GameObject("Second").AddComponent<h980220_EnemyController>();
        first.Configure(h980220_EnemyType.Basic, 1);
        second.Configure(h980220_EnemyType.Basic, 1);
        var room = roomGo.AddComponent<h980220_RoomController>();
        int completionCount = 0;
        room.Completed += _ => completionCount++;
        room.Initialize(0, new[] { first, second }, null);
        first.ReceiveVirusHit(); second.ReceiveVirusHit(); second.ReceiveVirusHit();
        Assert.That(room.RemainingEnemies, Is.Zero);
        Assert.That(completionCount, Is.EqualTo(1));
        Object.DestroyImmediate(roomGo);
        Object.DestroyImmediate(first.gameObject);
        Object.DestroyImmediate(second.gameObject);
    }
}
```

- [ ] **Step 2: 테스트를 실행해 적·방 타입 미정의 실패 확인**

Run: Task 1 Step 4의 Unity 명령

Expected: `h980220_EnemyController`, `h980220_EnemyType`, `h980220_RoomController` 미정의 컴파일 실패.

- [ ] **Step 3: 적 컨트롤러 구현**

`h980220_EnemyType`은 `Basic`, `Ranged`, `Elite`다. `Configure(type, requiredHits)`는 명중 수를 최소 1로 설정한다. `ReceiveVirusHit`은 이미 감염된 경우 아무 일도 하지 않으며, 명중 수를 증가시킨 뒤 Renderer가 연결되어 있을 때만 청록색에서 보라색으로 보간한다. 필요 명중 수에 도달하면 정확히 한 번 `public event Action<h980220_EnemyController> Infected`를 호출한다. `SetCombatEnabled(bool)`은 아직 감염되지 않은 적의 추격과 치료제 공격만 켜거나 끈다.

활성 적의 `Update` 행동은 다음과 같다.

```csharp
switch (enemyType)
{
    case h980220_EnemyType.Basic:
        MoveTowardPlayer();
        TryContactCure();
        break;
    case h980220_EnemyType.Ranged:
        FacePlayer();
        TryFireCure();
        break;
    case h980220_EnemyType.Elite:
        MoveTowardPlayer();
        FacePlayer();
        TryContactCure();
        TryFireCure();
        break;
}
```

`MoveTowardPlayer`는 Enemy의 CharacterController로 수평 방향을 이동해 벽을 통과하지 않게 한다. `TryContactCure`는 수평 거리와 Physics linecast로 벽이 없는지 확인한 뒤 PlayerInfection을 호출한다. `TryFireCure`는 흰 Sphere prefab을 `Cure`, 속도 `7`, 최대 거리 `12`로 초기화한다. 감염 후에는 모든 전투 행동을 멈추고 `sin(Time.time)` 기반의 좌우 기울기와 상하 움직임만 반복한다.

- [ ] **Step 4: 방 컨트롤러 구현**

`Initialize(int roomIndex, IEnumerable<h980220_EnemyController> enemies, Transform exitDoor)`에서 중복을 제거한 적 목록을 만들고 각 `Infected` 이벤트를 구독한다. `RemainingEnemies`가 0이 되면 `completed` 플래그를 먼저 설정하고, 출구 문이 있으면 위로 4m 이동시키며 `public event Action<int> Completed`를 정확히 한 번 호출한다. `SetCombatEnabled(bool)`은 방에 등록된 모든 적의 동명 메서드를 호출한다.

- [ ] **Step 5: 적·방 테스트와 컴파일 확인**

Run: Task 1 Step 4의 Unity 명령

Expected: 적·방 테스트 2개와 기존 테스트 모두 PASS.

- [ ] **Step 6: 커밋**

```powershell
git add 'Assets/HOPAK VIRUS/Runtime/h980220_EnemyController.cs' 'Assets/HOPAK VIRUS/Runtime/h980220_RoomController.cs' 'Assets/HOPAK VIRUS/Tests/EditMode/h980220_EnemyAndRoomTests.cs'
git commit -m 'feat: add cure enemies and room completion'
```

---

### Task 6: 카메라, UI와 전체 게임 상태

**Files:**
- Create: `Assets/HOPAK VIRUS/Runtime/h980220_FollowCamera.cs`
- Create: `Assets/HOPAK VIRUS/Runtime/h980220_GameManager.cs`

**Interfaces:**
- Consumes: PlayerRhythmController, PlayerCombat, PlayerInfection.Cured, RoomController.Completed
- Produces: `GameState`, `StartGame`, `Win`, `Lose`, `SetCurrentRoom`, 결과 UI와 입력 잠금

- [ ] **Step 1: 높은 자동 추적 카메라 구현**

`h980220_FollowCamera`는 `target`, `offset = (0, 7, -9)`, `positionSmooth = 8`, `rotationSmooth = 10`을 가진다. `LateUpdate`에서 `target.TransformPoint(offset)`으로 목표 위치를 계산하고 `Vector3.Lerp`로 이동하며, `Quaternion.LookRotation(target.position + Vector3.up * 1.5f - transform.position)`을 `Quaternion.Slerp`로 적용한다. `SetTarget(Transform)`과 승리 시 offset을 `(0, 10, -14)`로 바꾸는 `SetVictoryView()`를 제공한다.

- [ ] **Step 2: 게임 상태 관리자 구현**

`h980220_GameState`는 `Title`, `Playing`, `Won`, `Cured` 네 값이다. `h980220_GameManager`는 시작 패널, HUD 패널, 결과 패널, 결과 Text, 방 Text, PlayerRhythmController, PlayerCombat, PlayerInfection, FollowCamera, RoomController 배열을 참조한다.

```csharp
private void Update()
{
    if (State == h980220_GameState.Title && Input.GetKeyDown(KeyCode.Return)) StartGame();
    if ((State == h980220_GameState.Won || State == h980220_GameState.Cured) && Input.GetKeyDown(KeyCode.R))
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
}

public void StartGame()
{
    State = h980220_GameState.Playing;
    titlePanel.SetActive(false);
    hudPanel.SetActive(true);
    SetPlayerInput(true);
    SetCurrentRoom(0);
}

public void Lose()
{
    if (State != h980220_GameState.Playing) return;
    State = h980220_GameState.Cured;
    Finish("CURED...");
}

private void Win()
{
    if (State != h980220_GameState.Playing) return;
    State = h980220_GameState.Won;
    followCamera.SetVictoryView();
    Finish("HOPAK VIRUS SPREAD COMPLETE");
}
```

`Awake`에서 PlayerInfection.Cured와 각 RoomController.Completed를 구독한다. 모든 방에 `SetCombatEnabled(false)`를 호출해 Title에서 AI를 멈춘다. `StartGame`과 `SetCurrentRoom`은 현재 방 하나에만 `SetCombatEnabled(true)`를 호출한다. 방 완료 인덱스가 마지막이면 `Win`, 아니면 `SetCurrentRoom(index + 1)`을 호출한다. `Finish`는 PlayerRhythmController와 PlayerCombat 입력 및 모든 방 전투를 끄고 HUD를 숨기며 결과 패널과 `R: RESTART`를 표시한다.

- [ ] **Step 3: Unity 컴파일과 기존 테스트 확인**

Run: Task 1 Step 4의 Unity 명령

Expected: 컴파일 오류 0개, 모든 EditMode 테스트 PASS.

- [ ] **Step 4: 커밋**

```powershell
git add 'Assets/HOPAK VIRUS/Runtime/h980220_FollowCamera.cs' 'Assets/HOPAK VIRUS/Runtime/h980220_GameManager.cs'
git commit -m 'feat: add camera and complete game flow'
```

---

### Task 7: 세 방과 UI를 기본 프리미티브로 생성

**Files:**
- Create: `Assets/HOPAK VIRUS/Editor/h980220_HopakVirus.Editor.asmdef`
- Create: `Assets/HOPAK VIRUS/Editor/h980220_GameSceneBuilder.cs`
- Create: `Assets/HOPAK VIRUS/Editor/h980220_BuildAutomation.cs`
- Generate: `Assets/HOPAK VIRUS/Scenes/HOPAK VIRUS.unity`
- Generate: `Assets/HOPAK VIRUS/Materials/*.mat`
- Generate: `Assets/HOPAK VIRUS/Prefabs/VirusProjectile.prefab`
- Generate: `Assets/HOPAK VIRUS/Prefabs/CureProjectile.prefab`
- Modify: `ProjectSettings/EditorBuildSettings.asset`

**Interfaces:**
- Consumes: Tasks 3~6의 모든 Runtime 컴포넌트
- Produces: `h980220_GameSceneBuilder.BuildScene()`, 완전히 연결된 단일 플레이 씬

- [ ] **Step 1: Editor asmdef 생성**

```json
{
  "name": "h980220_HopakVirus.Editor",
  "rootNamespace": "",
  "references": ["h980220_HopakVirus.Runtime"],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

- [ ] **Step 2: Scene Builder의 공통 생성 도우미 작성**

`h980220_GameSceneBuilder`에 `[MenuItem("HOPAK VIRUS/Build Game Scene")] public static void BuildScene()`을 만들고 다음 도우미를 구현한다.

```csharp
private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
{
    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
    go.name = name;
    go.transform.SetParent(parent);
    go.transform.position = position;
    go.transform.localScale = scale;
    go.GetComponent<Renderer>().sharedMaterial = material;
    return go;
}

private static Material MaterialAt(string fileName, Color color)
{
    string path = $"Assets/HOPAK VIRUS/Materials/{fileName}.mat";
    Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
    if (material == null)
    {
        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        AssetDatabase.CreateAsset(material, path);
    }
    material.color = color;
    EditorUtility.SetDirty(material);
    return material;
}
```

Material은 `Purple (0.55, 0.10, 0.75)`, `Cyan (0.20, 0.85, 0.90)`, `White (0.95, 0.95, 1.00)`, `GrayFloor (0.18, 0.20, 0.23)`, `GrayWall (0.30, 0.32, 0.36)`, `Skin (0.76, 0.65, 0.55)` 여섯 개만 만든다. AudioSource, ParticleSystem, TrailRenderer, LineRenderer는 생성하지 않는다.

- [ ] **Step 3: 플레이어와 투사체 원본 생성**

플레이어 root 위치는 `(0, 0, -6)`이며 `CharacterController.center = (0, 1.75, 0)`, `height = 3.5`, `radius = 0.6`으로 붙인다. root 아래에 다음 여섯 Cube를 만든다.

```text
Head             localPosition (0, 3.2, 0)   localScale (0.9, 0.9, 0.9)
Torso            localPosition (0, 2.1, 0)   localScale (1.2, 1.4, 0.8)
LeftThigh        localPosition (-0.4, 1.2, 0) localScale (0.35, 0.9, 0.35)
LeftShin         parent LeftThigh, localPosition (0, -0.85, 0), localScale (0.9, 0.9, 0.9)
RightThigh       localPosition (0.4, 1.2, 0)  localScale (0.35, 0.9, 0.35)
RightShin        parent RightThigh, localPosition (0, -0.85, 0), localScale (0.9, 0.9, 0.9)
```

각 visual Cube의 Collider는 제거하고 root CharacterController만 충돌에 사용한다. `PlayerRhythmController`, `PlayerCombat`, `PlayerInfection`을 root에 붙이고 Transform 및 Renderer 참조를 직렬화한다. FirePoint는 `(0, 2, 0.8)`이다.

바이러스 원본은 보라색 Sphere, 치료제 원본은 흰색 Sphere로 만들고 SphereCollider `isTrigger = true`, `useGravity = false`인 kinematic Rigidbody, Projectile을 붙인다. `PrefabUtility.SaveAsPrefabAsset`으로 각각 `Assets/HOPAK VIRUS/Prefabs/VirusProjectile.prefab`과 `CureProjectile.prefab`에 저장한 뒤 씬의 임시 원본은 즉시 제거한다. 두 prefab asset을 PlayerCombat과 적들에게 연결한다. 비활성 씬 오브젝트를 Instantiate하는 방식은 사용하지 않는다.

- [ ] **Step 4: 세 공간의 정확한 배치 생성**

모든 바닥 y는 `-0.25`, 두께는 `0.5`, 벽 높이는 `3`이다.

```text
Room 1 floor: center (0,-0.25,0), scale (20,0.5,16)
Room 1 walls: x=-10, x=10, z=-8; z=8에는 폭 4의 Gate 1
Room 1 enemies: Basic (-5,0,1), (0,0,5), (5,0,1)

Room 2 floor: center (0,-0.25,21), scale (12,0.5,26)
Room 2 outer walls: x=-6, x=6
Room 2 zigzag walls: (-2,1.5,15) scale (8,3,1), (2,1.5,22) scale (8,3,1), (-2,1.5,29) scale (8,3,1)
Room 2 enemies: Basic (-4,0,18), (4,0,27), Ranged (4,0,13), (-4,0,31)
Gate 2: center (0,1.5,34), scale (4,3,0.5)

Room 3 floor: Cylinder center (0,-0.25,48), scale (28,0.25,28), 반지름 14
Room 3 pillars: (-4,1.5,44), (4,1.5,44), (-4,1.5,52), (4,1.5,52), each scale (2,3,2)
Room 3 enemies: Basic (-7,0,48), (7,0,48), Elite (0,0,48)
Room 3 boundary: 16 Cube wall segments around radius 13, 중심에서 음의 z 방향인 입구 segment 한 개는 생략
```

각 방 root에 RoomController를 붙여 해당 적 배열과 다음 Gate를 연결한다. Room 3의 exit door는 null이다. Basic requiredHits `1`, Ranged `1`, Elite `3`으로 설정한다.

- [ ] **Step 5: 카메라, 조명과 최소 UI 생성**

Main Camera에 FollowCamera를 붙이고 player target과 offset `(0,7,-9)`를 연결한다. Directional Light 하나를 회전 `(50,-30,0)`으로 배치한다. Canvas는 Screen Space Overlay이며 외부 폰트 대신 `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`를 Text에 사용한다.

UI는 다음만 만든다.

```text
TitlePanel: HOPAK VIRUS, A/D, Left/Right, Space, Enter 안내
HudPanel: 왼쪽 위 보라색 Image 3개, 오른쪽 위 Room 1/3 Text
ResultPanel: 결과 Text, R: RESTART Text
```

리듬 게이지, 성공 횟수, 사거리, 조준선과 버튼은 만들지 않는다. GameManager에 모든 UI와 Runtime 컴포넌트를 연결한다.

- [ ] **Step 6: 씬 저장과 Build Settings 등록**

`BuildScene()` 마지막에 다음을 실행한다.

```csharp
string scenePath = "Assets/HOPAK VIRUS/Scenes/HOPAK VIRUS.unity";
EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
AssetDatabase.SaveAssets();
```

`h980220_BuildAutomation`은 다음 진입점을 제공한다.

```csharp
public static void RebuildScene() => h980220_GameSceneBuilder.BuildScene();

public static void BuildWindows()
{
    h980220_GameSceneBuilder.BuildScene();
    PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Standalone, ManagedStrippingLevel.High);
    BuildPipeline.BuildPlayer(new BuildPlayerOptions
    {
        scenes = new[] { "Assets/HOPAK VIRUS/Scenes/HOPAK VIRUS.unity" },
        locationPathName = "Builds/Windows/HOPAK VIRUS.exe",
        target = BuildTarget.StandaloneWindows64,
        options = BuildOptions.None
    });
}
```

- [ ] **Step 7: Unity batchmode로 씬 생성**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\Documents\Unity\HOPAK VIRUS' -executeMethod h980220_BuildAutomation.RebuildScene -logFile 'D:\Documents\Unity\HOPAK VIRUS\Logs\scene-build.log'
```

Expected: exit code 0, `Assets/HOPAK VIRUS/Scenes/HOPAK VIRUS.unity` 생성, 콘솔 컴파일 오류 0개.

- [ ] **Step 8: 생성 씬을 Unity Editor에서 열어 시각 검수**

검수 항목은 플레이어가 육면체 정확히 6개인지, Room 1이 비어 있는 광장인지, Room 2가 지그재그 통로인지, Room 3이 기둥 4개의 원형 경기장인지, 카메라가 벽에 가리지 않는지다. 문제를 Scene Builder 수치에서 수정하고 Step 7을 다시 실행한다.

- [ ] **Step 9: 커밋**

```powershell
git add 'Assets/HOPAK VIRUS/Editor' 'Assets/HOPAK VIRUS/Scenes' 'Assets/HOPAK VIRUS/Materials' 'Assets/HOPAK VIRUS/Prefabs' ProjectSettings/EditorBuildSettings.asset
git commit -m 'feat: build three-room primitive game scene'
```

---

### Task 8: PlayMode 완주 테스트, 재미 튜닝과 제출 빌드

**Files:**
- Create: `Assets/HOPAK VIRUS/Tests/PlayMode/h980220_HopakVirus.PlayModeTests.asmdef`
- Create: `Assets/HOPAK VIRUS/Tests/PlayMode/h980220_GameplaySmokeTests.cs`
- Modify: Inspector values in `Assets/HOPAK VIRUS/Scenes/HOPAK VIRUS.unity` through Scene Builder defaults

**Interfaces:**
- Consumes: 생성된 HOPAK VIRUS 씬과 모든 Runtime 시스템
- Produces: 자동 연동 검증, 5분 미만의 수동 완주 기록, 100MB 미만 Windows 제출 빌드

- [ ] **Step 1: PlayMode 테스트 asmdef 생성**

```json
{
  "name": "h980220_HopakVirus.PlayModeTests",
  "rootNamespace": "",
  "references": ["h980220_HopakVirus.Runtime"],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "versionDefines": [],
  "noEngineReferences": false,
  "optionalUnityReferences": ["TestAssemblies"]
}
```

- [ ] **Step 2: 씬 구조와 전체 상태 전환 PlayMode 테스트 작성**

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class h980220_GameplaySmokeTests
{
    [UnitySetUp]
    public IEnumerator LoadGame()
    {
        yield return SceneManager.LoadSceneAsync("HOPAK VIRUS");
        yield return null;
    }

    [UnityTest]
    public IEnumerator SceneHasRequiredPrimitivePlayerAndThreeRooms()
    {
        var player = GameObject.Find("Player");
        Assert.That(player, Is.Not.Null);
        Assert.That(player.GetComponentsInChildren<MeshRenderer>(true).Length, Is.EqualTo(6));
        Assert.That(Object.FindObjectsByType<h980220_RoomController>(FindObjectsSortMode.None).Length, Is.EqualTo(3));
        yield return null;
    }

    [UnityTest]
    public IEnumerator EveryEnemyCanBeInfectedAndFinalRoomCompletes()
    {
        var rooms = Object.FindObjectsByType<h980220_RoomController>(FindObjectsSortMode.None);
        foreach (var enemy in Object.FindObjectsByType<h980220_EnemyController>(FindObjectsSortMode.None))
            while (!enemy.IsInfected) enemy.ReceiveVirusHit();
        yield return null;
        foreach (var room in rooms) Assert.That(room.RemainingEnemies, Is.Zero);
    }
}
```

- [ ] **Step 3: PlayMode 테스트 실행**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\Documents\Unity\HOPAK VIRUS' -runTests -testPlatform PlayMode -testResults 'D:\Documents\Unity\HOPAK VIRUS\TestResults\playmode.xml' -logFile 'D:\Documents\Unity\HOPAK VIRUS\Logs\playmode-tests.log'
```

Expected: PlayMode 테스트 2개 PASS, 예외와 컴파일 오류 0개.

- [ ] **Step 4: 수동 재미 테스트 5회 수행**

각 실행에서 시작부터 승리까지 시간을 기록하고 다음을 평가한다.

```text
Run 1: 이동 학습 시간 / 완주 시간 / 박자 실패 원인 / 불쾌한 피격
Run 2: 이동 학습 시간 / 완주 시간 / 박자 실패 원인 / 불쾌한 피격
Run 3: 이동 학습 시간 / 완주 시간 / 박자 실패 원인 / 불쾌한 피격
Run 4: 이동 학습 시간 / 완주 시간 / 박자 실패 원인 / 불쾌한 피격
Run 5: 이동 학습 시간 / 완주 시간 / 박자 실패 원인 / 불쾌한 피격
```

합격 기준은 첫 전진 이해 30초 이내, 모든 완주 5분 이내, 중앙 완주 시간 3~4분이다. 기준을 벗어나면 먼저 `successWindow`, `baseMoveSpeed`, `maxMoveSpeed`, `successesToMaxSpeed` 순서로 한 변수씩 바꾸고 다섯 번을 다시 측정한다. 최고 이동 속도와 최고 속도 도달 성공 횟수는 Scene Builder 상수로 덮어쓰지 말고 PlayerRhythmController의 직렬화 필드로 유지한다.

- [ ] **Step 5: 승리·패배·재시작 수동 회귀 테스트**

치료제를 세 번 맞아 `CURED...`와 몸 색상 복귀를 확인하고 R 재시작한다. 이후 모든 적을 감염시켜 승리 문구, 멀어진 카메라, 춤추는 적을 확인하고 다시 R 재시작한다. 각 재시작에서 감염도 3칸, 닫힌 문, 미감염 적과 Title 화면이 복구되어야 한다.

- [ ] **Step 6: 전체 자동 테스트 재실행**

Run: Task 1 Step 4의 EditMode 명령과 Task 8 Step 3의 PlayMode 명령

Expected: 모든 테스트 PASS, 컴파일 오류 0개.

- [ ] **Step 7: Windows 빌드 생성**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'D:\Documents\Unity\HOPAK VIRUS' -executeMethod h980220_BuildAutomation.BuildWindows -logFile 'D:\Documents\Unity\HOPAK VIRUS\Logs\windows-build.log'
```

Expected: `Builds/Windows/HOPAK VIRUS.exe` 생성, `Build completed with a result of 'Succeeded'`.

- [ ] **Step 8: 빌드 크기와 금지 요소 검사**

```powershell
$bytes = (Get-ChildItem -Recurse -File 'Builds\Windows' | Measure-Object -Property Length -Sum).Sum
[math]::Round($bytes / 1MB, 2)
rg -n 'AudioSource|ParticleSystem|TrailRenderer|LineRenderer|UnityEngine.InputSystem' 'Assets/HOPAK VIRUS'
rg --files 'Assets' -g '*.cs' | Where-Object { [IO.Path]::GetFileName($_) -notlike 'h980220_*' }
```

Expected: 빌드 폴더 100MB 미만, 두 `rg` 검사에서 금지 컴포넌트·새 Input System·접두어 위반 0건.

- [ ] **Step 9: 최종 커밋**

```powershell
git add 'Assets/HOPAK VIRUS' ProjectSettings/EditorBuildSettings.asset Packages ProjectSettings
git commit -m 'feat: complete HOPAK VIRUS vertical slice'
```

---

## Final Verification Gate

완료를 주장하기 전에 다음을 실제 출력으로 확인한다.

1. EditMode와 PlayMode XML 결과의 failure 수가 0이다.
2. Unity 로그에 `error CS`, `NullReferenceException`, `MissingReferenceException`이 없다.
3. 빌드 실행 파일에서 Title → Playing → CURED → Restart와 Title → Playing → Win → Restart가 모두 동작한다.
4. 플레이어 visual MeshRenderer 수가 정확히 6이고 모두 Cube mesh다.
5. 모든 Asset C# 파일명과 클래스명이 `h980220_`로 시작한다.
6. `activeInputHandler: 0`이며 `com.unity.inputsystem` 의존성과 Input Actions 에셋이 없다.
7. 음원, AudioSource, ParticleSystem, 파동, 사거리 표시와 외부 아트가 없다.
8. 다섯 번의 초보자 기준 완주가 각각 5분 이내다.
9. 제출 빌드 또는 제출 압축 파일이 100MB 미만이다.
