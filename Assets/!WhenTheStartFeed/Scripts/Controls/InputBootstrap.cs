using UnityEngine;

/// <summary>
/// Упрощённый InputBootstrap.
///
/// Раньше здесь была логика «выбрать desktop или mobile контролы»,
/// но CameraController теперь сам определяет тип ввода по Input.touchCount.
/// Этот скрипт остался только для блокировки ввода при GameOver.
///
/// НАСТРОЙКА:
///   Оставьте этот компонент на сцене (если он уже есть).
///   Поля desktopControls и mobileControls больше не нужны —
///   можно убрать их из Inspector или оставить пустыми.
/// </summary>
public class InputBootstrap : MonoBehaviour
{
    // Поля оставлены для обратной совместимости (чтобы не сломать Inspector)
    // но логика выбора desktop/mobile удалена — она была источником проблем
    [SerializeField] private GameObject desktopControls;
    [SerializeField] private GameObject mobileControls;

    private void Awake()
    {
        // Активируем оба объекта (если назначены) — CameraController сам разберётся
        if (desktopControls != null) desktopControls.SetActive(true);
        if (mobileControls  != null) mobileControls.SetActive(true);

        Debug.Log($"[InputBootstrap] Platform: {Application.platform}, " +
                  $"isMobile: {Application.isMobilePlatform}, " +
                  $"TouchSupport: {Input.touchSupported}");
    }

    private void Start()
    {
        if (EndGameManager.Instance != null)
            EndGameManager.Instance.OnStateChanged += OnGameStateChanged;
    }

    private void OnDestroy()
    {
        if (EndGameManager.Instance != null)
            EndGameManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state != GameState.GameOver) return;

        if (desktopControls != null) desktopControls.SetActive(false);
        if (mobileControls  != null) mobileControls.SetActive(false);
    }
}