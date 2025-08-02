using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// 웨이브마다 스폰되는 몬스터. 간단한 AI만 포함.
public class Enemy : CharacterBase
{
    private PlayerCharacter target;
    private Animator animator;
    private MonsterData monsterData;
    private int currentWave;
    
    [Header("등장 애니메이션")]
    [SerializeField] private float walkInDuration = 2f;
    [SerializeField] private float offscreenDistance = 10f;
    
    private Vector3 targetPosition;
    private bool isWalkingIn = false;
    private bool canAttack = false;  // 전투 시작 가능 여부
    
    protected override void Update()
    {
        // 등장 애니메이션 중이거나 전투 시작 전에는 공격하지 않음
        if (isWalkingIn || !canAttack)
            return;
            
        base.Update();
    }

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        if (animator == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Enemy Animator를 찾을 수 없습니다!");
        }
        else
        {
            Debug.Log($"[{gameObject.name}] Enemy Animator 찾음: {animator.gameObject.name}");
            
            // 애니메이터 파라미터 목록 출력
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                Debug.Log($"[{gameObject.name}] 애니메이터 파라미터: {param.name} ({param.type})");
            }
        }
    }

    // MonsterData를 사용한 셋업
    public void Setup(MonsterData data, int waveNumber)
    {
        monsterData = data;
        currentWave = waveNumber;
        
        (int hp, int atk) = data.GetScaledStats(waveNumber);
        InitStats(hp, atk, data.attackInterval);
    }

    // 기존 호환성을 위한 오버로드
    public void Setup(int baseHp, int baseAtk, float interval = 1.2f)
    {
        InitStats(baseHp, baseAtk, interval);
    }

    /// <summary>자동 공격: MonsterData 설정에 따른 타깃 선택</summary>
    protected override void TryAttack()
    {
        target = BattleManager.Instance.GetRandomAlivePlayer();
        if (target == null) 
        {
            Debug.LogWarning($"[{gameObject.name}] 타겟할 플레이어가 없습니다!");
            return;
        }

        Debug.Log($"[{gameObject.name}] 플레이어 {target.name} 공격!");
        // 애니메이션 트리거만 실행 (실제 데미지는 DealDamage에서)
        animator.SetTrigger("Attack");
    }
    
    // 타격 프레임(애니메이션 이벤트)에서 호출
    public void DealDamage()
    {
        if (target != null && target.isActiveAndEnabled)
            target.TakeDamage(atk);

        target = null;  // 클린업
    }
    
    public override void TakeDamage(int dmg)
    {
        base.TakeDamage(dmg);
        Debug.Log($"Enemy HP: {currentHp}/{maxHp}");
        
        // 사망 시 보상 지급
        if (currentHp <= 0)
        {
            animator.SetTrigger("Death");
            Debug.Log($"Enemy 사망! monsterData: {monsterData != null}, GameDataManager: {GameDataManager.Instance != null}, currentWave: {currentWave}");
            
            if (monsterData != null)
            {
                (int exp, int gold) = monsterData.GetScaledRewards(currentWave);
                Debug.Log($"계산된 보상: EXP={exp}, Gold={gold}");
                
                // 실제 보상 지급
                if (GameDataManager.Instance != null)
                {
                    Debug.Log($"보상 지급 전 - 현재 골드: {GameDataManager.Instance.CurrentGold}, 현재 EXP: {GameDataManager.Instance.CurrentExp}");
                    
                    GameDataManager.Instance.AddGold(gold);
                    GameDataManager.Instance.AddExp(exp);
                    
                    Debug.Log($"보상 지급 후 - 현재 골드: {GameDataManager.Instance.CurrentGold}, 현재 EXP: {GameDataManager.Instance.CurrentExp}");
                    
                    // UI 이펙트 표시
                    GameDataUI.ShowGoldReward(gold);
                }
                else
                {
                    Debug.LogError("GameDataManager.Instance가 null입니다!");
                }
                
                Debug.Log($"Monster defeated! Gained {exp} EXP, {gold} Gold");
            }
            else
            {
                Debug.LogError("MonsterData가 null입니다! 보상을 지급할 수 없습니다.");
            }
        }
    }

    public MonsterData GetMonsterData()
    {
        return monsterData;
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
    
    /// <summary>화면 밖에서 걸어들어오는 애니메이션 시작</summary>
    public void StartWalkInAnimation(Vector3 finalPosition)
    {
        targetPosition = finalPosition;
        
        // 화면 밖 위치로 이동 (오른쪽에서 들어옴)
        Vector3 offscreenPosition = finalPosition + Vector3.right * offscreenDistance;
        transform.localPosition = offscreenPosition;
        
        Debug.Log($"[{gameObject.name}] 몬스터 등장 시작: {offscreenPosition} → {finalPosition}");
        
        // 걷기 애니메이션 시작
        SetWalkingAnimation(true);
        
        // 걸어들어오는 코루틴 시작
        StartCoroutine(WalkInCoroutine());
    }
    
    /// <summary>걸어들어오는 코루틴</summary>
    private IEnumerator WalkInCoroutine()
    {
        isWalkingIn = true;
        Vector3 startPosition = transform.localPosition;
        float elapsedTime = 0f;
        
        while (elapsedTime < walkInDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / walkInDuration;
            
            // 부드러운 이동 (EaseOut)
            float smoothProgress = 1f - Mathf.Pow(1f - progress, 3f);
            transform.localPosition = Vector3.Lerp(startPosition, targetPosition, smoothProgress);
            
            yield return null;
        }
        
        // 최종 위치 설정
        transform.localPosition = targetPosition;
        
        // 걷기 애니메이션 중지
        SetWalkingAnimation(false);
        isWalkingIn = false;
        
        Debug.Log($"[{gameObject.name}] 몬스터 등장 완료");
    }
    
    /// <summary>현재 등장 애니메이션 중인지 확인</summary>
    public bool IsWalkingIn()
    {
        return isWalkingIn;
    }
    
    /// <summary>BattleManager에서 호출 - 전투 시작</summary>
    public void StartCombat()
    {
        canAttack = true;
        Debug.Log($"[{gameObject.name}] 전투 시작!");
    }
}