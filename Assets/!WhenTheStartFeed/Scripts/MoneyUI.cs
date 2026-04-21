using UnityEngine;
using TMPro;

/// <summary>
/// Отображение денег в UI.
///
/// Изменения:
///   - Убран Update (опрос каждый кадр — расточительно).
///   - Вместо этого подписываемся на MoneyManager.OnMoneyChanged.
///   - Текст обновляется только когда деньги реально изменились.
///   - Опциональное форматирование больших чисел (1 250 вместо 1250).
/// </summary>
public class MoneyUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _moneyText;
    [SerializeField] private bool _formatWithSpaces = true; // "1 250" вместо "1250"

    private MoneyManager _moneyManager;

    // ── Lifecycle ─────────────────────────────────────────────────────

    private void Start()
    {
        // Start гарантирует что MoneyManager.Awake уже выполнился
        _moneyManager = ServiceLocator.TryGet<MoneyManager>();

        if (_moneyManager == null)
        {
            Debug.LogWarning("[MoneyUI] MoneyManager не найден в ServiceLocator!");
            return;
        }

        // Подписка на событие
        _moneyManager.OnMoneyChanged += UpdateText;

        // Инициализируем текст сразу
        UpdateText(_moneyManager.CurrentMoney);
    }

    private void OnDestroy()
    {
        if (_moneyManager != null)
            _moneyManager.OnMoneyChanged -= UpdateText;
    }

    // ── Обновление текста ─────────────────────────────────────────────

    private void UpdateText(int amount)
    {
        if (_moneyText == null) return;

        _moneyText.text = _formatWithSpaces
            ? FormatNumber(amount)
            : amount.ToString();
    }

    private static string FormatNumber(int n)
    {
        // Форматирование: 1250 → "1 250", 1000000 → "1 000 000"
        return n.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("ru-RU"));
    }
}
