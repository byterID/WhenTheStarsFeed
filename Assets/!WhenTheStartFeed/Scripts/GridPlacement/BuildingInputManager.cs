using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class BuildingInputManager : MonoBehaviour
{
    [SerializeField] private Camera _sceneCamera;
    [SerializeField] private LayerMask _placementLayermask;

    [Header("Drag Settings")]
    [SerializeField] private float movementThreshold = 0.02f;

    // ── События ───────────────────────────────────────────────────────
    public event Action OnExit;
    public event Action<Vector3> OnDragMove;   // палец/мышь двигается — новая мировая позиция
    public event Action OnDragReleased;        // отпустили кнопку/палец — показать панель

    // ── Состояние ─────────────────────────────────────────────────────
    private bool _isActive = false;
    private bool _isDragging = false;          // зажата ли кнопка прямо сейчас
    private Vector3 _currentWorldPos;          // последняя известная позиция голограммы

    // ── Активация ─────────────────────────────────────────────────────

    public void Activate(Vector3 startWorldPos)
    {
        _isActive = true;
        _isDragging = false;
        _currentWorldPos = startWorldPos;
    }

    public void Deactivate()
    {
        _isActive = false;
        _isDragging = false;
    }

    // ── Unity Update ──────────────────────────────────────────────────

    private void Update()
    {
        if (!_isActive) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnExit?.Invoke();
            return;
        }

        if (Application.isMobilePlatform)
            HandleTouchInput();
        else
            HandleMouseInput();
    }

    // ── Mouse Input ───────────────────────────────────────────────────

    private void HandleMouseInput()
    {
        // Нажали ЛКМ
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            Vector3 worldPos = ScreenToWorld(Input.mousePosition);
            if (worldPos == Vector3.zero) return;

            _isDragging = true;
            _currentWorldPos = worldPos;
            OnDragMove?.Invoke(worldPos); // сразу двигаем на новую позицию
        }

        // Держим ЛКМ — двигаем голограмму
        if (Input.GetMouseButton(0) && _isDragging)
        {
            Vector3 worldPos = ScreenToWorld(Input.mousePosition);
            if (worldPos == Vector3.zero) return;

            float delta = Vector3.Distance(worldPos, _currentWorldPos);
            if (delta > movementThreshold)
            {
                _currentWorldPos = worldPos;
                OnDragMove?.Invoke(worldPos);
            }
        }

        // Отпустили ЛКМ — показываем панель
        if (Input.GetMouseButtonUp(0) && _isDragging)
        {
            _isDragging = false;
            OnDragReleased?.Invoke();
        }
    }

    // ── Touch Input ───────────────────────────────────────────────────

    private void HandleTouchInput()
    {
        if (Touchscreen.current == null) return;

        TouchControl touch = Touchscreen.current.primaryTouch;

        // Начало касания
        if (touch.press.wasPressedThisFrame)
        {
#if UNITY_ANDROID || UNITY_IOS
            if (Input.touchCount > 0 &&
                EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                return;
#endif
            Vector3 worldPos = ScreenToWorld(touch.position.ReadValue());
            if (worldPos == Vector3.zero) return;

            _isDragging = true;
            _currentWorldPos = worldPos;
            OnDragMove?.Invoke(worldPos);
        }

        // Движение пальца
        if (touch.press.isPressed && _isDragging)
        {
            Vector3 worldPos = ScreenToWorld(touch.position.ReadValue());
            if (worldPos == Vector3.zero) return;

            float delta = Vector3.Distance(worldPos, _currentWorldPos);
            if (delta > movementThreshold)
            {
                _currentWorldPos = worldPos;
                OnDragMove?.Invoke(worldPos);
            }
        }

        // Отпустили палец — показываем панель
        if (touch.press.wasReleasedThisFrame && _isDragging)
        {
            _isDragging = false;
            OnDragReleased?.Invoke();
        }
    }

    // ── Публичные утилиты ─────────────────────────────────────────────

    public Vector3 GetSelectedMapPosition() => _currentWorldPos;

    public bool IsPointerOverUI()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
#endif
        return EventSystem.current.IsPointerOverGameObject();
    }

    // ── Утилиты ───────────────────────────────────────────────────────

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        Ray ray = _sceneCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, _placementLayermask))
            return hit.point;
        return Vector3.zero;
    }
}
