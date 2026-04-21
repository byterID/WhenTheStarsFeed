using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Главное здание игрока. Реализует IDamageable — враги атакуют его
/// точно так же, как атакуют игрока: через TakeDamage.
///
/// Настройка на сцене:
///   1. Поставьте этот компонент на объект главного здания.
///   2. Назначьте тег "Base" (или любой — это не обязательно).
///   3. (Опционально) подключите Slider для отображения HP базы в UI.
///   4. Убедитесь, что Script Execution Order для MainBase равен -50.
/// </summary>
public class MainBase : MonoBehaviour, IDamageable
{
    [Header("Здоровье базы")]
    [SerializeField] private float maxHp = 100f;

    [Header("UI (опционально)")]
    [SerializeField] private Slider hpSlider;        // полоска HP базы на экране

    // ── Состояние ─────────────────────────────────────────────────────
    private float _currentHp;
    private bool _isDead;

    // ── Событие — на него подпишется GameManager ──────────────────────
    public event System.Action OnBaseDestroyed;

    // ── Публичные свойства ────────────────────────────────────────────
    public float CurrentHp => _currentHp;
    public float MaxHp => maxHp;
    public bool IsDead => _isDead;

    // ── Lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        // Регистрируем себя в ServiceLocator — враги получат ссылку через него
        ServiceLocator.Register<MainBase>(this);

        _currentHp = maxHp;
        _isDead = false;

        RefreshSlider();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<MainBase>();
    }

    // ── IDamageable ───────────────────────────────────────────────────

    public void TakeDamage(float damage, float armorPenetration)
    {
        if (_isDead) return;

        // База не имеет брони по умолчанию, но armorPenetration не влияет на неё.
        // Если хочешь добавить броню базы — добавь [SerializeField] float _resistance.
        _currentHp -= damage;
        _currentHp = Mathf.Max(_currentHp, 0f);

        RefreshSlider();

        if (_currentHp <= 0f)
            Die();
    }

    // ── Смерть ────────────────────────────────────────────────────────

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        OnBaseDestroyed?.Invoke();
    }

    // ── UI ────────────────────────────────────────────────────────────

    private void RefreshSlider()
    {
        if (hpSlider != null)
            hpSlider.value = _currentHp / maxHp;
    }
}
