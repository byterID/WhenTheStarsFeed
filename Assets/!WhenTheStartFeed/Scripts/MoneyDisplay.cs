using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MoneyDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private MoneyManager moneyManager;

    private void Update()
    {
        if (moneyManager != null && moneyText != null)
        {
            moneyText.text = moneyManager.CurrentMoney.ToString();
        }
    }
}