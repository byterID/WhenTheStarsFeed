using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// UI-экран «Игра окончена».
///
/// ═══════════════════════════════════════════════════════════════════
/// ИСПРАВЛЕННЫЙ БАГ: "Can't start coroutine on inactive object"
/// ═══════════════════════════════════════════════════════════════════
/// Проблема: gameOverPanel.SetActive(false) в Awake делает объект
/// неактивным. Если GameOverScreen находится НА ЭТОЙ ЖЕ панели,
/// то и сам компонент деактивируется → StartCoroutine падает.
///
/// Решение: GameOverScreen НЕ деактивирует сам себя.
/// Вместо этого панель скрывается через CanvasGroup.alpha = 0
/// и CanvasGroup.blocksRaycasts = false.
/// Объект остаётся активным, корутина работает нормально.
///
/// Настройка на сцене (ВАЖНО читать):
///   1. Создайте Canvas.
///   2. Добавьте дочерний Panel — назовите "GameOverPanel".
///      На этот Panel добавьте компонент CanvasGroup.
///   3. На "GameOverPanel" добавьте этот компонент GameOverScreen.
///   4. Внутри Panel создайте:
///      - TextMeshProUGUI "TitleText"    → "ИГРА ОКОНЧЕНА"
///      - TextMeshProUGUI "WaveText"     → "Вы дошли до волны: N"
///      - Button "RestartButton"         → "Начать заново"
///      - Button "MenuButton"            → "В меню"
///   5. Назначьте все ссылки в Inspector.
///   6. CanvasGroup автоматически найдётся если стоит на том же объекте.
///   7. GameOverPanel должен быть АКТИВНЫМ в иерархии (не SetActive(false)!),
///      просто невидимым через alpha=0.
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    [Header("Ссылки на UI элементы")]
    [SerializeField] private CanvasGroup _canvasGroup;           // скрываем через alpha, не SetActive!
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _waveReachedText;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _menuButton;

    [Header("Настройки")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";
    [SerializeField] private float _showDelay = 1f;              // задержка перед появлением (сек)
    [SerializeField] private float _fadeInDuration = 0.4f;       // длительность плавного появления

    // ── Lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        // Авто-поиск CanvasGroup на том же объекте
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        // Создаём CanvasGroup если нет — не ломаем проект
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            Debug.Log("[GameOverScreen] CanvasGroup добавлен автоматически. " +
                      "Рекомендуется добавить его вручную в Inspector.");
        }

        // Скрываем через alpha — объект ОСТАЁТСЯ АКТИВНЫМ
        SetVisible(false);

        if (_restartButton != null)
            _restartButton.onClick.AddListener(OnRestartClicked);

        if (_menuButton != null)
            _menuButton.onClick.AddListener(OnMenuClicked);
    }

    // ── Публичный API (вызывается GameManager) ────────────────────────

    public void Show(int waveReached)
    {
        // gameObject активен → StartCoroutine работает без ошибок
        StartCoroutine(ShowRoutine(waveReached));
    }

    // ── Корутина показа ───────────────────────────────────────────────

    private IEnumerator ShowRoutine(int waveReached)
    {
        // Ждём задержку (реальное время — не зависит от timeScale)
        yield return new WaitForSecondsRealtime(_showDelay);

        // Обновляем текст
        if (_waveReachedText != null)
            _waveReachedText.text = $"Вы дошли до волны: {waveReached}";

        // Плавно появляемся (fadeIn)
        yield return StartCoroutine(FadeIn());

        // Останавливаем время — ПОСЛЕ fade чтобы анимация отыграла
        Time.timeScale = 0f;
    }

    private IEnumerator FadeIn()
    {
        // Делаем видимым (блокируем raycast только здесь)
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;

        float elapsed = 0f;
        while (elapsed < _fadeInDuration)
        {
            // WaitForSecondsRealtime уже отработал выше, здесь timeScale ещё = 1
            // Поэтому используем unscaledDeltaTime на случай если вызовут при timeScale != 1
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(elapsed / _fadeInDuration);
            yield return null;
        }

        _canvasGroup.alpha = 1f;
    }

    // ── Кнопки ────────────────────────────────────────────────────────

    private void OnRestartClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnMenuClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(_mainMenuSceneName);
    }

    // ── Скрытие ───────────────────────────────────────────────────────

    public void Hide()
    {
        StopAllCoroutines();
        SetVisible(false);
        Time.timeScale = 1f;
    }

    private void SetVisible(bool visible)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.blocksRaycasts = visible;
        _canvasGroup.interactable = visible;
    }
}
