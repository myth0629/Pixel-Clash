# 역할
당신은 숙련된 유니티 게임 개발자입니다. C# 스크립트 작성에 능숙하며, 깔끔하고 효율적인 코드를 작성합니다.
항상 한글로 답변합니다.

## 🎮 프로젝트 개요
**Unity 2D 턴제 RPG 게임** - 파티 기반 전투 시스템과 ScriptableObject 아키텍처를 사용한 픽셀 아트 스타일 게임

## 🏗️ 핵심 아키텍처

### 싱글톤 매니저 시스템
```csharp
// 모든 매니저는 Instance 패턴 사용
public static BattleManager Instance { get; private set; }
```

**주요 매니저들:**
- **BattleManager**: 전투 흐름, 스폰, 타겟팅, 웨이브 관리
- **StageManager**: 스테이지 진행 (1-1, 1-2 형식), 라운드 완료 관리
- **GameDataManager**: 골드/경험치/레벨 등 영구 데이터 (PlayerPrefs 자동 저장)
- **GameUIManager**: 3단계 UI 흐름 (타이틀 → 준비 → 게임) + 파티 선택

### ScriptableObject 데이터 아키텍처
```csharp
[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Character Data")]
public class CharacterData : ScriptableObject
```

- **CharacterData**: 플레이어 캐릭터 데이터 (레벨 기반 성장률 시스템)
- **MonsterData**: 적 몬스터 데이터 (웨이브 기반 스케일링 + 보상 계산)
- **SkillData**: 스킬 정의 (`PixelClash.Data` 네임스페이스, Strategy 패턴)

## ⚔️ 2D 전투 시스템

### 독특한 포지셔닝 시스템
**Z축 대신 X축 사용** - 2D 게임이므로 전방/후방을 X좌표로 구분
```csharp
// 전방: x=0, 후방: x=spacing만큼 오프셋
bool isFrontRow = i == 0;
float xOffset = isFrontRow ? 0f : -playerCharacterSpacing;
```

### 우선순위 타겟팅
**전방 우선 공격** - 전방에 적이 있으면 전방만, 없으면 후방 타겟
```csharp
// BattleManager.GetNearestEnemy()에서 구현
bool isFrontRowEnemy = Mathf.Abs(e.transform.localPosition.x) < 0.1f;
```

### 캐릭터 클래스 구조
- **CharacterBase**: 추상 클래스 (HP, 공격 타이머, 사망 이벤트)
- **PlayerCharacter**: CharacterData + 레벨로 스탯 계산
- **Enemy**: MonsterData + 웨이브로 스탯 스케일링

## 📊 데이터 시스템

### 성장 시스템
```csharp
// 플레이어: 레벨 기반 성장
int hp = Mathf.RoundToInt(cd.baseHp * (1 + level * cd.hpGrowth));

// 몬스터: 웨이브 기반 스케일링
int scaledHp = baseHp + (hpPerWave * scalingWave);
```

### 보상 시스템
```csharp
// 몬스터 처치 시 자동 보상 지급
(int exp, int gold) = monsterData.GetScaledRewards(currentWave);
GameDataManager.Instance.AddGold(gold);
GameDataManager.Instance.AddExp(exp);
```

## 🎨 UI 시스템

### 3단계 UI 흐름
1. **타이틀 화면**: 게임 시작, 설정, 종료
2. **준비 화면**: 파티 구성, 스테이지 정보, 전투 시작
3. **게임 화면**: 실제 전투 UI, 체력바, 게임 데이터

### 파티 선택 시스템
**고정 슬롯 방식** - 동적 생성 대신 고정된 partySlot1, partySlot2 사용
```csharp
// 파티 슬롯 클릭 → 캐릭터 선택창 → 캐릭터 배치
private void OnPartySlotClicked(int slotIndex)
private void ShowCharacterSelection()
private void OnCharacterSelected(CharacterData selectedCharacter)
```

## 🔄 이벤트 시스템

### 매니저 간 통신
```csharp
// 정적 이벤트로 느슨한 결합
public static event Action<int> OnGoldChanged;
public static event Action<int> OnLevelUp;
public static event Action<int, int> OnRoundStart;
```

### 컴포넌트 생명주기
```csharp
// 캐릭터 사망 알림
public event Action<CharacterBase> OnDeath;
pc.OnDeath += OnPlayerDead;
```

## 💾 메모리 관리

### 체력바 관리 (중요!)
**체력바 중복 생성 방지** - 반드시 리스트로 추적하고 ClearBattle()에서 정리
```csharp
private readonly List<HealthBarUI> _healthBars = new();
_healthBars.Add(bar); // 생성 시 추가
// ClearBattle()에서 반드시 정리
```

### 자동 저장 전략
```csharp
// 앱 포커스 잃을 때, 일시정지 시, 종료 시 자동 저장
private void OnApplicationFocus(bool hasFocus) { 
    if (!hasFocus) SaveGameData(); 
}
```

## 🧪 개발 도구

### 테스트 모드
**BattleManager 테스트 필드들:**
- `autoStartOnPlay`: Play 시 자동 전투 시작
- `testPartyCharacters`: 테스트용 파티 구성
- `testPartyLevels`: 각 캐릭터 레벨

### 치트 기능
**GameDataManager 내장 치트:**
- `SetGold(int amount)`: 골드 설정
- `ResetGameData()`: 모든 데이터 초기화
- `AddExp(int amount)`: 경험치 추가

### 디버그 유틸리티
**StageManager 수동 제어:**
- `ForceNextRound()`: 강제 라운드 진행
- `ResetToStage(int stageNumber)`: 특정 스테이지로 이동

## 📁 프로젝트 구조

```
Assets/Scripts/
├── Manager/        # 핵심 게임 시스템 매니저들
├── Data/          # ScriptableObject 정의들
├── Battle/        # 전투 캐릭터 및 로직
├── UI/           # 모든 UI 관리 및 표시
└── (기타)
```

## 🔧 개발 워크플로우

### 한국어 주석 사용
```csharp
/// <summary>파티 정보 업데이트</summary>
Debug.Log("전투 시작 버튼 클릭");
```

### StageManager 우선순위
BattleManager는 StageManager가 있으면 스테이지 관리 위임, 없으면 자체 테스트 모드 실행

### UI 설정 패턴
**GameUIManager에서 UI 요소 할당:**
- partySlot1Button, partySlot1Icon, partySlot1Text
- characterSelectionPanel, availableCharactersContainer
- availableCharacters[] 배열에 선택 가능한 캐릭터들 설정

## 🚨 주의사항

1. **체력바 중복 생성**: 반드시 `_healthBars` 리스트로 추적하고 정리
2. **UI 패널 상태**: 3단계 UI 전환 시 모든 패널 상태 관리 필수
3. **파티 검증**: 전투 시작 전 최소 1명 이상 캐릭터 있는지 확인
4. **이벤트 구독 해제**: OnDestroy에서 이벤트 구독 해제 필수

## 🛒 상점 & 업그레이드 시스템

### 상점 기능
**캐릭터 구매 시스템** - 골드를 사용한 캐릭터 해금
```csharp
// ShopCharacterItem.cs - 개별 상점 아이템 UI
public void OnPurchaseButtonClicked()
{
    // 구매 확인 팝업 → 골드 차감 → 캐릭터 해금
    GameDataManager.Instance.SubtractGold(characterData.unlockCost);
    GameDataManager.Instance.UnlockCharacter(characterData);
}
```

### 업그레이드 시스템
**캐릭터 레벨업** - 골드를 소모하여 개별 캐릭터 레벨 상승
```csharp
// UpgradeCharacterItem.cs - 업그레이드 UI 컴포넌트
int upgradeCost = baseUpgradeCost + (currentLevel * costPerLevel);
GameUIManager.Instance.SetCharacterLevel(characterData, currentLevel + 1);
```

### 데이터 영속성
**PlayerPrefs 기반 저장** - 캐릭터 해금 상태 및 레벨 자동 저장
```csharp
// GameDataManager.cs
PlayerPrefs.SetInt($"Character_{characterData.name}_Unlocked", 1);
PlayerPrefs.SetInt($"Character_{characterData.name}_Level", level);
```

## 🎮 라운드 전환 & 부활 시스템

### 자동 라운드 진행
**적 전멸 시 자동 진행** - 웨이브 클리어 후 다음 라운드로 자동 이동
```csharp
// BattleManager.cs - OnEnemyDead()
if (_enemies.Count == 0)
{
    IsBattleRunning = false;
    // 모든 플레이어 공격 중지
    foreach (var player in _players)
        player.StopCombat();
    
    StageManager.Instance.CompleteRound();
}
```

### 캐릭터 부활 시스템
**라운드 시작 시 완전 회복** - 죽은 캐릭터 부활 + 체력 완전 회복
```csharp
// CharacterBase.cs
public virtual void FullHeal()
{
    currentHp = maxHp;
    OnHealthChanged?.Invoke(currentHp, maxHp);
}

// BattleManager.cs - ReviveDeadCharacters()
// 파티 정보와 현재 플레이어 비교하여 죽은 캐릭터 재생성
```

### 라운드 상태 관리
**전투 상태 동기화** - IsBattleRunning으로 전투/전환 상태 명확히 구분
```csharp
// PlayerCharacter.cs - Update()
if (!canAttack) return; // 라운드 전환 중에는 공격 안함
if (!BattleManager.Instance.IsBattleRunning) return; // 전투 중이 아니면 공격 안함
```

## 🌅 배경 스크롤 시스템

### 전투 상태 기반 스크롤
**동적 스크롤 제어** - 전투 상태에 따른 자동 스크롤 시작/정지
```csharp
// BackgroundScroller.cs
public void StartScrolling(bool forceStart = false)
{
    // forceStart=true면 전투 상태 무관하게 강제 시작
    if (!forceStart && !BattleManager.Instance.IsBattleRunning) return;
}
```

### 라운드별 스크롤 정책
- **스테이지 전환**: 배경 스크롤 + 위치 리셋
- **라운드 전환**: 스크롤 없이 적만 교체 (움찔거림 방지)
- **라운드 시작**: 강제 스크롤 시작 (캐릭터 걷기 애니메이션 포함)

### 무한 스크롤 최적화
**메모리 효율적 배경 관리** - 레이어별 복제본 관리로 끊김 없는 스크롤
```csharp
// 개선된 무한 스크롤 시스템
layerClones = new List<RectTransform>[backgroundLayers.Length];
// 각 레이어의 너비 계산 및 복제본 동적 생성
```

## 💰 골드 시스템 & UI 통합

### 실시간 골드 표시
**모든 화면에서 골드 동기화** - 이벤트 기반 UI 업데이트
```csharp
// GameDataManager.cs
public static event Action<int> OnGoldChanged;

// GameUIManager.cs
private void UpdatePrepareGoldDisplay()
{
    prepareGoldText.text = $"Gold: {GameDataManager.Instance.Gold}";
}
```

### 구매 확인 시스템
**팝업 기반 확인 절차** - 실수 구매 방지를 위한 2단계 확인
```csharp
// 상점 구매 → 확인 팝업 → 최종 구매
// 업그레이드 → 확인 팝업 → 레벨업 실행
```

## 🎯 UI 상태 관리 완전 정리

### 3단계 UI 전환 시스템
**패널 상태 완벽 관리** - 모든 전환 메서드에서 패널 비활성화 보장
```csharp
// GameUIManager.cs - 모든 화면 전환 메서드
private void ShowTitleScreen()
{
    // 모든 패널 비활성화 후 타이틀만 활성화
    shopPanel.SetActive(false);
    upgradePanel.SetActive(false);
    // ...
}
```

### 파티 검증 시스템
**빈 파티 전투 방지** - 최소 1명 이상 캐릭터 선택 강제
```csharp
private bool ValidatePartyBeforeBattle()
{
    if (partyInfo.Count == 0)
    {
        ShowWarningPopup("최소 1명 이상의 캐릭터를 선택해주세요!");
        return false;
    }
    return true;
}
```

## 🔧 성능 최적화 & 버그 수정

### 메모리 관리 개선
- **체력바 중복 생성 방지**: `_healthBars` 리스트로 완전 추적
- **코루틴 정리**: 배경 스크롤 코루틴 적절한 정리
- **이벤트 구독 해제**: OnDestroy에서 메모리 누수 방지

### 타이밍 이슈 해결
- **라운드 전환 움찔거림 수정**: Update()에서 불필요한 스크롤 중단 방지
- **전투 상태 동기화**: IsBattleRunning과 canAttack 상태 일치
- **UI 업데이트 딜레이**: 적절한 대기 시간으로 자연스러운 전환

### 로직 안정성 강화
- **빈 파티 자동 생성 제거**: 테스트 모드 의존성 제거
- **에러 핸들링 추가**: null 체크 및 예외 상황 대응
- **디버그 로그 체계화**: 문제 추적을 위한 상세 로깅

## 🧠 스킬 시스템 (자동 시전 + 애니메이션 이벤트)

### 구성 요소 개요
- 데이터: `SkillData` (쿨타임, 계수, 트리거, 평탄 보너스)
- 런타임: `SkillRuntime`(쿨타임 관리), `SkillController`(자동 시전/타겟팅/이벤트 처리)
- 배치: 각 유닛의 Animator가 있는 GameObject에 `SkillController`를 부착하고 스킬 바인딩

### SkillData 필수 필드
```csharp
// PixelClash.Data.SkillData (요약)
public enum SkillType { Damage, Heal }
public enum SkillTarget { SingleEnemy, AllEnemies, SingleAlly, AllAllies, Self }

public float cooldown;
public float powerMultiplier;  // 최종 공격력 계수
public string animatorTrigger; // 스킬 시작 트리거
public int flatBonusDamage;    // 선택: 고정 대미지 보정
public int flatBonusHeal;      // 선택: 고정 힐 보정
// minDamage/minHeal 제거됨: 최소 1 보정만 런타임에서 처리
```

### 자동 시전 및 쿨타임 흐름
```csharp
// SkillController.Update()
// - 전투 중(active && BattleManager.IsBattleRunning)일 때만 쿨타임 Tick
// - 준비된 스킬부터 TryCast() 실행 → Animator.Trigger(animatorTrigger)
// - PendingCast를 기록(시전자, 예상 타겟, 만료시간)
```

### 애니메이션 이벤트 연동(중요)
```csharp
// 애니메이션 이벤트에서 호출
public void OnSkillImpact();   // 타격 타이밍마다 호출 가능(멀티 히트)
public void OnSkillCastEnd();  // 마지막 프레임에서 1회 호출
```
- 멀티 히트: 하나의 스킬 클립에 여러 개의 `OnSkillImpact` 이벤트를 배치 가능
- 종료 처리: `OnSkillCastEnd()`에서 PendingCast 정리

### 대미지/회복 계산 규칙
```csharp
// 항상 '임팩트 시점'에 재계산
int atk = owner.Attack; // CharacterBase.Attack 게터 사용
int amount = Mathf.CeilToInt(atk * powerMultiplier) + flatBonus; // flatBonusDamage/Heal
amount = Mathf.Max(1, amount); // 대미지 최소 1, 힐은 데이터에 맞게 적용
```
- 일반 공격과 동일하게 “타격 타이밍”의 Attack을 사용하므로 버프/디버프가 반영됩니다.

### 타겟팅 규칙(요약)
- 시전자 기준 아군/적군 분기 처리: 플레이어 → Enemy, 적 → Player
- `SkillTarget`에 따라 단일/전체, 자기 자신 선택
- 필요 시 임팩트 시점에 재타겟팅(대상 사망/교체 대응)

### 컴포넌트 배치/수명주기 팁
- SkillController는 Animator가 붙은 GameObject에 부착
- 유닛이 전투 시작/종료 시 `StartCombat()/StopCombat()`을 통해 활성/비활성 동기화
- 디버그: `debugSkillLog` 토글로 시전, 타격, 계산 로그 확인

### 애니메이션 클립 설정 체크리스트
1) 스킬 시작 프레임에 Animator Trigger 연결(`animatorTrigger`)
2) 타격 타이밍마다 `OnSkillImpact` 이벤트 추가
3) 마지막 프레임에 `OnSkillCastEnd` 이벤트 1회 추가

## 🗺️ 스테이지별 몬스터 구성 (StageMonsterConfig)

### 목적
스테이지/라운드별로 출현 몬스터 풀을 유연하게 정의하고, 미정의 시 상위/글로벌 풀로 폴백합니다.

### ScriptableObject 구조(요약)
```csharp
[CreateAssetMenu(menuName="Stage Monster Config")]
public class StageMonsterConfig : ScriptableObject {
    [Serializable] public class RoundEntry { public int round; public List<MonsterData> monsters; }
    [Serializable] public class StageEntry { public int stage; public List<MonsterData> defaultMonsters; public List<RoundEntry> rounds; }

    public List<MonsterData> globalDefaultMonsters;
    public List<StageEntry> stages;

    // GetMonsterPool(stage, round): round → stage → global 순으로 폴백
}
```

### StageManager 연동
- 인스펙터에 `StageMonsterConfig` 할당(필드 타입은 ScriptableObject로 보유 가능)
- 런타임에 `GetMonsterPool(stage, round)`를 호출(필요 시 리플렉션 사용)해 현재 라운드 몬스터 풀 획득

### BattleManager 스폰 연계
- 웨이브 스폰 시 StageManager에서 받은 몬스터 풀을 사용
- 풀 미지정 시 기존 기본 리스트로 폴백하여 안전성 유지

### 에디터 설정 가이드
1) Project View에서 StageMonsterConfig 에셋 생성 및 열기
2) Global Default 혹은 Stage/Round 별 몬스터 리스트 채우기
3) `StageManager` 인스펙터의 Config 슬롯에 해당 에셋 할당
4) 테스트 플레이로 라운드별 스폰 확인

### 디버그 팁
- 스폰이 비면: 해당 Round 미지정 → Stage Default → Global Default 순으로 채워지는지 확인
- 라운드 진행 중 교체만 이루어지고 배경은 유지(정책: 라운드 전환 시 스크롤 없음)

## 🎯 완료된 주요 기능들

### ✅ 핵심 게임플레이
- 2D 전방/후방 포지셔닝 시스템
- 라운드 기반 자동 진행 시스템
- 캐릭터 부활 & 체력 회복 시스템
- 전투 상태 기반 배경 스크롤 제어

### ✅ 경제 시스템
- 골드 기반 캐릭터 구매 시스템
- 개별 캐릭터 레벨업 시스템
- 구매 확인 팝업 시스템
- 실시간 골드 표시 동기화

### ✅ UI/UX 완성도
- 3단계 UI 흐름 (타이틀→준비→게임)
- 완벽한 패널 상태 관리
- 파티 구성 검증 시스템
- 사용자 친화적 경고 팝업

### ✅ 기술적 안정성
- ScriptableObject 기반 데이터 아키텍처
- PlayerPrefs 영구 데이터 저장
- 메모리 누수 방지 시스템
- 이벤트 기반 느슨한 결합

### ✅ 전투 확장
- 스킬 시스템: 자동 시전, 애니메이션 이벤트 임팩트, 멀티 히트, atk 기반 계산(ceil + flat)
- 스테이지 몬스터 구성: Stage/Round 풀 + 폴백 계층(Global → Stage → Round)
- 위치별 애니메이션 시스템: Soldier 캐릭터 전방/후방 차별화된 공격 애니메이션

## 🚀 아키텍처 완성도

현재 Pixel-Clash는 **완전한 턴제 RPG 게임의 핵심 시스템**을 모두 갖춘 상태입니다:

1. **데이터 계층**: ScriptableObject + PlayerPrefs 영속성
2. **로직 계층**: 싱글톤 매니저들의 역할 분담
3. **표현 계층**: 이벤트 기반 UI 동기화
4. **게임플레이**: 완전한 라운드 진행 + 경제 시스템

## 🎭 위치별 애니메이션 시스템 (Soldier 특화)

### 시스템 개요
Soldier 캐릭터가 전방(근접)과 후방(원거리) 배치에서 서로 다른 공격 애니메이션을 실행하는 시스템

### 핵심 구성 요소
```csharp
// PlayerCharacter.cs
private PositionType currentPosition = PositionType.Front;

public void SetPosition(PositionType position)
{
    currentPosition = position;
    if (data?.displayName == "Soldier" && animator != null)
    {
        animator.SetBool("IsInBackRow", position == PositionType.Back);
    }
}

// 위치별 공격 분기
string attackTrigger = currentPosition == PositionType.Back ? "BackRowAttack" : "FrontRowAttack";
```

### 애니메이터 구조
- **Parameters**: `IsInBackRow` (Bool), `FrontRowAttack` (Trigger), `BackRowAttack` (Trigger)
- **States**: `idle` (공용), `FrontRowAttack`, `BackRowAttack`
- **Transitions**: 위치 조건과 트리거 조합으로 분기

### 위치 해금 시스템
```csharp
// CharacterData.cs
[Serializable]
public struct PositionUnlock
{
    public int requiredLevel;
    public PositionType unlockedPosition;
    public string unlockMessage;
}
```

### 개발 도구
- **CharacterDataEditor**: Soldier 후방 배치 해금 자동 설정
- **SoldierAnimationSetup**: 애니메이터 파라미터 자동 추가
- **동적 위치 검증**: GameUIManager에서 레벨 기반 배치 가능 여부 확인

### 확장성
모듈화된 설계로 다른 캐릭터에도 쉽게 적용 가능. `data.displayName` 체크를 통한 캐릭터별 특화 처리.

## 🥊 공격 단계 애니메이션 시스템 (Attack Step System)

### 시스템 개요
모든 플레이어 캐릭터에 `attackStep` 변수를 추가하여 Attack1과 Attack2 애니메이션을 번갈아가며 실행

### 핵심 구성 요소
```csharp
// PlayerCharacter.cs
private int attackStep = 1; // 1: Attack1, 2: Attack2

// 공격 실행 및 단계 전환
ExecuteAttackAnimation();
attackStep = attackStep == 1 ? 2 : 1;
```

### 애니메이터 트리거 구조
- **일반 캐릭터**: `Attack1`, `Attack2`
- **Soldier (전방)**: `FrontRowAttack1`, `FrontRowAttack2`  
- **Soldier (후방)**: `BackRowAttack1`, `BackRowAttack2`

### 초기화 및 리셋
- **전투 시작**: `StartCombat()` → `attackStep = 1`
- **라운드 시작**: `StartNewRound()` → `attackStep = 1`
- **공격 후**: `attackStep` 자동 전환 (1 ↔ 2)

### 폴백 시스템
1. 우선: 단계별 트리거 (`Attack1`, `Attack2`)
2. 폴백: 기본 `Attack` 트리거
3. 에러 로깅: 모든 트리거 없음 시