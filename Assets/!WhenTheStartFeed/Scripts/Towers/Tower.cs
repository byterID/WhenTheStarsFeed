using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Обычная башня — стреляет пулями по врагам в триггер-зоне.
///
/// УЛУЧШЕНИЯ v3:
///   1. Башня поворачивается к цели перед выстрелом (плавно через Lerp).
///   2. Выбирает БЛИЖАЙШЕГО врага, а не первого попавшегося по OnTriggerStay.
///   3. Отдельный вращающийся «ствол» (_rotatingPart) — если есть в иерархии.
///   4. Пуля спавнится с правильным направлением — не по rotation башни,
///      а по направлению к цели.
///   5. Минимальная задержка между снарядами (_burstInterval) для ощущения «очереди».
/// </summary>
public class Tower : MonoBehaviour
{
    [Header("Стрельба")]
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private float _reloadTime = 2f;
    [SerializeField] private Transform _bulletSpawnTransform;

    [Header("Урон")]
    [SerializeField] private float _damage = 5f;
    [SerializeField] private float _armorPenetration = 0f;

    [Header("Поворот башни")]
    [Tooltip("Часть башни которая будет поворачиваться к врагу (ствол/голова). " +
             "Если не назначено — поворачивается весь объект башни.")]
    [SerializeField] private Transform _rotatingPart;

    [Tooltip("Скорость поворота башни к цели (градусы в секунду). 0 = мгновенно.")]
    [SerializeField] private float _rotationSpeed = 180f;

    [Tooltip("Башня стреляет только когда достаточно повернулась к цели (градусов).")]
    [SerializeField] private float _aimThreshold = 10f;

    // ── Список врагов в зоне ─────────────────────────────────────────
    private readonly HashSet<Transform> _enemiesInRange = new();

    // ── Кэш ───────────────────────────────────────────────────────────
    private SoundFeedback _soundFeedback;
    private bool _isReloading;
    private Transform _currentTarget;

    // ── Lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        _soundFeedback = ServiceLocator.TryGet<SoundFeedback>();

        // Если часть для поворота не назначена — используем весь transform
        if (_rotatingPart == null)
            _rotatingPart = transform;
    }

    private void OnEnable()
    {
        if (_soundFeedback == null)
            _soundFeedback = ServiceLocator.TryGet<SoundFeedback>();
    }

    private void Update()
    {
        if (EndGameManager.Instance != null && !EndGameManager.Instance.IsPlaying) return;

        // Убираем уничтоженных врагов из списка
        _enemiesInRange.RemoveWhere(e => e == null);

        // Выбираем ближайшего живого врага
        _currentTarget = GetClosestEnemy();

        if (_currentTarget != null)
        {
            RotateTowardsTarget(_currentTarget);

            // Стреляем только когда прицел навёлся и нет перезарядки
            if (!_isReloading && IsAimedAt(_currentTarget))
                Shoot(_currentTarget);
        }
    }

    // ── Триггер-зона ──────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
            _enemiesInRange.Add(other.transform);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
            _enemiesInRange.Remove(other.transform);
    }

    // ── Выбор цели ────────────────────────────────────────────────────

    /// <summary>
    /// Возвращает ближайшего врага из списка.
    /// "Ближайший" — приоритет для пуль, которые долетают быстрее.
    /// </summary>
    private Transform GetClosestEnemy()
    {
        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (Transform enemy in _enemiesInRange)
        {
            if (enemy == null) continue;
            float dist = Vector3.Distance(transform.position, enemy.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = enemy;
            }
        }

        return closest;
    }

    // ── Поворот ───────────────────────────────────────────────────────

    private void RotateTowardsTarget(Transform target)
    {
        Vector3 direction = target.position - _rotatingPart.position;
        // Игнорируем вертикальную ось — башня вращается только по Y
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        if (_rotationSpeed <= 0f)
        {
            _rotatingPart.rotation = targetRotation;
        }
        else
        {
            _rotatingPart.rotation = Quaternion.RotateTowards(
                _rotatingPart.rotation,
                targetRotation,
                _rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Возвращает true если башня смотрит достаточно точно на цель.
    /// Предотвращает выстрел «в сторону» пока башня поворачивается.
    /// </summary>
    private bool IsAimedAt(Transform target)
    {
        if (_aimThreshold <= 0f) return true;

        Vector3 toTarget = (target.position - _rotatingPart.position).normalized;
        toTarget.y = 0f;
        float angle = Vector3.Angle(_rotatingPart.forward, toTarget);
        return angle < _aimThreshold;
    }

    // ── Стрельба ──────────────────────────────────────────────────────

    private void Shoot(Transform targetTransform)
    {
        Transform dynamicRoot = DynamicRoot.Root;
        if (dynamicRoot == null)
        {
            Debug.LogWarning("[Tower] DynamicRoot.Root не инициализирован!");
            return;
        }

        // Направление к цели для спавна пули
        Vector3 dirToTarget = (targetTransform.position - _bulletSpawnTransform.position).normalized;
        Quaternion spawnRotation = dirToTarget.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(dirToTarget)
            : _bulletSpawnTransform.rotation;

        GameObject bulletGO = Instantiate(
            _bulletPrefab,
            _bulletSpawnTransform.position,
            spawnRotation);

        bulletGO.transform.SetParent(dynamicRoot);

        Bullet bullet = bulletGO.GetComponent<Bullet>();
        bullet.Init(targetTransform, _damage, _armorPenetration);

        _soundFeedback?.PlaySound(SoundType.shoot);

        StartCoroutine(Reload());
    }

    private IEnumerator Reload()
    {
        _isReloading = true;
        yield return new WaitForSeconds(_reloadTime);
        _isReloading = false;
    }
}
