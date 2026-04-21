using System.Collections;
using UnityEngine;

public class Waves : MonoBehaviour
{
    public static Waves Instance { get; private set; }

    [Header("База волн")]
    [SerializeField] private WavesDatabaseSO _wavesDatabase;

    [Header("Спавнер")]
    [SerializeField] private EnemySpawner _spawner;

    [Header("Масштабирование статов за каждый цикл волн")]
    [SerializeField] private float _statScalePerCycle = 0.2f; // +20% за цикл

    // ── Состояние ─────────────────────────────────────────────────────
    private int _currentWaveIndex = 0;   // индекс в базе (0..N-1)
    private int _cycleCount = 0;         // сколько раз прошли все волны
    private bool _waveRunning = false;
    private Coroutine _waveCoroutine;

    // ── События для UI ────────────────────────────────────────────────
    public event System.Action<int> OnWaveStarted;       // номер волны (1-based)
    public event System.Action<float> OnCountdownTick;   // секунды до следующей волны

    // ── Lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Автостарт первой волны
        StartNextWave();
    }

    // ── Публичный запуск ──────────────────────────────────────────────

    public void StartNextWave()
    {
        if (_waveRunning) return;
        if (_waveCoroutine != null) StopCoroutine(_waveCoroutine);
        _waveCoroutine = StartCoroutine(RunWaveWithCountdown());
    }

    // ── Главный цикл ──────────────────────────────────────────────────

    private IEnumerator RunWaveWithCountdown()
    {
        _waveRunning = true;

        WaveData wave = _wavesDatabase.waveDatabase[_currentWaveIndex];

        // ── Подготовка: обратный отсчёт ───────────────────────────────
        float timer = wave.preparationTime;
        while (timer > 0f)
        {
            OnCountdownTick?.Invoke(timer);
            yield return new WaitForSeconds(1f);
            timer -= 1f;
        }
        OnCountdownTick?.Invoke(0f);

        // ── Номер волны для UI (глобальный, не сбрасывается) ──────────
        int globalWaveNumber = _cycleCount * _wavesDatabase.waveDatabase.Count
                               + _currentWaveIndex + 1;
        OnWaveStarted?.Invoke(globalWaveNumber);

        // ── Множитель статов ──────────────────────────────────────────
        float scaleMult = 1f + _cycleCount * _statScalePerCycle;

        // ── Спавн отрядов по порядку ──────────────────────────────────
        // Запускаем спавн в фоне, параллельно начинаем отсчёт до следующей волны
        Coroutine spawnCoroutine = StartCoroutine(SpawnAllSquads(wave, scaleMult));

        // ── Отсчёт до следующей волны (начинается сразу при старте волны) ──
        yield return new WaitForSeconds(wave.timeToNextWave);

        // ── Переходим к следующей волне ───────────────────────────────
        _currentWaveIndex++;
        if (_currentWaveIndex >= _wavesDatabase.waveDatabase.Count)
        {
            _currentWaveIndex = 0;
            _cycleCount++;
            Debug.Log($"Цикл волн завершён. Начинается цикл {_cycleCount + 1}, " +
                      $"множитель статов: {1f + _cycleCount * _statScalePerCycle:F2}x");
        }

        _waveRunning = false;

        // Автоматически запускаем следующую волну
        StartNextWave();
    }

    private IEnumerator SpawnAllSquads(WaveData wave, float scaleMult)
    {
        foreach (EnemySquad squad in wave.squads)
        {
            // Спавним отряд и ждём пока все юниты не появятся
            yield return StartCoroutine(_spawner.SpawnSquad(squad, scaleMult));
        }
    }

    // ── Публичное свойство для UI ─────────────────────────────────────
    public int CurrentWaveNumber =>
        _cycleCount * _wavesDatabase.waveDatabase.Count + _currentWaveIndex + 1;
}
