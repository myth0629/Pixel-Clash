# 스테이지 전환 애니메이션 Unity 설정 가이드

## 🎮 문제 상황
전투 승리 후 스테이지가 넘어갈 때 기존과 똑같이 동작하여 걷기 애니메이션이 재생되지 않는 문제

## 🔧 Unity 설정 단계

### 1. **Animator Controller 생성**
```
1. Project 창에서 Assets/Animations/ 폴더 생성
2. 폴더에서 우클릭 → Create → Animator Controller
3. 이름: "CharacterWalkAnimator"
```

### 2. **Animator Controller 설정**
```
Parameters 탭에서:
1. + 버튼 클릭 → Bool 선택
2. 이름: "IsWalk" (정확히 이 이름으로!)

States 설정:
1. Idle State (기본): 정지 스프라이트
2. Walk State: 걷기 스프라이트 애니메이션
3. Transitions:
   - Idle → Walk: Condition "IsWalk" = true
   - Walk → Idle: Condition "IsWalk" = false
```

### 3. **애니메이션 클립 생성**
```
걷기 애니메이션:
1. 캐릭터 걷기 스프라이트들 (2-4장) 선택
2. 우클릭 → Create → Animation
3. 이름: "CharacterWalk"
4. Inspector에서 Loop Time 체크
5. Sample Rate: 8-12 정도 (자연스러운 속도)
```

### 4. **캐릭터 프리팹 설정**
```
캐릭터 GameObject에 컴포넌트 추가:
1. Animator 컴포넌트
   - Controller: 위에서 만든 "CharacterWalkAnimator" 할당

2. CharacterWalkAnimation 컴포넌트
   - Animator: 자동으로 할당됨
   - Walk State Name: "IsWalk" (Bool 파라미터명)
   - Walk Bob Speed: 2
   - Walk Bob Amount: 0.1
   - Walk Side Speed: 1.5
   - Walk Side Amount: 0.05
```

### 5. **디버그 확인 방법**
```
Console 창에서 다음 로그 확인:
1. "=== 스테이지 전환 시작 ===" (StageManager)
2. "OnStageTransitionStart 이벤트 발생" (StageManager)
3. "[캐릭터이름] 스테이지 X로 전환 - 캐릭터 걷기 애니메이션 시작" (CharacterWalkAnimation)
4. "[캐릭터이름] StartWalking 호출" (CharacterWalkAnimation)
5. "[캐릭터이름] 애니메이터 트리거 실행: IsWalk" (CharacterWalkAnimation)
```

## 🚨 체크포인트

### 애니메이션이 작동하지 않을 때:
1. **CharacterWalkAnimation 컴포넌트가 캐릭터에 있는가?**
2. **Animator Controller가 올바르게 할당되었는가?**
3. **Bool 파라미터 이름이 정확히 "IsWalk"인가?**
4. **Console에서 이벤트 구독 로그가 보이는가?**

### 애니메이션 클립 문제:
1. **걷기 스프라이트가 Loop로 설정되었는가?**
2. **Sample Rate가 너무 빠르거나 느리지 않은가?**
3. **Animator State에서 애니메이션 클립이 할당되었는가?**

### 이벤트 시스템 문제:
1. **BattleManager.OnEnemyDead()에서 StageManager.CompleteRound() 호출되는가?**
2. **StageManager.MoveToNextStage() 코루틴이 실행되는가?**
3. **OnStageTransitionStart 이벤트가 발생하는가?**

## 🎯 최종 결과
- **물리적 움직임**: CharacterWalkAnimation의 Transform 애니메이션 (상하 바운싱, 좌우 흔들림)
- **스프라이트 애니메이션**: Animator Controller의 Bool 파라미터로 제어되는 스프라이트 프레임 변경
- **이벤트 동기화**: StageManager → CharacterWalkAnimation 자동 연동

이 설정을 완료하면 전투 승리 후 스테이지 전환 시 자연스러운 걷기 애니메이션이 3초간 재생되고, 다음 스테이지가 시작됩니다! 🚶‍♂️✨
