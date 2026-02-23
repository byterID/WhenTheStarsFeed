using UnityEngine;
using System.Collections;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance;

    [SerializeField] private int _startMoney = 100;
    [SerializeField] private int _passiveIncome = 5;
    [SerializeField] private float _incomeInterval = 2f;

    public int CurrentMoney { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        CurrentMoney = _startMoney;
        StartCoroutine(PassiveIncome());
    }

    private IEnumerator PassiveIncome()
    {
        while (true)
        {
            yield return new WaitForSeconds(_incomeInterval);

            CurrentMoney += _passiveIncome;
            Debug.Log("Монетки: " + CurrentMoney);
        }
    }

    public bool TrySpend(int amount)
    {
        if (CurrentMoney < amount)
            return false;

        CurrentMoney -= amount;
        return true;
    }
}