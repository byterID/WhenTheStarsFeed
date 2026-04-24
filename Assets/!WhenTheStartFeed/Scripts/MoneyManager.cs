using UnityEngine;
using System.Collections;

/// <summary>
/// Менеджер денег игрока.
///
/// Изменения:
///   - Регистрируется в ServiceLocator (убран static Instance — используйте
///     ServiceLocator.Get<MoneyManager>() или MoneyManager.Instance для совместимости).
///   - Пассивный доход останавливается при GameOver.
///   - Добавлено событие OnMoneyChanged — MoneyUI подписывается на него
///     вместо опроса в Update каждый кадр.
///   - Добавлен метод AddMoney (для наград за убийство врагов).
/// </summary>
public class MoneyManager : MonoBehaviour
{
    // Для удобства оставляем Instance (совместимость с PlacementState и другими)
    public static MoneyManager Instance { get; private set; }

    [Header("Настройки")]
    [SerializeField] private int _startMoney = 500;
    [SerializeField] private int _passiveIncome = 5;
    [SerializeField] private float _incomeInterval = 2f;

    // ── Состояние ─────────────────────────────────────────────────────
    public int CurrentMoney { get; private set; }

    // ── Событие — MoneyUI подпишется вместо Update-поллинга ──────────
    public event System.Action<int> OnMoneyChanged;

    private Coroutine _incomeCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        // Синглтон + ServiceLocator
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ServiceLocator.Register<MoneyManager>(this);
    }

    private void Start()
    {
        CurrentMoney = _startMoney;
        OnMoneyChanged?.Invoke(CurrentMoney);

        _incomeCoroutine = StartCoroutine(PassiveIncome());

        // Подписываемся на конец игры — остановить доход
        if (EndGameManager.Instance != null)
            EndGameManager.Instance.OnStateChanged += OnGameStateChanged;
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<MoneyManager>();

        if (Instance == this)
            Instance = null;

        if (EndGameManager.Instance != null)
            EndGameManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    // ── Пассивный доход ───────────────────────────────────────────────

    private IEnumerator PassiveIncome()
    {
        while (true)
        {
            yield return new WaitForSeconds(_incomeInterval);
            AddMoney(_passiveIncome);
        }
    }

    // ── Реакция на GameOver ───────────────────────────────────────────

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.GameOver && _incomeCoroutine != null)
        {
            StopCoroutine(_incomeCoroutine);
            _incomeCoroutine = null;
        }
    }

    // ── Публичный API ─────────────────────────────────────────────────

    /// <summary>Добавить деньги (награда за убийство врага и т.д.)</summary>
    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        CurrentMoney += amount;
        OnMoneyChanged?.Invoke(CurrentMoney);
    }

    /// <summary>Потратить деньги. Возвращает false если недостаточно.</summary>
    public bool TrySpend(int amount)
    {
        if (CurrentMoney < amount)
            return false;

        CurrentMoney -= amount;
        OnMoneyChanged?.Invoke(CurrentMoney);
        return true;
    }
}
