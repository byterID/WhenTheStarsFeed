using UnityEngine;

/// <summary>
/// Движение врага по маршруту.
///
/// Порядок инициализации:
///   Awake  — кэшируем компоненты (GetComponent безопасен здесь).
///   Start  — ничего не делаем (маршрут задаётся через SetPath из EnemySpawner).
///   SetPath — вызывается из EnemySpawner сразу после Instantiate,
///             ДО первого Update, поэтому всё корректно.
///
/// Когда враг доходит до конца пути:
///   1. Движение останавливается (_isMoving = false).
///   2. Вызывается EnemyActions.StartAttackingBase() — тот запускает AttackLoop.
///   3. Враг остаётся на месте и уязвим к башням (TakeDamage работает).
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private float _speed = 3f;

    // Маршрут — назначается из EnemySpawner.SetPath()
    private Transform[] _moveTargets;
    private int _currentTargetIndex;
    private bool _isMoving;

    // ── Кэш компонентов ───────────────────────────────────────────────
    private EnemyActions _enemyActions;

    // ── Lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        // Awake — правильное место для GetComponent на том же GameObject.
        // Гарантированно выполняется до Start и до вызовов извне (SetPath).
        _enemyActions = GetComponent<EnemyActions>();
    }

    // ── Публичный API (вызывается из EnemySpawner) ────────────────────

    public void SetPath(Transform[] path)
    {
        _moveTargets = path;
        _currentTargetIndex = 0;
        _isMoving = true;
    }

    /// <summary>Внешняя остановка/возобновление движения (для будущих систем)</summary>
    public void SetMoving(bool moving)
    {
        _isMoving = moving;
    }

    // ── Движение ──────────────────────────────────────────────────────

    private void Update()
    {
        // Не двигаемся если: остановлены, маршрут не назначен,
        // или уже атакуем базу
        if (!_isMoving) return;
        if (_moveTargets == null || _moveTargets.Length == 0) return;
        if (_currentTargetIndex >= _moveTargets.Length) return;

        // Если EnemyActions уже переключился в режим атаки — останавливаемся
        if (_enemyActions != null && _enemyActions.IsAttackingBase)
        {
            _isMoving = false;
            return;
        }

        Transform target = _moveTargets[_currentTargetIndex];

        // Двигаемся к следующей точке маршрута
        float step = _speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target.position, step);

        // Достигли текущей точки — переходим к следующей
        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            _currentTargetIndex++;

            if (_currentTargetIndex >= _moveTargets.Length)
            {
                // Конец пути — начинаем атаку базы, останавливаем движение
                _isMoving = false;
                _enemyActions?.StartAttackingBase();
            }
        }
    }
}
