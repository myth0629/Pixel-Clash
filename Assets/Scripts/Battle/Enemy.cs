using System;
using UnityEngine;

/// 웨이브마다 스폰되는 몬스터. 간단한 AI만 포함.
public class Enemy : CharacterBase
{
    private PlayerCharacter target;
    private Animator animator;
    private MonsterData monsterData;
    private int currentWave;

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
        if (target == null) return;

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
}