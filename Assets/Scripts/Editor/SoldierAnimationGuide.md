# Soldier 캐릭터 위치별 애니메이션 설정 가이드

## 🎯 개요
Soldier 캐릭터가 전방과 후방 배치에서 서로 다른 애니메이션을 실행하도록 구현되었습니다.

## 🔧 Unity 에디터 설정

### 1. 애니메이터 파라미터 자동 추가
1. Unity 에디터에서 **Tools > Animation > Setup Soldier Position Animations** 메뉴 클릭
2. Soldier Animator Controller 파일 선택:
   ```
   Assets/Assets/Tiny RPG Character Asset Pack v1.03 -Full 20 Characters/Characters(100x100)/Player/Soldier/Soldier/Soldier_new.controller
   ```
3. **"애니메이터 파라미터 자동 추가"** 버튼 클릭

### 2. 수동으로 추가해야 할 요소들

#### Parameters (자동 추가됨)
- `IsInBackRow` (Bool) - 후방 배치 여부
- `FrontRowAttack` (Trigger) - 전방 공격 트리거  
- `BackRowAttack` (Trigger) - 후방 공격 트리거

#### States (수동 추가 필요)
- `idle` - 공용 대기 상태 (기존 애니메이션 사용)
- `FrontRowAttack` - 전방 공격 (근접 공격)
- `BackRowAttack` - 후방 공격 (원거리 공격)

#### Transitions (수동 설정 필요)
1. **공격 전환**:
   - `idle` → `FrontRowAttack`
   - Conditions: `FrontRowAttack` Trigger AND `IsInBackRow` = false
   
   - `idle` → `BackRowAttack`  
   - Conditions: `BackRowAttack` Trigger AND `IsInBackRow` = true

2. **공격 복귀**:
   - `FrontRowAttack` → `idle` (애니메이션 종료 시)
   - `BackRowAttack` → `idle` (애니메이션 종료 시)

## 💡 애니메이션 클립 추천

### 공용 Idle
- **idle**: 기존 대기 애니메이션 사용 (전방/후방 공통)

### 위치별 공격 애니메이션
#### 전방 (Front Row) - 근접 전투
- **FrontRowAttack**: 검으로 베기, 찌르기 등 근접 공격

#### 후방 (Back Row) - 원거리 지원  
- **BackRowAttack**: 창 던지기 또는 원거리 공격 자세

## 🎮 동작 원리

### 코드 레벨 동작
1. **위치 감지**: `PlayerCharacter.DetectAndSetPosition()`에서 X 좌표로 전방/후방 판단
2. **파라미터 설정**: `SetPosition()`에서 `IsInBackRow` 파라미터만 설정 (idle은 공용)
3. **공격 분기**: `TryAttack()`에서 위치에 따라 다른 트리거 실행

### 게임 플레이
1. **Soldier 레벨 3 달성** → 후방 배치 해금
2. **배치 위치 상관없이** → 동일한 `idle` 애니메이션 사용
3. **공격 시** → 위치에 따라 `FrontRowAttack` 또는 `BackRowAttack` 실행

## 🔍 디버그 정보

콘솔에서 다음 로그들을 확인할 수 있습니다:
```
[Soldier(Clone)] 위치 감지: X=-2.00, Position=Back
[Soldier(Clone)] Soldier 위치 애니메이션 설정: IsInBackRow=True
[Soldier(Clone)] Soldier 위치별 공격: BackRowAttack
```

## ⚠️ 주의사항

1. **애니메이션 클립 설정**: 공격 상태에만 별도 애니메이션 클립 할당 필요
2. **트랜지션 조건**: 공격 트랜지션에 `IsInBackRow` 조건 추가 필수
3. **Idle 공용**: 기존 idle 애니메이션을 그대로 사용하므로 별도 설정 불필요
4. **폴백 처리**: 위치별 공격 애니메이션이 없으면 기본 Attack 트리거 사용

## 🚀 확장 가능성

이 시스템을 다른 캐릭터에도 적용하려면:
1. `PlayerCharacter.SetPosition()`에서 캐릭터 이름 조건 추가
2. 해당 캐릭터의 Animator Controller에 위치별 파라미터/상태 추가
3. 필요시 `CharacterData`에 `supportsPositionAnimations` 플래그 추가
