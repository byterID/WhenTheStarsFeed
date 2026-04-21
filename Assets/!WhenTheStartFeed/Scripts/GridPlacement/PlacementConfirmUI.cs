using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlacementConfirmUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("World Space Settings")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.5f, 0f); // высота над башней
    [SerializeField] private Camera uiCamera; // Main Camera

    [Header("Rotation (Annihilator only)")]
    [SerializeField] private Button rotateButton;  // кнопка поворота
    [SerializeField] private GameObject rotateButtonGO; // её GameObject

    private Action _onConfirm;
    private Action _onCancel;
    private Transform _targetGhost; // трансформ голограммы

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);
        panel.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        // Каждый кадр прикрепляем панель к голограмме
        if (panel.gameObject.activeSelf && _targetGhost != null)
        {
            FollowGhost();
        }
    }

    // ── Публичные методы ──────────────────────────────────────────────

    public void Show(Transform ghostTransform, Action onConfirm, Action onCancel,
                     bool isAnnihilator = false, Action onRotate = null)
    {
        _onConfirm = onConfirm;
        _onCancel = onCancel;
        _targetGhost = ghostTransform;

        // Показываем кнопку поворота только аннигилятору
        if (rotateButtonGO != null)
            rotateButtonGO.SetActive(isAnnihilator);

        if (isAnnihilator && onRotate != null)
        {
            rotateButton.onClick.RemoveAllListeners();
            rotateButton.onClick.AddListener(() => onRotate?.Invoke());
        }

        panel.gameObject.SetActive(true);
        FollowGhost();

        StopAllCoroutines();
        StartCoroutine(AnimateIn());
    }

    public void Hide()
    {
        StopAllCoroutines();
        panel.gameObject.SetActive(false);
        _targetGhost = null;
    }

    // ── Позиционирование ──────────────────────────────────────────────

    private void FollowGhost()
    {
        if (_targetGhost == null || uiCamera == null) return;

        // Мировая позиция над башней → экранная позиция
        Vector3 worldPos = _targetGhost.position + worldOffset;
        Vector3 screenPos = uiCamera.WorldToScreenPoint(worldPos);

        // Если башня за камерой — прячем
        if (screenPos.z < 0f)
        {
            panel.gameObject.SetActive(false);
            return;
        }

        panel.position = screenPos;
    }

    // ── Кнопки ────────────────────────────────────────────────────────

    private void OnConfirmClicked()
    {
        Hide();
        _onConfirm?.Invoke();
    }

    private void OnCancelClicked()
    {
        Hide();
        _onCancel?.Invoke();
    }

    // ── Анимация ──────────────────────────────────────────────────────

    private IEnumerator AnimateIn()
    {
        float t = 0f;
        float duration = 0.18f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            panel.localScale = Vector3.one * EaseOutBack(Mathf.Clamp01(t));
            yield return null;
        }
        panel.localScale = Vector3.one;
    }

    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
