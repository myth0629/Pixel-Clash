using UnityEditor.U2D.Animation;
using UnityEngine;
using System.Collections;

/// 파티 슬롯에 들어가는 플레이어 캐릭터.
/// CharacterData + 레벨을 받아서 내부 스탯 계산.
public class PlayerCharacter : CharacterBase
{
    [HideInInspector] public CharacterData data;
    [HideInInspector] public int level = 1;
    
    private Enemy enemy;  // 현재 타겟 적
    private Animator animator;
    private bool canAttack = false;  // 공격 가능 여부

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        if (animator == null)
        {
            Debug.LogWarning($"[{gameObject.name}] PlayerCharacter Animator를 찾을 수 없습니다!");
        }
        else
        {
            Debug.Log($"[{gameObject.name}] PlayerCharacter Animator 찾음: {animator.gameObject.name}");
            
            // 애니메이터 파라미터 목록 출력
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                Debug.Log($"[{gameObject.name}] 애니메이터 파라미터: {param.name} ({param.type})");
            }
        }
    }

    /// <summary>외부(BattleManager)에서 호출: 캐릭터 SO & 레벨 세팅</summary>
    public void Setup(CharacterData cd, int lv)
    {
        data  = cd;
        level = lv;

        int hp  = Mathf.RoundToInt(cd.baseHp  * (1 + level * cd.hpGrowth));
        int atk = Mathf.RoundToInt(cd.baseAtk * (1 + level * cd.atkGrowth));

        // SPD 개념을 hpGrowth처럼 두고 interval 산출 가능
        float interval = attackInterval; // ex) 1초 기본값

        InitStats(hp, atk, interval);
    }
    
    protected override void Update()
    {
        // 공격 가능 상태가 아니면 공격하지 않음
        if (!canAttack)
            return;
        
        // BattleManager 상태 확인
        if (!BattleManager.Instance.IsBattleRunning)
        {
            Debug.LogWarning($"[{gameObject.name}] BattleManager.IsBattleRunning이 false입니다!");
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
        animator.SetTrigger("Attack");
    }
    
    // 타격 프레임(애니메이션 이벤트)에서 호출
    public void DealDamage()
    {
        if (enemy != null && enemy.isActiveAndEnabled)
            enemy.TakeDamage(atk);

        enemy = null;  // 클린업
    }
    
    /// <summary>걷기 애니메이션 제어</summary>
    public void SetWalkingAnimation(bool isWalking)
    {
        if (animator != null)
        {
            // 파라미터 존재 여부 확인
            bool hasWalkingParam = false;
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == "IsWalking" && param.type == AnimatorControllerParameterType.Bool)
                {
                    hasWalkingParam = true;
                    break;
                }
            }
            
            if (hasWalkingParam)
            {
                animator.SetBool("IsWalking", isWalking);
                Debug.Log($"[{gameObject.name}] 걷기 애니메이션 설정: {isWalking}");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] Animator에 'IsWalking' bool 파라미터가 없습니다!");
                
                // 대안으로 Walk 트리거 사용
                if (isWalking)
                {
                    // Walk 트리거가 있는지 확인
                    foreach (AnimatorControllerParameter param in animator.parameters)
                    {
                        if (param.name == "Walk" && param.type == AnimatorControllerParameterType.Trigger)
                        {
                            animator.SetTrigger("Walk");
                            Debug.Log($"[{gameObject.name}] Walk 트리거 실행");
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Animator가 null입니다!");
        }
    }
    
    /// <summary>BattleManager에서 호출 - 전투 시작</summary>
    public void StartCombat()
    {
        canAttack = true;
        Debug.Log($"[{gameObject.name}] 전투 시작!");
    }
    
    /// <summary>새로운 라운드 시작 시 호출 - 전투 준비</summary>
    public void StartNewRound()
    {
        canAttack = false;
        Debug.Log($"[{gameObject.name}] 새로운 라운드 준비 - BattleManager 딜레이 대기 중");
    }
}