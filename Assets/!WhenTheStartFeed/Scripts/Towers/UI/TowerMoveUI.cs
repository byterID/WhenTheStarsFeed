using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton панель с кнопкой перемещения башни.
/// Canvas — Screen Space Overlay.
/// Висит над выбранной башней.
/// </summary>
public class TowerMoveUI : MonoBehaviour
{
    public static TowerMoveUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private Button moveButton;      // синяя стрелка →

    [Header("Settings")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.5f, 0f);
    [SerializeField] private Camera uiCamera;

    private TowerClickHandler _currentTower;

    private void Awake()
    {
        Instance = this;
        moveButton.onClick.AddListener(OnMoveClicked);
        panel.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        // Следуем за башней каждый кадр
        if (panel.gameObject.activeSelf && _currentTower != null)
            FollowTower();
    }

    // ── Публичные методы ──────────────────────────────────────────────

    public void Show(TowerClickHandler tower)
    {
        if (_currentTower == tower && panel.gameObject.activeSelf) return;

        _currentTower = tower;
        panel.gameObject.SetActive(true);
        FollowTower();

        StopAllCoroutines();
        StartCoroutine(AnimateIn());
    }

    public void Hide()
    {
        StopAllCoroutines();
        panel.gameObject.SetActive(false);
        _currentTower = null;
    }

    // ── Кнопка перемещения ────────────────────────────────────────────

    private void OnMoveClicked()
    {
        if (_currentTower == null) return;

        TowerClickHandler tower = _currentTower;
        Hide();

        // Передаём в PlacementSystem запрос на перемещение
        PlacementSystem.Instance.StartMoving(tower);
    }

    // ── Позиционирование ──────────────────────────────────────────────

    private void FollowTower()
    {
        if (_currentTower == null || uiCamera == null) return;

        Vector3 worldPos = _currentTower.transform.position + worldOffset;
        Vector3 screenPos = uiCamera.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0f)
        {
            panel.gameObject.SetActive(false);
            return;
        }

        panel.position = screenPos;
    }

    // ── Анимация ──────────────────────────────────────────────────────

    private IEnumerator AnimateIn()
    {
        float t = 0f;
        const float duration = 0.15f;
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
