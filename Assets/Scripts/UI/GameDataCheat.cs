using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 개발자용 치트 패널
/// </summary>
public class GameDataCheat : MonoBehaviour
{
    [Header("치트 UI")]
    [SerializeField] private Button addGoldButton;
    [SerializeField] private Button addExpButton;
    [SerializeField] private Button resetDataButton;
    [SerializeField] private InputField goldInput;
    [SerializeField] private InputField expInput;

    [Header("기본 값")]
    [SerializeField] private int defaultGoldAmount = 1000;
    [SerializeField] private int defaultExpAmount = 50;

    private void Start()
    {
        SetupButtons();
    }

    private void SetupButtons()
    {
        if (addGoldButton != null)
            addGoldButton.onClick.AddListener(AddGold);

        if (addExpButton != null)
            addExpButton.onClick.AddListener(AddExp);

        if (resetDataButton != null)
            resetDataButton.onClick.AddListener(ResetData);
    }

    public void AddGold()
    {
        int amount = defaultGoldAmount;
        
        if (goldInput != null && int.TryParse(goldInput.text, out int inputAmount))
        {
            amount = inputAmount;
        }

        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.AddGold(amount);
        }
    }

    public void AddExp()
    {
        int amount = defaultExpAmount;
        
        if (expInput != null && int.TryParse(expInput.text, out int inputAmount))
        {
            amount = inputAmount;
        }

        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.AddExp(amount);
        }
    }

    public void ResetData()
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.ResetGameData();
        }
    }

    // 키보드 단축키
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            AddGold();

        if (Input.GetKeyDown(KeyCode.F2))
            AddExp();

        if (Input.GetKeyDown(KeyCode.F12))
            ResetData();
    }
}
