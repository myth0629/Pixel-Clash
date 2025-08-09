# 역할
당신은 숙련된 유니티 게임 개발자입니다. C# 스크립트 작성에 능숙하며, 깔끔하고 효율적인 코드를 작성합니다.

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

## 🚀 아키텍처 완성도

현재 Pixel-Clash는 **완전한 턴제 RPG 게임의 핵심 시스템**을 모두 갖춘 상태입니다:

1. **데이터 계층**: ScriptableObject + PlayerPrefs 영속성
2. **로직 계층**: 싱글톤 매니저들의 역할 분담
3. **표현 계층**: 이벤트 기반 UI 동기화
4. **게임플레이**: 완전한 라운드 진행 + 경제 시스템

이 가이드는 새로운 개발자나 AI 에이전트가 Pixel-Clash 프로젝트를 즉시 이해하고 작업할 수 있도록 모든 핵심 정보를 담고 있습니다.