using UnityEditor.U2D.Animation;
using PixelClash.Data;
using UnityEngine;
using System.Collections;

/// 파티 슬롯에 들어가는 플레이어 캐릭터.
/// CharacterData + 레벨을 받아서 내부 스탯 계산.
public class PlayerCharacter : CharacterBase
{
    [HideInInspector] public CharacterData data;
    [HideInInspector] public int level = 1;
    
    private Enemy enemy;  // 현재 타겟 적
    private bool canAttack = false;  // 공격 가능 여부
    private PositionType currentPosition = PositionType.Front; // 현재 배치 위치

    protected override void Start()
    {
        base.Start(); // CharacterBase의 InitializeAnimator 호출
        
        // PlayerCharacter 특화 초기화가 필요하면 여기서 추가
    }

    /// <summary>외부(BattleManager)에서 호출: 캐릭터 SO & 레벨 세팅</summary>
    public void Setup(CharacterData cd, int lv)
    {
        data  = cd;
        level = lv;

        // 곱연산 성장: base * (1 + growth)^(level - 1)
        int effectiveLevel = Mathf.Max(1, level);
        float hpMultiplier = Mathf.Pow(1f + cd.hpGrowth, effectiveLevel - 1);
        float atkMultiplier = Mathf.Pow(1f + cd.atkGrowth, effectiveLevel - 1);
        int hp  = Mathf.RoundToInt(cd.baseHp  * hpMultiplier);
        int atk = Mathf.RoundToInt(cd.baseAtk * atkMultiplier);

        // SPD 개념을 hpGrowth처럼 두고 interval 산출 가능
        float interval = attackInterval; // ex) 1초 기본값

        Debug.Log($"[PlayerCharacter] Setup: {cd.displayName} Lv.{lv}, hp={hp}, atk={atk}");
        InitStats(hp, atk, interval);
        Debug.Log($"[PlayerCharacter] After InitStats: Attack={Attack}");

        // 위치 감지 및 애니메이션 상태 설정
        DetectAndSetPosition();

        // 스킬 바인딩: 캐릭터 레벨 기준 해금 (Animator가 있는 GameObject에 부착하여 이벤트 일치)
        var targetGo = animator != null ? animator.gameObject : this.gameObject;
        var skillCtrl = targetGo.GetComponent<SkillController>();
        if (skillCtrl == null) skillCtrl = targetGo.AddComponent<SkillController>();
        
        // SkillController의 _owner를 수동으로 설정 (타이밍 문제 해결)
        skillCtrl.SetOwner(this);
        
        if (cd.skills != null && cd.skills.Count > 0)
        {
            var unlocked = new System.Collections.Generic.List<PixelClash.Data.SkillData>();
            foreach (var su in cd.skills)
            {
                if (su.requiredLevel <= level && su.skill != null)
                    unlocked.Add(su.skill);
            }
            skillCtrl.BindSkills(unlocked);
            Debug.Log($"[PlayerCharacter] 스킬 바인딩 완료: {unlocked.Count}개 스킬");
        }
    }

    /// <summary>현재 위치를 감지하고 애니메이션 상태를 설정</summary>
    private void DetectAndSetPosition()
    {
        // X 좌표를 기준으로 전방/후방 판단 (0에 가까우면 전방, 음수면 후방)
        float xPosition = transform.localPosition.x;
        PositionType detectedPosition = Mathf.Abs(xPosition) < 0.1f ? PositionType.Front : PositionType.Back;
        
        SetPosition(detectedPosition);
        
        Debug.Log($"[{gameObject.name}] 위치 감지: X={xPosition:F2}, Position={currentPosition}");
    }

    /// <summary>캐릭터 위치를 설정하고 해당 애니메이션 상태를 적용</summary>
    public void SetPosition(PositionType position)
    {
        currentPosition = position;

        // Soldier 캐릭터만 위치별 애니메이션 적용 (공격만, idle은 공용)
        if (data != null && data.displayName == "병사")
        {
            // 애니메이터 파라미터 설정 (공격 분기용)
            if (HasAnimatorParameter("IsInBackRow"))
            {
                bool isBackRow = (position == PositionType.Back);
                animator.SetBool("IsInBackRow", isBackRow);
                Debug.Log($"[{gameObject.name}] Soldier 위치 애니메이션 설정: IsInBackRow={isBackRow}");
            }
        }
    }

    /// <summary>애니메이터에 특정 상태가 있는지 확인</summary>
    private bool HasAnimatorState(string stateName)
    {
        if (animator == null) return false;
        
        for (int i = 0; i < animator.layerCount; i++)
        {
            if (animator.HasState(i, Animator.StringToHash(stateName)))
                return true;
        }
        return false;
    }
    
    
    protected override void Update()
    {
        // 공격 가능 상태가 아니면 공격하지 않음 (라운드 전환 중이거나 전투 시작 전)
        if (!canAttack)
            return;
        
        // BattleManager 상태 확인 (전투 중이 아니면 공격하지 않음)
        if (!BattleManager.Instance.IsBattleRunning)
        {
            // canAttack이 true인데 IsBattleRunning이 false면 비정상 상태
            Debug.LogWarning($"[{gameObject.name}] 전투 가능 상태이지만 BattleManager.IsBattleRunning이 false입니다!");
            return;
        }
            
        base.Update();
    }

    /// <summary>자동 공격 로직: 가장 가까운 적 타깃팅</summary>
    protected override void TryAttack()
    {
        enemy = BattleManager.Instance.GetNearestEnemy(transform.position);
        if (enemy == null) 
        {
            Debug.LogWarning($"[{gameObject.name}] 타겟할 적이 없습니다!");
            return;
        }
        
        Debug.Log($"[{gameObject.name}] 적 {enemy.name} 공격!");
        
        // 베이스 클래스의 공격 애니메이션 실행
        ExecuteAttackAnimation();
    }

    /// <summary>현재 상황에 맞는 공격 트리거 이름 반환</summary>
    protected override string GetAttackTrigger()
    {
        // Soldier 캐릭터의 위치별 공격 (기존 시스템 유지)
        if (data != null && data.displayName == "병사")
        {
            if (currentPosition == PositionType.Back)
            {
                return $"BackRowAttack"; // BackRowAttack1, BackRowAttack2
            }
            else
            {
                return $"FrontRowAttack{attackStep}"; // FrontRowAttack1, FrontRowAttack2
            }
        }
        
        // 다른 캐릭터들의 기본 공격 단계 시스템
        return $"Attack{attackStep}"; // Attack1, Attack2
    }
    
    
    // 타격 프레임(애니메이션 이벤트)에서 호출
    public void DealDamage()
    {
        if (enemy != null && enemy.isActiveAndEnabled)
            enemy.TakeDamage(atk);

        enemy = null;  // 클린업
    }
    
    /// <summary>BattleManager에서 호출 - 전투 시작</summary>
    public void StartCombat()
    {
        canAttack = true;
        ResetAttackStep(); // 베이스 클래스 메서드 사용
        
        // 모든 하위 SkillController 활성화 (Animator에 부착된 컴포넌트 포함)
        var scList = GetComponentsInChildren<SkillController>(true);
        foreach (var sc in scList) sc.StartCombat();
        Debug.Log($"[{gameObject.name}] 전투 시작! Attack={Attack}, AttackStep={attackStep}, SkillControllers={scList.Length}개");
    }
    
    /// <summary>새로운 라운드 시작 시 호출 - 전투 준비</summary>
    public void StartNewRound()
    {
        canAttack = false;
        ResetAttackStep(); // 베이스 클래스 메서드 사용
        
        // 체력 완전 회복 (사망 상태에서도 부활)
        FullHeal();
        
        // Death 애니메이션 상태 리셋 - 강제로 Idle 상태로 전환
        if (animator != null)
        {
            // 또는 직접 Idle 상태로 전환
            if (HasAnimatorState("idle") || HasAnimatorState("Idle"))
            {
                string idleStateName = HasAnimatorState("idle") ? "idle" : "Idle";
                animator.Play(idleStateName, 0, 0f);
                Debug.Log($"[{gameObject.name}] {idleStateName} 상태로 강제 전환");
            }
        }
        
        Debug.Log($"[{gameObject.name}] 새로운 라운드 준비 - 체력 회복 완료, AttackStep 초기화, 애니메이션 리셋");
    }
    
    /// <summary>전투 중지 (라운드 완료 시 호출)</summary>
    public void StopCombat()
    {
        canAttack = false;
    var scList = GetComponentsInChildren<SkillController>(true);
    foreach (var sc in scList) sc.StopCombat();
        Debug.Log($"[{gameObject.name}] 전투 중지 - 라운드 완료");
    }
    
    /// <summary>플레이어 사망 처리 - Destroy하지 않고 Death 애니메이션 상태 유지</summary>
    protected override void HandleDeath()
    {
        // 플레이어는 사망 시 Destroy하지 않고 Death 애니메이션 상태를 유지
        canAttack = false; // 공격 중지
        
        // 스킬 컨트롤러들도 중지
        var scList = GetComponentsInChildren<SkillController>(true);
        foreach (var sc in scList) sc.StopCombat();
        
        Debug.Log($"[{gameObject.name}] 플레이어 사망 - Death 애니메이션 상태 유지, 오브젝트 보존");
    }
}