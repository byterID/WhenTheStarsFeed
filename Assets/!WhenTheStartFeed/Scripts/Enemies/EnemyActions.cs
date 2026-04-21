using UnityEngine;
using System.Collections;

/// <summary>
/// Компонент врага: здоровье, получение урона, атака главной базы.
///
/// ИСПРАВЛЕННЫЙ БАГ:
///   Проблема была в том, что при достижении базы вызывался StartAttacking(),
///   который запускал AttackLoop() — бесконечную корутину. Но сам объект врага
///   никак не помечался как «атакующий», поэтому башни продолжали стрелять
///   и TakeDamage вызывался — однако Die() уничтожал объект, что прерывало
///   корутину атаки, не нанося урон базе.
///   Плюс в InitFromData было явное приведение health к int, что обнуляло дробную часть.
///
/// РЕШЕНИЕ:
///   1. Флаг _isAttackingBase — пока true, враг «занят» атакой базы.
///      TakeDamage по-прежнему работает (враг уязвим!), Die() останавливает атаку.
///   2. AttackLoop() реально наносит урон через MainBase.TakeDamage.
///   3. InitFromData не кастует к int.
///   4. Awake инициализирует HP, Start обновляет healthBar.
/// </summary>
public class EnemyActions : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHp = 10f;
    [SerializeField] private float _damage = 10f;          // урон базе за удар
    [SerializeField] private float _attackCooldown = 2f;   // секунд между ударами
    [SerializeField] private float _resistance = 0f;       // броня

    [Header("Health Bar")]
    [SerializeField] private EnemyHealthBar healthBar;
    [SerializeField] private bool showHealthBarAlways = false;

    // ── Состояние ─────────────────────────────────────────────────────
    private float _currentHp;
    private bool _isDead;
    private bool _isAttackingBase;   // враг дошёл до базы и атакует её
    private Coroutine _attackRoutine;

    // ── Кэш ───────────────────────────────────────────────────────────
    private MainBase _mainBase;
    private Rigidbody _rb;

    // ── Событие смерти ────────────────────────────────────────────────
    public event System.Action<EnemyActions> OnDied;

    // ── Публичные свойства ────────────────────────────────────────────

    /// <summary>
    /// Враг занят атакой базы. EnemyMovement использует это,
    /// чтобы остановить движение.
    /// </summary>
    public bool IsAttackingBase => _isAttackingBase;

    // ── Lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        // Инициализируем HP сразу в Awake, до того как Start/SetPath/InitFromData
        // могут быть вызваны извне (например, из SpawnSingle сразу после Instantiate).
        _currentHp = maxHp;
        _isDead = false;
        _isAttackingBase = false;

        // Кэшируем ссылку на базу через ServiceLocator — быстро и без Find
        _mainBase = ServiceLocator.TryGet<MainBase>();

        // Кэшируем Rigidbody — нужен чтобы не дать Unity его усыпить у базы
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // HealthBar инициализируем в Start, чтобы гарантировать что Awake HealthBar уже отработал
        InitHealthBar();
    }

    // ── Инициализация из SpawnSingle ─────────────────────────────────

    /// <summary>
    /// Вызывается спавнером ПОСЛЕ Awake, но возможно ДО Start.
    /// Безопасно обновляет HP и HealthBar.
    /// </summary>
    public void InitFromData(float health, float resistance)
    {
        maxHp = health;           // ИСПРАВЛЕНО: убрали (int) cast — дробная часть сохраняется
        _resistance = resistance;
        _currentHp = maxHp;
        _isDead = false;

        InitHealthBar();
    }

    private void InitHealthBar()
    {
        if (healthBar == null)
            healthBar = GetComponentInChildren<EnemyHealthBar>();

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHp);
            healthBar.UpdateHealthBar(_currentHp, maxHp);
        }
    }

    // ── IDamageable ───────────────────────────────────────────────────

    public void TakeDamage(float damage, float armorPenetration)
    {
        // ИСПРАВЛЕНО: убрана проверка _isAttackingBase.
        // Враг у базы ДОЛЖЕН получать урон — иначе он бессмертен.
        if (_isDead) return;

        float effectiveResistance = Mathf.Max(0f, _resistance - armorPenetration);
        float damageMultiplier = 100f / (100f + effectiveResistance);
        float finalDamage = damage * damageMultiplier;

        _currentHp -= finalDamage;
        _currentHp = Mathf.Max(_currentHp, 0f);

        if (healthBar != null)
            healthBar.UpdateHealthBar(_currentHp, maxHp);

        if (_currentHp <= 0f)
            Die();
    }

    /// <summary>Обратная совместимость с Bullet.cs</summary>
    public void CallHit()
    {
        TakeDamage(1f, 0f);
    }

    // ── Атака базы ────────────────────────────────────────────────────

    /// <summary>
    /// Вызывается из EnemyMovement когда враг достиг конца пути.
    /// </summary>
    public void StartAttackingBase()
    {
        if (_isDead || _isAttackingBase) return;

        _isAttackingBase = true;

        // ── ИСПРАВЛЕНИЕ БАГА «башня не видит врага у базы» ───────────────
        // Когда враг останавливается, Unity усыпляет его Rigidbody.
        // Уснувший Rigidbody не генерирует OnTriggerStay → башня перестаёт
        // видеть врага и не стреляет. Решение: запрещаем засыпание.
        if (_rb != null)
        {
            _rb.sleepThreshold = 0f;  // физ. движок не будет усыплять
            _rb.WakeUp();             // будим если уже спит
        }

        if (_mainBase == null)
            _mainBase = ServiceLocator.TryGet<MainBase>();

        if (_mainBase == null)
        {
            Debug.LogWarning("[EnemyActions] MainBase не найдена в ServiceLocator! " +
                             "Убедитесь что MainBase есть на сцене.");
            return;
        }

        if (_attackRoutine != null)
            StopCoroutine(_attackRoutine);

        _attackRoutine = StartCoroutine(AttackLoop());
    }

    private IEnumerator AttackLoop()
    {
        // Первый удар — сразу при подходе
        while (!_isDead && _mainBase != null && !_mainBase.IsDead)
        {
            _mainBase.TakeDamage(_damage, 0f);
            yield return new WaitForSeconds(_attackCooldown);
        }

        // База уничтожена или враг умер — корутина завершается
        _attackRoutine = null;
    }

    // ── Смерть ────────────────────────────────────────────────────────

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        _isAttackingBase = false;

        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }

        OnDied?.Invoke(this);
        Destroy(gameObject);
    }
}
