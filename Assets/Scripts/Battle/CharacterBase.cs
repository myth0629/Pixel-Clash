using System;
using UnityEngine;

/// 전투 유닛(플레이어·적) 공통 기능:
///  HP 관리, 자동 공격 타이머, 사망 이벤트
public abstract class CharacterBase : MonoBehaviour
{
    // ---------- 필드 ----------
    protected int maxHp;
    protected int currentHp;
    protected int atk;

    [Tooltip("공격 간격(초). SPD에 따라 파생 클래스에서 계산")]
    [SerializeField] protected float attackInterval = 1f;
    private float _attackTimer;

    // 사망 콜백 (BattleManager에 알림)
    public event Action<CharacterBase> OnDeath;
    
    /// HP 변경 시 브로드캐스트 (현재 HP, 최대 HP)
    public event Action<int, int> OnHealthChanged;

    public int CurrentHp => currentHp;
    public int MaxHp     => maxHp;
    public int Attack    => atk; // 스킬/UI에서 현재 공격력 참조 용도
    
    // 애니메이션 관리
    protected Animator animator;
    protected int attackStep = 1; // 공격 단계 (1: Attack1, 2: Attack2)

    // ---------- 유니티 라이프사이클 ----------
    protected virtual void Start()
    {
        InitializeAnimator();
    }
    
    protected virtual void Update()
    {
        if (!BattleManager.Instance.IsBattleRunning) return;

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= attackInterval)
        {
            _attackTimer = 0f;
            TryAttack();
        }
    }

    // ---------- 애니메이션 관리 ----------
    /// <summary>애니메이터 초기화 및 파라미터 확인</summary>
    protected virtual void InitializeAnimator()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        if (animator == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Animator를 찾을 수 없습니다!");
        }
        else
        {
            Debug.Log($"[{gameObject.name}] Animator 찾음: {animator.gameObject.name}");
            LogAnimatorParameters();
        }
    }
    
    /// <summary>애니메이터 파라미터 목록 출력 (디버그용)</summary>
    protected void LogAnimatorParameters()
    {
        if (animator == null) return;
        
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            Debug.Log($"[{gameObject.name}] 애니메이터 파라미터: {param.name} ({param.type})");
        }
    }
    
    /// <summary>애니메이터에 특정 파라미터가 있는지 확인</summary>
    protected bool HasAnimatorParameter(string parameterName)
    {
        if (animator == null) return false;
        
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == parameterName)
                return true;
        }
        return false;
    }
    
    /// <summary>공격 애니메이션 실행 (공통 로직)</summary>
    protected virtual void ExecuteAttackAnimation()
    {
        if (animator == null)
        {
            Debug.LogError($"[{gameObject.name}] Animator가 null입니다!");
            return;
        }

        string attackTrigger = GetAttackTrigger();
        
        if (HasAnimatorParameter(attackTrigger))
        {
            animator.SetTrigger(attackTrigger);
            Debug.Log($"[{gameObject.name}] 공격 애니메이션 실행: {attackTrigger}");
        }
        else
        {
            // 폴백: 기본 Attack 트리거 사용
            string fallbackTrigger = "Attack";
            if (HasAnimatorParameter(fallbackTrigger))
            {
                animator.SetTrigger(fallbackTrigger);
                Debug.Log($"[{gameObject.name}] 폴백 공격 애니메이션: {fallbackTrigger}");
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] 사용 가능한 공격 트리거가 없습니다!");
            }
        }
        
        // 공격 단계 전환 (1 ↔ 2)
        attackStep = attackStep == 1 ? 2 : 1;
        Debug.Log($"[{gameObject.name}] 다음 공격 단계: Attack{attackStep}");
    }
    
    /// <summary>공격 트리거 이름 반환 (파생 클래스에서 오버라이드)</summary>
    protected virtual string GetAttackTrigger()
    {
        return $"Attack{attackStep}"; // 기본: Attack1, Attack2
    }
    
    /// <summary>공격 단계 초기화</summary>
    protected virtual void ResetAttackStep()
    {
        attackStep = 1;
        Debug.Log($"[{gameObject.name}] 공격 단계 초기화: Attack{attackStep}");
    }
    
    /// <summary>걷기 애니메이션 제어 (공통 로직)</summary>
    public virtual void SetWalkingAnimation(bool isWalking)
    {
        if (animator != null)
        {
            // 파라미터 존재 여부 확인
            if (HasAnimatorParameter("IsWalking"))
            {
                animator.SetBool("IsWalking", isWalking);
                Debug.Log($"[{gameObject.name}] 걷기 애니메이션 설정: {isWalking}");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] Animator에 'IsWalking' bool 파라미터가 없습니다!");
                
                // 대안으로 Walk 트리거 사용
                if (isWalking && HasAnimatorParameter("Walk"))
                {
                    animator.SetTrigger("Walk");
                    Debug.Log($"[{gameObject.name}] Walk 트리거로 걷기 애니메이션 실행");
                }
            }
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Animator가 null입니다!");
        }
    }

    // ---------- API ----------
    /// <summary>스탯 초기화. Player/Enemy에서 호출</summary>
    public virtual void InitStats(int hp, int atk, float interval = 1f)
    {
        maxHp      = hp;
        currentHp  = hp;
        this.atk   = atk;
        attackInterval = interval;
        Debug.Log($"[CharacterBase] {gameObject.name} InitStats: hp={hp}, atk={atk}, Attack property={Attack}");
        OnHealthChanged?.Invoke(currentHp, maxHp);
    }

    public virtual void TakeDamage(int dmg)
    {
        currentHp = Mathf.Max(0, currentHp - dmg);
        OnHealthChanged?.Invoke(currentHp, maxHp);
        // TODO: 피격 이펙트 호출
        if (currentHp == 0)
            Die();
    }
    
    /// <summary>체력 회복</summary>
    public virtual void Heal(int amount)
    {
        currentHp = Mathf.Min(maxHp, currentHp + amount);
        OnHealthChanged?.Invoke(currentHp, maxHp);
        Debug.Log($"[{gameObject.name}] 체력 회복: {amount}, 현재 HP: {currentHp}/{maxHp}");
    }
    
    /// <summary>체력 완전 회복</summary>
    public virtual void FullHeal()
    {
        currentHp = maxHp;
        OnHealthChanged?.Invoke(currentHp, maxHp);
        Debug.Log($"[{gameObject.name}] 체력 완전 회복: {currentHp}/{maxHp}");
    }
    
    /// <summary>사망 여부 확인</summary>
    public bool IsDead => currentHp <= 0;

    // ---------- 추상 / 가상 ----------
    protected abstract void TryAttack();

    protected virtual void Die()
    {
        // Death 애니메이션 트리거 실행
        if (animator != null && HasAnimatorParameter("Death"))
        {
            animator.SetTrigger("Death");
            Debug.Log($"[{gameObject.name}] Death 애니메이션 실행");
        }
        
        OnDeath?.Invoke(this);              // 매니저에 알림
        
        // 파생 클래스에서 오버라이드하여 처리 방식 결정
        HandleDeath();
    }
    
    /// <summary>사망 처리 방식 (파생 클래스에서 오버라이드)</summary>
    protected virtual void HandleDeath()
    {
        // 기본: 즉시 파괴
        Destroy(gameObject);
    }
}