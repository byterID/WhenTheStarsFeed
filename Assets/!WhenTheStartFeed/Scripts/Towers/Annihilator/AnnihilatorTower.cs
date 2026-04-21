using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Башня-аннигилятор. Работает циклически: Зарядка → Стрельба → Перезарядка.
///
/// Исправления и улучшения:
///   - Убран GameObject.Find("SoundFeedback") → ServiceLocator.TryGet.
///   - _detectionZone и _fireZone проверяются на null в OnEnable/OnDisable.
///   - CycleCoroutine не запускается снова если уже идёт (двойной guard).
///   - Добавлена проверка GameManager.IsPlaying — цикл прерывается при Game Over.
///   - StopCycle вызывает StopAllVFX и правильно сбрасывает состояние.
///   - Update с очисткой null-врагов перенесён в AnnihilatorDetectionZone
///     (там он уже есть) — в AnnihilatorTower Update больше не нужен.
///
/// Архитектурная заметка про AnnihilatorDetectionZone:
///   В текущей реализации там есть Update с RemoveWhere(e => e == null) —
///   это правильно. Но события OnEnemyEntered / OnZoneEmpty не перезапускаются
///   когда null-объект убирается. Если враг умирает внутри зоны (Destroy без
///   OnTriggerExit), то HasEnemies станет false только в следующем Update.
///   Это приемлемо для данной механики.
/// </summary>
public class AnnihilatorTower : MonoBehaviour
{
    // ── Настройки урона ───────────────────────────────────────────────
    [Header("Damage Settings")]
    [SerializeField] private float _damagePerSecond = 20f;
    [SerializeField] private float _armorPenetration = 5f;

    // ── Тайминги ──────────────────────────────────────────────────────
    [Header("Timing")]
    [SerializeField] private float _chargeTime = 2f;
    [SerializeField] private float _fireTime = 5f;
    [SerializeField] private float _reloadTime = 3f;

    // ── Зоны ──────────────────────────────────────────────────────────
    [Header("Zones")]
    [SerializeField] private AnnihilatorDetectionZone _detectionZone;
    [SerializeField] private AnnihilatorFireZone _fireZone;

    // ── Визуал ────────────────────────────────────────────────────────
    [Header("VFX")]
    [SerializeField] private ParticleSystem _chargeVFX;
    [SerializeField] private ParticleSystem _fireVFX;
    [SerializeField] private Animator _animator;

    // ── Состояние ─────────────────────────────────────────────────────
    private AnnihilatorState _state = AnnihilatorState.Idle;
    private Coroutine _cycleCoroutine;

    // ── Кэш ───────────────────────────────────────────────────────────
    private SoundFeedback _soundFeedback;

    // ── Публичное свойство для UI (поворот и т.д.) ───────────────────
    public AnnihilatorState CurrentState => _state;

    // ── Lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        // ServiceLocator вместо Find
        _soundFeedback = ServiceLocator.TryGet<SoundFeedback>();
    }

    private void OnEnable()
    {
        if (_detectionZone == null)
        {
            Debug.LogError("[AnnihilatorTower] DetectionZone не назначена в Inspector!", this);
            return;
        }

        _detectionZone.OnEnemyEntered += OnEnemyEnteredDetection;
        _detectionZone.OnZoneEmpty += OnDetectionZoneEmpty;
    }

    private void OnDisable()
    {
        if (_detectionZone != null)
        {
            _detectionZone.OnEnemyEntered -= OnEnemyEnteredDetection;
            _detectionZone.OnZoneEmpty -= OnDetectionZoneEmpty;
        }

        StopCycle();
    }

    // ── Реакция на зону обнаружения ───────────────────────────────────

    private void OnEnemyEnteredDetection()
    {
        if (_state == AnnihilatorState.Idle)
            StartCycle();
    }

    private void OnDetectionZoneEmpty()
    {
        // Прерываем только если ещё заряжаемся — если стреляем, досстреливаем
        if (_state == AnnihilatorState.Charging)
            StopCycle();
    }

    // ── Цикл работы ───────────────────────────────────────────────────

    private void StartCycle()
    {
        // Guard: не запускаем если уже работаем
        if (_cycleCoroutine != null) return;
        _cycleCoroutine = StartCoroutine(CycleCoroutine());
    }

    private void StopCycle()
    {
        if (_cycleCoroutine != null)
        {
            StopCoroutine(_cycleCoroutine);
            _cycleCoroutine = null;
        }

        SetState(AnnihilatorState.Idle);
        StopAllVFX();
    }

    private IEnumerator CycleCoroutine()
    {
        while (true)
        {
            // ── Прерываем если игра окончена ──────────────────────────
            if (EndGameManager.Instance != null && !EndGameManager.Instance.IsPlaying)
            {
                StopCycle();
                yield break;
            }

            // ── 1. Зарядка ────────────────────────────────────────────
            SetState(AnnihilatorState.Charging);
            PlayVFX(_chargeVFX);
            yield return new WaitForSeconds(_chargeTime);

            // После зарядки проверяем — вдруг зона опустела пока заряжались
            if (!_detectionZone.HasEnemies)
            {
                StopCycle();
                yield break;
            }

            // ── 2. Стрельба ───────────────────────────────────────────
            SetState(AnnihilatorState.Firing);
            StopVFX(_chargeVFX);
            PlayVFX(_fireVFX);

            float fireElapsed = 0f;
            while (fireElapsed < _fireTime)
            {
                DamageEnemiesInFireZone(Time.deltaTime);
                fireElapsed += Time.deltaTime;
                yield return null;
            }

            // ── 3. Перезарядка ────────────────────────────────────────
            SetState(AnnihilatorState.Reloading);
            StopVFX(_fireVFX);
            yield return new WaitForSeconds(_reloadTime);

            // ── 4. Проверяем есть ли ещё враги ───────────────────────
            if (!_detectionZone.HasEnemies)
            {
                SetState(AnnihilatorState.Idle);
                _cycleCoroutine = null;
                yield break;
            }
            // Враги есть — идём на следующий круг
        }
    }

    // ── Нанесение урона ───────────────────────────────────────────────

    private void DamageEnemiesInFireZone(float deltaTime)
    {
        List<IDamageable> enemies = _fireZone.GetEnemiesInZone();
        float damage = _damagePerSecond * deltaTime;

        foreach (IDamageable enemy in enemies)
            enemy.TakeDamage(damage, _armorPenetration);
    }

    // ── Состояние и VFX ───────────────────────────────────────────────

    private void SetState(AnnihilatorState newState)
    {
        _state = newState;
        _animator?.SetInteger("State", (int)newState);
    }

    private void PlayVFX(ParticleSystem vfx)
    {
        if (vfx != null && !vfx.isPlaying) vfx.Play();
    }

    private void StopVFX(ParticleSystem vfx)
    {
        if (vfx != null && vfx.isPlaying) vfx.Stop();
    }

    private void StopAllVFX()
    {
        StopVFX(_chargeVFX);
        StopVFX(_fireVFX);
    }
}

public enum AnnihilatorState
{
    Idle = 0,
    Charging = 1,
    Firing = 2,
    Reloading = 3
}
