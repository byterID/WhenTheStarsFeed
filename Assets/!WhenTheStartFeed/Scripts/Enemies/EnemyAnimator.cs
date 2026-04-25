using UnityEngine;

/// <summary>
/// Управляет анимациями врага и поворотом к следующей точке пути.
///
/// Намеренно вынесен в отдельный компонент — не затрагивает логику
/// EnemyMovement и EnemyActions, только читает их состояние.
/// </summary>
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyActions))]
public class EnemyAnimator : MonoBehaviour
{
    // ── Хэши параметров Animator ──────────────────────────────────────
    private static readonly int ParamSpeed  = Animator.StringToHash("Speed");
    private static readonly int ParamAttack = Animator.StringToHash("Attack");

    [Header("Компоненты")]
    [Tooltip("Animator модели врага. Если не назначен — ищется GetComponentInChildren автоматически.")]
    [SerializeField] private Animator _animator;

    [Header("Поворот")]
    [Tooltip("Скорость поворота к следующей точке пути (градусов/сек). 0 = мгновенно.")]
    [SerializeField] private float _rotationSpeed = 360f;

    [Tooltip("Transform главной базы для поворота при атаке. " +
             "Если пусто — находится через ServiceLocator автоматически.")]
    [SerializeField] private Transform _baseTransform;

    [Header("Настройки анимации")]
    [Tooltip("Порог скорости перемещения для запуска анимации Run (м/кадр). " +
             "Компенсирует кадр когда IsMoving уже false но враг ещё не начал атаку.")]
    [SerializeField] private float _moveThreshold = 0.01f;

    // ── Кэш ───────────────────────────────────────────────────────────
    private EnemyMovement _movement;
    private EnemyActions  _actions;

    // ── Состояние ─────────────────────────────────────────────────────
    private bool _wasAttacking;
    private Vector3 _prevPosition;

    // ── Lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        _movement = GetComponent<EnemyMovement>();
        _actions  = GetComponent<EnemyActions>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
            Debug.LogWarning("[EnemyAnimator] Animator не найден!", this);

        if (_baseTransform == null)
        {
            MainBase mainBase = ServiceLocator.TryGet<MainBase>();
            if (mainBase != null)
                _baseTransform = mainBase.transform;
        }
    }

    private void Start()
    {
        _prevPosition = transform.position;
    }

    private void Update()
    {
        if (_animator == null) return;

        UpdateRotation();
        UpdateAnimations();

        _prevPosition = transform.position;
    }

    // ── Поворот к цели ────────────────────────────────────────────────

    private void UpdateRotation()
    {
        bool isAttacking = _actions != null && _actions.IsAttackingBase;

        Vector3 lookDir;

        if (isAttacking)
        {
            // При атаке — смотрим на базу
            if (_baseTransform == null) return;
            lookDir = _baseTransform.position - transform.position;
        }
        else
        {
            // При движении — смотрим в направлении следующей точки
            lookDir = _movement != null ? _movement.CurrentMoveDirection : Vector3.zero;
        }

        lookDir.y = 0f;
        if (lookDir.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDir);

        if (_rotationSpeed <= 0f)
            transform.rotation = targetRotation;
        else
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _rotationSpeed * Time.deltaTime);
    }

    // ── Анимации ──────────────────────────────────────────────────────

    private void UpdateAnimations()
    {
        bool isAttacking = _actions != null && _actions.IsAttackingBase;

        // Attack — устанавливаем только при изменении состояния
        if (isAttacking != _wasAttacking)
        {
            _animator.SetBool(ParamAttack, isAttacking);
            _wasAttacking = isAttacking;
        }

        if (isAttacking)
        {
            // Атакуем — Run не нужен
            _animator.SetFloat(ParamSpeed, 0f);
            return;
        }

        // Определяем движение по РЕАЛЬНОМУ смещению позиции за кадр.
        // Это надёжнее флага IsMoving: работает даже если враг физически
        // толкается другими врагами и IsMoving = false, но он ещё движется.
        float movedDistance = Vector3.Distance(transform.position, _prevPosition);
        float speed = movedDistance > _moveThreshold ? 1f : 0f;
        _animator.SetFloat(ParamSpeed, speed);
    }
}
