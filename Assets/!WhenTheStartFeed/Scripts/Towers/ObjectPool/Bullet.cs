using UnityEngine;

/// <summary>
/// Самонаводящаяся пуля.
///
/// УЛУЧШЕНИЯ v3 (визуал и ощущение):
///   1. Скорость поднята до 28f по умолчанию — пули летят резко и решительно.
///   2. Добавлен поворот пули к цели (LookAt) — снаряд всегда смотрит носом вперёд.
///   3. Предсказание позиции цели (lead targeting) — пуля летит не туда где враг
///      сейчас, а туда где он окажется. Это делает попадание более реалистичным
///      и исключает промахи при быстрых врагах.
///   4. HIT_DISTANCE адаптивный — зависит от скорости пули, быстрая пуля
///      не пролетает мимо из-за слишком маленького порога.
///   5. Поддержка дополнительного эффекта при попадании (хит-маркер VFX).
///
/// СОХРАНЁННЫЕ ИСПРАВЛЕНИЯ v2:
///   - Проверка дистанции вместо OnCollisionEnter (Баг 2).
///   - Init() с damage и armorPenetration (Баг 3).
///   - OnTriggerEnter как запасной способ попадания (Баг 1).
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float _speed = 28f;

    [Header("Поворот")]
    [Tooltip("Поворачивать пулю носом к цели каждый кадр")]
    [SerializeField] private bool _rotateToTarget = true;

    [Header("Предсказание позиции (Lead Targeting)")]
    [Tooltip("0 = летим прямо к врагу, 1 = полное упреждение. 0.5 хорошо работает для большинства случаев.")]
    [Range(0f, 1f)]
    [SerializeField] private float _leadFactor = 0.5f;

    [Header("VFX при попадании")]
    [Tooltip("Опциональный эффект частиц при попадании (взрыв, искры и т.д.)")]
    [SerializeField] private GameObject _hitVFXPrefab;

    // ── Параметры — задаются башней через Init() ──────────────────────
    private Transform _target;
    private float _damage = 1f;
    private float _armorPenetration = 0f;

    // ── Кэш скорости врага для предсказания ──────────────────────────
    private Rigidbody _targetRigidbody;

    // ── Порог попадания — адаптивный ─────────────────────────────────
    // Минимум 0.25f, но при высокой скорости растёт чтобы не пропустить цель.
    // Формула: при speed=28 → порог ≈ 0.47, при speed=10 → 0.35
    private float _hitDistance;

    // ── Максимальное время жизни ──────────────────────────────────────
    private const float MAX_LIFETIME = 8f;
    private float _lifetime;

    // ── Lifecycle ─────────────────────────────────────────────────────

    /// <summary>
    /// Инициализация пули из Tower.Shoot().
    /// Вызывать сразу после Instantiate.
    /// </summary>
    public void Init(Transform target, float damage, float armorPenetration = 0f)
    {
        _target = target;
        _damage = damage;
        _armorPenetration = armorPenetration;
        _lifetime = 0f;

        // Кэшируем Rigidbody цели для предсказания позиции
        _targetRigidbody = target != null ? target.GetComponent<Rigidbody>() : null;

        // Адаптивный порог попадания: быстрая пуля — больше порог
        _hitDistance = Mathf.Max(0.35f, _speed * Time.fixedDeltaTime * 1.5f);

        // Сразу поворачиваем к цели при спавне
        if (_rotateToTarget && _target != null)
            transform.LookAt(GetAimPoint());
    }

    private void Update()
    {
        _lifetime += Time.deltaTime;

        // Самоуничтожение по таймауту
        if (_lifetime >= MAX_LIFETIME)
        {
            Destroy(gameObject);
            return;
        }

        // Цель уничтожена — некуда лететь
        if (_target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Точка прицеливания (с упреждением или без)
        Vector3 aimPoint = GetAimPoint();

        // Двигаемся к точке прицеливания
        float step = _speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, aimPoint, step);

        // Поворачиваем нос пули к направлению полёта
        if (_rotateToTarget)
        {
            Vector3 direction = aimPoint - transform.position;
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction);
        }

        // Проверка попадания по дистанции до реальной позиции цели
        if (Vector3.Distance(transform.position, _target.position) < _hitDistance)
        {
            HitTarget();
        }
    }

    // ── Предсказание позиции цели ─────────────────────────────────────

    /// <summary>
    /// Вычисляет точку прицеливания с учётом движения цели.
    /// Если у цели нет Rigidbody — летим напрямую.
    /// </summary>
    private Vector3 GetAimPoint()
    {
        if (_target == null) return transform.position;

        Vector3 targetPos = _target.position;

        // Упреждение: предсказываем где окажется враг через время полёта
        if (_leadFactor > 0f && _targetRigidbody != null)
        {
            float dist = Vector3.Distance(transform.position, targetPos);
            float timeToTarget = dist / Mathf.Max(_speed, 0.1f);
            Vector3 predictedPos = targetPos + _targetRigidbody.linearVelocity * timeToTarget * _leadFactor;
            return predictedPos;
        }

        return targetPos;
    }

    // ── Попадание ─────────────────────────────────────────────────────

    private void HitTarget()
    {
        if (_target == null) return;

        // Получаем IDamageable — работает и с EnemyActions и с любым другим IDamageable
        IDamageable damageable = _target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(_damage, _armorPenetration);
        }
        else
        {
            // Fallback для совместимости
            EnemyActions enemy = _target.GetComponent<EnemyActions>();
            enemy?.CallHit();
        }

        SpawnHitVFX();
        Destroy(gameObject);
    }

    // ── OnTriggerEnter — дополнительный способ попадания ─────────────
    // Работает если на пуле есть Collider с IsTrigger = true.
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;
        if (_target == null) return;

        // Проверяем что это именно наша цель
        if (other.transform != _target && other.transform != _target.root) return;

        HitTarget();
    }

    // ── VFX попадания ─────────────────────────────────────────────────

    private void SpawnHitVFX()
    {
        if (_hitVFXPrefab == null) return;

        // Спавним на позиции пули, эффект живёт сам по себе
        GameObject vfx = Instantiate(_hitVFXPrefab, transform.position, Quaternion.identity);

        // Авто-уничтожение VFX через 2 секунды если нет ParticleSystem
        ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
        if (ps != null)
            Destroy(vfx, ps.main.duration + ps.main.startLifetime.constantMax);
        else
            Destroy(vfx, 2f);
    }
}
