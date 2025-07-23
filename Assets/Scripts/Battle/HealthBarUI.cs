using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 머리 위에 따라다니는 HP 바.
/// • Canvas(RenderMode: WorldSpace) 하위에 두고,
/// • fill 이미지만 있으면 된다.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image fill;          // 빨간 바
    [SerializeField] private Vector3 offset = new(0, 0, 0); // 캐릭터 위 살짝 띄우기

    private CharacterBase target;
    private Camera cam;

    public void Init(CharacterBase owner)
    {
        target = owner;
        cam = Camera.main;

        if (target == null)
        {
            Debug.LogError("HealthBarUI: target is null!");
            return;
        }

        // 초기 상태 동기화
        UpdateFill(owner.CurrentHp, owner.MaxHp);
        owner.OnHealthChanged += UpdateFill;        // Subscribe

        // 파괴 이벤트 구독
        owner.OnDeath += OnTargetDeath;
        
        Debug.Log($"HealthBarUI initialized for {owner.name}: {owner.CurrentHp}/{owner.MaxHp}");
    }

    private void LateUpdate()
    {
        if (target == null) return;
        // 월드 포지션 → 스크린 → 월드 스페이스 Canvas 위치 보정
        transform.position = target.transform.position + offset;
        transform.LookAt(transform.position + cam.transform.forward); // 항상 카메라 정면
    }

    private void UpdateFill(int cur, int max)
    {
        if (fill == null)
        {
            Debug.LogError("HealthBarUI: fill Image is null!");
            return;
        }
        
        if (max <= 0)
        {
            Debug.LogWarning($"HealthBarUI: Invalid max HP ({max}) for {target?.name}");
            fill.fillAmount = 0f;
            return;
        }
        
        float fillAmount = (float)cur / max;
        fill.fillAmount = fillAmount;
        
        Debug.Log($"HealthBarUI: {target?.name} HP updated: {cur}/{max} = {fillAmount:F2}");
    }

    private void OnTargetDeath(CharacterBase deadTarget)
    {
        Debug.Log($"HealthBarUI: Target {deadTarget.name} died, destroying health bar");
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (target != null)
        {
            target.OnHealthChanged -= UpdateFill;
            target.OnDeath -= OnTargetDeath;
            Debug.Log($"HealthBarUI: Unsubscribed events for {target.name}");
        }
    }
}