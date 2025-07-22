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
        if (currentHp <= 0 && monsterData != null)
        {
            (int exp, int gold) = monsterData.GetScaledRewards(currentWave);
            
            // 실제 보상 지급
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.AddGold(gold);
                GameDataManager.Instance.AddExp(exp);
                
                // UI 이펙트 표시
                GameDataUI.ShowGoldReward(gold);
            }
            
            Debug.Log($"Monster defeated! Gained {exp} EXP, {gold} Gold");
        }
    }

    public MonsterData GetMonsterData()
    {
        return monsterData;
    }
}