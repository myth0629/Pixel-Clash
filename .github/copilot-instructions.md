# Pixel-Clash 프로젝트 완전 가이드

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

## 🎯 최근 구현 완료 사항

- ✅ 2D 전방/후방 포지셔닝 시스템
- ✅ ScriptableObject 기반 몬스터 관리
- ✅ 골드/경험치 영구 저장 시스템
- ✅ 스테이지 진행 및 라운드 UI
- ✅ 타이틀 → 준비 → 전투 UI 흐름
- ✅ 파티 선택/변경 시스템
- ✅ 체력바 중복 생성 문제 해결

이 가이드는 새로운 개발자나 AI 에이전트가 Pixel-Clash 프로젝트를 즉시 이해하고 작업할 수 있도록 모든 핵심 정보를 담고 있습니다.