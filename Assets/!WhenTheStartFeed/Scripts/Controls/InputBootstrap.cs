using UnityEngine;

/// <summary>
/// Активирует нужный набор контролов (Desktop / Mobile).
///
/// Изменения:
///   - Зависимость от DeviceDetector решена через Script Execution Order:
///     DeviceDetector (-100) → InputBootstrap (-95) → остальные.
///   - При GameOver блокирует управление камерой и размещением.
///
/// Настройка на сцене:
///   Создайте два дочерних GameObject:
///     "DesktopControls" — содержит CameraController (desktop-режим)
///     "MobileControls"  — содержит CameraController (mobile-режим)
///   Назначьте их в Inspector этого компонента.
///
///   !!! ВАЖНО: оба объекта должны быть АКТИВНЫМИ в иерархии изначально,
///   InputBootstrap сам скроет ненужный в Awake.
/// </summary>
public class InputBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject desktopControls;
    [SerializeField] private GameObject mobileControls;

    private void Awake()
    {
        bool isMobile = DeviceDetector.CurrentDevice == DeviceType.Mobile;

        if (desktopControls != null) desktopControls.SetActive(!isMobile);
        if (mobileControls != null) mobileControls.SetActive(isMobile);

        Debug.Log($"[InputBootstrap] Активированы: {(isMobile ? "Mobile" : "Desktop")} контролы");
    }

    private void Start()
    {
        // Подписываемся на GameOver — блокируем управление
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
        if (state == GameState.GameOver)
        {
            // Отключаем все контролы — игрок ничего не может делать пока показан Game Over
            if (desktopControls != null) desktopControls.SetActive(false);
            if (mobileControls != null) mobileControls.SetActive(false);
        }
    }
}
