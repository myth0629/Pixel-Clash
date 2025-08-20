# 공격 단계 애니메이션 시스템 (Attack Step System)

## 🎯 개요
모든 플레이어 캐릭터에 `attackStep` 시스템을 추가하여 Attack1과 Attack2 애니메이션을 번갈아가며 실행

## 🏗️ 시스템 구조

### 핵심 변수
```csharp
private int attackStep = 1; // 현재 공격 단계 (1: Attack1, 2: Attack2)
```

### 공격 로직 흐름
```csharp
TryAttack() → ExecuteAttackAnimation() → GetAttackTrigger() → 애니메이션 실행 → attackStep 전환
```

## 🎮 애니메이터 트리거 구조

### 일반 캐릭터 (기본)
- `Attack1` - 첫 번째 공격 애니메이션
- `Attack2` - 두 번째 공격 애니메이션

### Soldier 캐릭터 (위치별 + 단계별)
#### 전방 (Front Row)
- `FrontRowAttack1` - 전방 첫 번째 공격
- `FrontRowAttack2` - 전방 두 번째 공격

#### 후방 (Back Row)  
- `BackRowAttack1` - 후방 첫 번째 공격
- `BackRowAttack2` - 후방 두 번째 공격

## 🔄 동작 원리

### 1. 초기화
```csharp
// 전투 시작 시
StartCombat() → attackStep = 1

// 새 라운드 시작 시  
StartNewRound() → attackStep = 1
```

### 2. 공격 실행
```csharp
// 공격 시도
TryAttack() {
    ExecuteAttackAnimation();  // 현재 attackStep에 맞는 애니메이션 실행
    attackStep = attackStep == 1 ? 2 : 1;  // 1 ↔ 2 전환
}
```

### 3. 트리거 선택 로직
```csharp
GetAttackTrigger() {
    if (캐릭터 == "병사") {
        if (후방) return $"BackRowAttack{attackStep}";
        else return $"FrontRowAttack{attackStep}";
    }
    return $"Attack{attackStep}";
}
```

## 🛠️ Unity 애니메이터 설정 가이드

### 필요한 트리거 파라미터
모든 캐릭터에 다음 트리거들을 추가해야 합니다:

#### 일반 캐릭터
- `Attack1` (Trigger)
- `Attack2` (Trigger)

#### Soldier 캐릭터 (추가)
- `FrontRowAttack1` (Trigger)
- `FrontRowAttack2` (Trigger)  
- `BackRowAttack1` (Trigger)
- `BackRowAttack2` (Trigger)

### 권장 애니메이션 상태 구조
```
idle
├── Attack1 → idle
├── Attack2 → idle
└── (Soldier 전용)
    ├── FrontRowAttack1 → idle
    ├── FrontRowAttack2 → idle
    ├── BackRowAttack1 → idle
    └── BackRowAttack2 → idle
```

## 💡 애니메이션 제작 가이드

### Attack1 vs Attack2 차별화 아이디어
1. **공격 방향**: 왼쪽 베기 vs 오른쪽 베기
2. **공격 높이**: 상단 공격 vs 하단 공격  
3. **공격 강도**: 빠른 공격 vs 강한 공격
4. **무기 사용**: 주무기 vs 보조무기

### Soldier 캐릭터 특화
- **전방**: 검/방패 근접 공격 패턴
- **후방**: 창/활 원거리 공격 패턴

## 🔍 디버그 정보

### 로그 메시지 예시
```
[Knight(Clone)] 공격 애니메이션 실행: Attack1
[Knight(Clone)] 다음 공격 단계: Attack2
[Soldier(Clone)] 공격 애니메이션 실행: FrontRowAttack2  
[Soldier(Clone)] 다음 공격 단계: Attack1
```

### 폴백 시스템
1. 우선: 해당 단계별 트리거 (`Attack1`, `Attack2`)
2. 폴백: 기본 `Attack` 트리거
3. 에러: 모든 트리거 없음

## 🚨 주의사항

### 애니메이션 이벤트 설정
각 공격 애니메이션에 `DealDamage()` 호출 이벤트를 추가해야 합니다:
- Attack1 애니메이션 → DealDamage 이벤트
- Attack2 애니메이션 → DealDamage 이벤트

### Has Exit Time 설정
공격 애니메이션이 완전히 재생된 후 idle로 복귀하도록 설정

### 트랜지션 조건
각 트리거는 별도의 트랜지션으로 설정 (조건 중복 방지)

## 🎯 확장 계획

### 3단계 공격 시스템
향후 `attackStep`을 3단계로 확장 가능:
```csharp
attackStep = (attackStep % 3) + 1; // 1 → 2 → 3 → 1
```

### 콤보 시스템
연속 공격 시 데미지 보너스나 특수 효과 추가 가능

### 캐릭터별 특화
각 캐릭터마다 고유한 공격 패턴 수 설정 가능

---

**개발 완료 상태**: ✅ 시스템 구현 완료, 🔄 애니메이터 설정 필요
