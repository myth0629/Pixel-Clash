using System;
using UnityEngine;

/// 웨이브마다 스폰되는 몬스터. 간단한 AI만 포함.
public class Enemy : CharacterBase
{
    private PlayerCharacter target;
    Animator animator;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }

    // stage, wave scaling용 필드
    public void Setup(int baseHp, int baseAtk, float interval = 1.2f)
    {
        InitStats(baseHp, baseAtk, interval);
    }

    /// <summary>자동 공격: 랜덤 살아있는 플레이어 타깃</summary>
    protected override void TryAttack()
    {
        target = BattleManager.Instance.GetRandomAlivePlayer();
        if (target == null) return;

        target.TakeDamage(atk);
        // TODO: 공격 애니·이펙트
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
    }
}