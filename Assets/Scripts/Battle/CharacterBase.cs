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

    // ---------- 유니티 라이프사이클 ----------
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

    // ---------- API ----------
    /// <summary>스탯 초기화. Player/Enemy에서 호출</summary>
    public virtual void InitStats(int hp, int atk, float interval = 1f)
    {
        maxHp      = hp;
        currentHp  = hp;
        this.atk   = atk;
        attackInterval = interval;
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

    // ---------- 추상 / 가상 ----------
    protected abstract void TryAttack();

    protected virtual void Die()
    {
        OnDeath?.Invoke(this);              // 매니저에 알림
        // TODO: 사망 FX / 풀 반납
        Destroy(gameObject);                // 풀링 쓰면 SetActive(false)로 교체
    }
}