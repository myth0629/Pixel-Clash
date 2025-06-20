using UnityEditor.U2D.Animation;
using UnityEngine;

/// 파티 슬롯에 들어가는 플레이어 캐릭터.
/// CharacterData + 레벨을 받아서 내부 스탯 계산.
public class PlayerCharacter : CharacterBase
{
    [HideInInspector] public CharacterData data;
    [HideInInspector] public int level = 1;
    
    private Enemy enemy;  // 현재 타겟 적
    Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
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

    /// <summary>자동 공격 로직: 가장 가까운 적 타깃팅</summary>
    protected override void TryAttack()
    {
        enemy = BattleManager.Instance.GetNearestEnemy(transform.position);
        if (enemy == null) return;
        
        animator.SetTrigger("Attack");
    }
    
    // 타격 프레임(애니메이션 이벤트)에서 호출
    public void DealDamage()
    {
        if (enemy != null && enemy.isActiveAndEnabled)
            enemy.TakeDamage(atk);

        enemy = null;  // 클린업
    }
}