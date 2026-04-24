using UnityEngine;

/// <summary>
/// Самонаводящаяся пуля.
///
/// УЛУЧШЕНИЯ v4 (VFX):
///   1. Trail VFX — частицы следа за пулей в полёте (дым, искры, свечение).
///   2. Hit VFX — эффект попадания ориентирован по направлению полёта.
///   3. Muzzle Flash — опциональная вспышка при выстреле (отсоединяется от пули).
///   4. Корректная очистка: trail отсоединяется перед Destroy,
///      чтобы дотянуть свой lifetime и не исчезнуть резко.
///
/// СОХРАНЁННЫЕ УЛУЧШЕНИЯ v3:
///   - Скорость 28f, LookAt, lead targeting, адаптивный HIT_DISTANCE.
///
/// СОХРАНЁННЫЕ ИСПРАВЛЕНИЯ v2:
///   - Проверка дистанции вместо OnCollisionEnter.
///   - Init() с damage и armorPenetration.
///   - OnTriggerEnter как запасной способ попадания.
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float _speed = 28f;

    [Header("Поворот")]
    [Tooltip("Поворачивать пулю носом к цели каждый кадр")]
    [SerializeField] private bool _rotateToTarget = true;

    [Header("Предсказание позиции (Lead Targeting)")]
    [Tooltip("0 = летим прямо к врагу, 1 = полное упреждение.")]
    [Range(0f, 1f)]
    [SerializeField] private float _leadFactor = 0.5f;

    // ───────────────────────────────────────────────────────────────────
    //  VFX
    // ───────────────────────────────────────────────────────────────────

    [Header("VFX — Вспышка выстрела (Muzzle Flash)")]
    [Tooltip("Префаб вспышки. Спавнится в точке появления пули и тут же отсоединяется.")]
    [SerializeField] private GameObject _muzzleFlashPrefab;

    [Header("VFX — Трейл в полёте")]
    [Tooltip("ParticleSystem-дочерний объект пули, играющий пока она летит. " +
             "Перетащи сюда дочерний GO с ParticleSystem (дым/искры за пулей).")]
    [SerializeField] private ParticleSystem _trailPS;

    [Tooltip("Если true — при попадании/уничтожении трейл отсоединяется " +
             "и доигрывает, а не исчезает резко.")]
    [SerializeField] private bool _detachTrailOnDestroy = true;

    [Header("VFX — Попадание (Impact)")]
    [Tooltip("Префаб эффекта попадания (взрыв, искры и т.д.)")]
    [SerializeField] private GameObject _hitVFXPrefab;

    [Tooltip("Масштаб эффекта попадания. Позволяет одному префабу " +
             "работать на разных калибрах.")]
    [SerializeField] private float _hitVFXScale = 1f;

    // ── Параметры — задаются башней через Init() ──────────────────────
    private Transform _target;
    private float _damage = 1f;
    private float _armorPenetration;

    // ── Кэш ──────────────────────────────────────────────────────────
    private Rigidbody _targetRigidbody;
    private float _hitDistance;
    private float _lifetime;
    private Vector3 _lastMoveDirection = Vector3.forward;

    private const float MAX_LIFETIME = 8f;

    // Звук — попадание пули
    private SoundFeedback _soundFeedback;

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

        _targetRigidbody = target != null ? target.GetComponent<Rigidbody>() : null;
        _hitDistance = Mathf.Max(0.35f, _speed * Time.fixedDeltaTime * 1.5f);

        _soundFeedback = ServiceLocator.TryGet<SoundFeedback>();

        if (_rotateToTarget && _target != null)
            transform.LookAt(GetAimPoint());

        // ── Muzzle Flash ──────────────────────────────────────────────
        SpawnMuzzleFlash();

        // ── Trail — запускаем если не играл ───────────────────────────
        if (_trailPS != null && !_trailPS.isPlaying)
            _trailPS.Play();
    }

    private void Update()
    {
        _lifetime += Time.deltaTime;

        if (_lifetime >= MAX_LIFETIME)
        {
            CleanupAndDestroy();
            return;
        }

        if (_target == null)
        {
            CleanupAndDestroy();
            return;
        }

        Vector3 aimPoint = GetAimPoint();
        float step = _speed * Time.deltaTime;
        Vector3 prevPos = transform.position;

        transform.position = Vector3.MoveTowards(transform.position, aimPoint, step);

        // Запоминаем направление для ориентации VFX попадания
        Vector3 delta = transform.position - prevPos;
        if (delta.sqrMagnitude > 0.0001f)
            _lastMoveDirection = delta.normalized;

        if (_rotateToTarget)
        {
            Vector3 direction = aimPoint - transform.position;
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction);
        }

        if (Vector3.Distance(transform.position, _target.position) < _hitDistance)
        {
            HitTarget();
        }
    }

    // ── Предсказание позиции цели ─────────────────────────────────────

    private Vector3 GetAimPoint()
    {
        if (_target == null) return transform.position;

        Vector3 targetPos = _target.position;

        if (_leadFactor > 0f && _targetRigidbody != null)
        {
            float dist = Vector3.Distance(transform.position, targetPos);
            float timeToTarget = dist / Mathf.Max(_speed, 0.1f);
            Vector3 predictedPos = targetPos
                + _targetRigidbody.linearVelocity * timeToTarget * _leadFactor;
            return predictedPos;
        }

        return targetPos;
    }

    // ── Попадание ─────────────────────────────────────────────────────

    private void HitTarget()
    {
        if (_target == null) return;

        IDamageable damageable = _target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(_damage, _armorPenetration);
        }
        else
        {
            EnemyActions enemy = _target.GetComponent<EnemyActions>();
            enemy?.CallHit();
        }

        SpawnHitVFX();
        _soundFeedback?.PlayBulletHit();
        CleanupAndDestroy();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;
        if (_target == null) return;
        if (other.transform != _target && other.transform != _target.root) return;

        HitTarget();
    }

    // ───────────────────────────────────────────────────────────────────
    //  VFX METHODS
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Вспышка выстрела. Спавнится в точке появления пули,
    /// сразу отсоединяется и живёт самостоятельно.
    /// </summary>
    private void SpawnMuzzleFlash()
    {
        if (_muzzleFlashPrefab == null) return;

        GameObject muzzle = Instantiate(
            _muzzleFlashPrefab,
            transform.position,
            transform.rotation
        );

        // Авто-уничтожение
        float lifetime = GetVFXLifetime(muzzle, 1.5f);
        Destroy(muzzle, lifetime);
    }

    /// <summary>
    /// Эффект попадания. Ориентирован по направлению полёта пули,
    /// так что искры и осколки разлетаются «от снаряда».
    /// </summary>
private void SpawnHitVFX()
{
    Debug.Log($"[Bullet] SpawnHitVFX called. Prefab: {_hitVFXPrefab}");

    if (_hitVFXPrefab == null)
    {
        Debug.LogWarning("[Bullet] _hitVFXPrefab is NULL!");
        return;
    }

    Quaternion rotation = Quaternion.LookRotation(_lastMoveDirection);

    GameObject vfx = Instantiate(
        _hitVFXPrefab,
        transform.position,
        rotation
    );

    Debug.Log($"[Bullet] VFX spawned at {transform.position}, " +
              $"rotation: {rotation.eulerAngles}, " +
              $"scale: {_hitVFXScale}");

    // Проверяем все ParticleSystem в префабе
    ParticleSystem[] systems = vfx.GetComponentsInChildren<ParticleSystem>();
    Debug.Log($"[Bullet] Found {systems.Length} ParticleSystems in VFX prefab");
    foreach (ParticleSystem ps in systems)
    {
        Debug.Log($"  - {ps.name}: PlayOnAwake={ps.main.playOnAwake}, " +
                  $"IsPlaying={ps.isPlaying}, " +
                  $"MaxParticles={ps.main.maxParticles}, " +
                  $"Duration={ps.main.duration}");
    }

    if (!Mathf.Approximately(_hitVFXScale, 1f))
        vfx.transform.localScale = Vector3.one * _hitVFXScale;

    float lifetime = GetVFXLifetime(vfx, 2f);
    Debug.Log($"[Bullet] VFX will be destroyed in {lifetime:F2}s");
    Destroy(vfx, lifetime);
}


    // ───────────────────────────────────────────────────────────────────
    //  CLEANUP
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Корректно уничтожает пулю:
    /// - Отсоединяет трейл, чтобы он мог доиграть;
    /// - Останавливает эмиссию трейла (частицы доживают свой lifetime);
    /// - Уничтожает GameObject пули.
    /// </summary>
    private void CleanupAndDestroy()
    {
        DetachTrail();
        Destroy(gameObject);
    }

    /// <summary>
    /// Отсоединяет трейл от пули и позволяет ему плавно погаснуть.
    /// Без этого трейл исчез бы мгновенно вместе с пулей.
    /// </summary>
    private void DetachTrail()
    {
        if (_trailPS == null) return;
        if (!_detachTrailOnDestroy) return;

        // Отсоединяем от пули
        _trailPS.transform.SetParent(null);

        // Прекращаем эмиссию, но уже выпущенные частицы доживают
        _trailPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // Уничтожаем GO трейла после того как все частицы погаснут
        float remaining = _trailPS.main.startLifetime.constantMax;
        Destroy(_trailPS.gameObject, remaining + 0.1f);

        // Обнуляем ссылку чтобы повторно не обработать
        _trailPS = null;
    }

    // ───────────────────────────────────────────────────────────────────
    //  UTILS
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Вычисляет сколько должен жить VFX объект.
    /// Ищет все ParticleSystem в иерархии и берёт самый длительный.
    /// </summary>
    private static float GetVFXLifetime(GameObject vfxRoot, float fallback)
    {
        ParticleSystem[] systems = vfxRoot.GetComponentsInChildren<ParticleSystem>();
        if (systems.Length == 0) return fallback;

        float maxLifetime = 0f;
        foreach (ParticleSystem ps in systems)
        {
            ParticleSystem.MainModule main = ps.main;
            float total = main.duration + main.startLifetime.constantMax;
            if (total > maxLifetime)
                maxLifetime = total;
        }

        return maxLifetime > 0f ? maxLifetime + 0.1f : fallback;
    }
}
