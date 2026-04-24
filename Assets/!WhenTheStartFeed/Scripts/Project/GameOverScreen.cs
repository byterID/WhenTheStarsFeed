using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// UI-экран «Игра окончена».
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    [Header("Ссылки на UI элементы")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _waveReachedText;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _menuButton;

    [Header("Настройки")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";
    [SerializeField] private float _showDelay = 1f;
    [SerializeField] private float _fadeInDuration = 0.4f;

    // ── Lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

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
            _waveReachedText.text = $"Max wave: {waveReached}";

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
