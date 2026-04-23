using UnityEngine;

/// <summary>
/// Движение врага по маршруту.
///
/// Порядок инициализации (из EnemySpawner):
///   1. Instantiate(prefab)  → Awake отрабатывает автоматически
///   2. InitFromData(speed)  → передаём скорость из EnemiesDatabaseSO
///   3. SetPath(waypoints)   → задаём маршрут и запускаем движение
///
/// Баг «очередь у базы»:
///   Враги с Rigidbody/Collider блокировали друг друга у последней точки.
///   Задние не могли подойти на 0.1f → StartAttackingBase не вызывался.
///   Решение: последняя точка использует увеличенный порог _baseArrivalRadius,
///   плюс OnTriggerEnter на коллайдере базы дублирует вызов StartAttackingBase.
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float _speed = 3f;

    [Tooltip("Радиус в котором промежуточные точки считаются достигнутыми (м).")]
    [SerializeField] private float _waypointRadius = 0.1f;

    [Tooltip("Радиус в котором последняя точка (база) считается достигнутой (м). " +
             "Должен быть достаточно большим чтобы несколько врагов могли атаковать одновременно.")]
    [SerializeField] private float _baseArrivalRadius = 1.5f;

    private Transform[] _moveTargets;
    private int _currentTargetIndex;
    private bool _isMoving;

    // ── Направление движения — читается EnemyAnimator ─────────────────
    private Vector3 _currentMoveDirection = Vector3.forward;

    /// <summary>Нормализованное направление к текущей цели. Vector3.zero если стоим.</summary>
    public Vector3 CurrentMoveDirection => _isMoving ? _currentMoveDirection : Vector3.zero;

    /// <summary>Враг сейчас движется по маршруту.</summary>
    public bool IsMoving => _isMoving;

    // ── Кэш ───────────────────────────────────────────────────────────
    private EnemyActions _enemyActions;

    private void Awake()
    {
        _enemyActions = GetComponent<EnemyActions>();
    }

    // ── Публичный API ─────────────────────────────────────────────────

    /// <summary>
    /// Вызывается из EnemySpawner после Instantiate.
    /// Устанавливает скорость из EnemiesDatabaseSO.moveSpeed.
    /// </summary>
    public void InitFromData(float speed)
    {
        if (speed > 0f)
            _speed = speed;
    }

    /// <summary>
    /// Задаёт маршрут и немедленно начинает движение.
    /// </summary>
    public void SetPath(Transform[] path)
    {
        _moveTargets = path;
        _currentTargetIndex = 0;
        _isMoving = true;

        if (path != null && path.Length > 0)
            UpdateMoveDirection(path[0].position);
    }

    /// <summary>Пауза / возобновление движения.</summary>
    public void SetMoving(bool moving)
    {
        _isMoving = moving;
    }

    // ── OnTriggerEnter — резервный вызов атаки ────────────────────────
    // Если враг оказался внутри триггера базы (например, его толкнули другие
    // враги или он пришёл сбоку) — StartAttackingBase вызывается здесь.
    // Требует: на объекте MainBase должен быть Collider с Is Trigger = true
    // и тег "Base" (или используй Layer "Base" и проверяй layer).
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Base"))
            ArriveAtBase();
    }

    // ── Движение ──────────────────────────────────────────────────────

    private void Update()
    {
        if (!_isMoving) return;
        if (_moveTargets == null || _moveTargets.Length == 0) return;
        if (_currentTargetIndex >= _moveTargets.Length) return;

        if (_enemyActions != null && _enemyActions.IsAttackingBase)
        {
            _isMoving = false;
            return;
        }

        Transform target = _moveTargets[_currentTargetIndex];
        UpdateMoveDirection(target.position);

        float step = _speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target.position, step);

        bool isLastWaypoint = (_currentTargetIndex == _moveTargets.Length - 1);
        float radius = isLastWaypoint ? _baseArrivalRadius : _waypointRadius;

        if (Vector3.Distance(transform.position, target.position) < radius)
        {
            _currentTargetIndex++;

            if (_currentTargetIndex >= _moveTargets.Length)
            {
                ArriveAtBase();
            }
            else
            {
                UpdateMoveDirection(_moveTargets[_currentTargetIndex].position);
            }
        }
    }

    // ── Прибытие к базе ───────────────────────────────────────────────

    private void ArriveAtBase()
    {
        if (_enemyActions != null && _enemyActions.IsAttackingBase) return;
        _isMoving = false;
        _enemyActions?.StartAttackingBase();
    }

    private void UpdateMoveDirection(Vector3 targetPosition)
    {
        Vector3 dir = targetPosition - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            _currentMoveDirection = dir.normalized;
    }
}
