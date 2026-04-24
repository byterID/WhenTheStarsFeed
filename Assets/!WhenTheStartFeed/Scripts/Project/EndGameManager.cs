using UnityEngine;

/// <summary>
/// Центральный менеджер состояния игры.
/// Подписывается на MainBase.OnBaseDestroyed и показывает GameOverScreen.
///
/// Настройка на сцене:
///   1. Создайте пустой GameObject «GameManager» на сцене.
///   2. Добавьте этот компонент.
///   3. Назначьте ссылки gameOverScreen и mainBase в Inspector
///      (или оставьте пустыми — они подхватятся автоматически из ServiceLocator).
///   4. Script Execution Order: -60 (регистрируется до большинства потребителей).
///
/// Состояния игры (GameState):
///   Playing  — нормальный игровой процесс
///   GameOver — база уничтожена, игра окончена
/// </summary>
public class EndGameManager : MonoBehaviour
{
    public static EndGameManager Instance { get; private set; }

    [Header("Зависимости")]
    [SerializeField] private GameOverScreen _gameOverScreen;  // можно не назначать — найдёт сам
    [SerializeField] private MainBase _mainBase;              // можно не назначать — берёт из ServiceLocator

    // ── Состояние ─────────────────────────────────────────────────────
    public GameState CurrentState { get; private set; } = GameState.Playing;

    // ── Событие смены состояния (UI могут подписаться) ───────────────
    public event System.Action<GameState> OnStateChanged;

    // ── Lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        // Синглтон
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Регистрируем в ServiceLocator (на случай если кто-то захочет GetManager)
        ServiceLocator.Register<EndGameManager>(this);
    }

    private void Start()
    {
        // Получаем MainBase — либо из Inspector, либо из ServiceLocator.
        // Start выполняется ПОСЛЕ всех Awake, поэтому MainBase уже зарегистрирована.
        if (_mainBase == null)
            _mainBase = ServiceLocator.TryGet<MainBase>();

        if (_mainBase == null)
        {
            Debug.LogError("[GameManager] MainBase не найдена! " +
                           "Добавьте компонент MainBase на главное здание на сцене.");
            return;
        }

        _mainBase.OnBaseDestroyed += OnBaseDestroyed;

        if (_gameOverScreen == null)
            Debug.LogWarning("[GameManager] GameOverScreen не найден на сцене! " +
                             "Создайте объект с компонентом GameOverScreen.");
    }

    private void OnDestroy()
    {
        if (_mainBase != null)
            _mainBase.OnBaseDestroyed -= OnBaseDestroyed;

        ServiceLocator.Unregister<EndGameManager>();

        if (Instance == this)
            Instance = null;
    }

    // ── Реакция на гибель базы ────────────────────────────────────────

    private void OnBaseDestroyed()
    {
        if (CurrentState == GameState.GameOver) return;

        SetState(GameState.GameOver);

        // Узнаём до какой волны дошли
        int waveReached = Waves.Instance != null
            ? Waves.Instance.CurrentWaveNumber
            : 0;

        _gameOverScreen?.Show(waveReached);
    }

    // ── Управление состоянием ─────────────────────────────────────────

    private void SetState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    // ── Публичный метод для проверки (башни, UI и т.д.) ──────────────
    public bool IsPlaying => CurrentState == GameState.Playing;
}

public enum GameState
{
    Playing,
    GameOver
}
