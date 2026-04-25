using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Менеджер ввода для размещения башен.
/// Использует только Legacy Input — работает на PC и Android/iOS.
/// </summary>
public class BuildingInputManager : MonoBehaviour
{
    [SerializeField] private Camera _sceneCamera;
    [SerializeField] private LayerMask _placementLayermask;

    [Header("Drag Settings")]
    [Tooltip("Минимальное смещение в мировых единицах для обновления позиции голограммы")]
    [SerializeField] private float movementThreshold = 0.02f;

    // ── События ───────────────────────────────────────────────────────
    public event Action          OnExit;
    public event Action<Vector3> OnDragMove;     // позиция обновилась
    public event Action          OnDragReleased; // палец/мышь отпущены — показать панель

    // ── Состояние ─────────────────────────────────────────────────────
    private bool    _isActive;
    private bool    _isDragging;
    private int     _dragFingerId = -1;   // ID пальца который ведёт голограмму
    private bool    _startedOnUI;         // касание началось на UI — полностью игнорируем
    private Vector3 _currentWorldPos;

    // ── Активация/деактивация ─────────────────────────────────────────

    public void Activate(Vector3 startWorldPos)
    {
        _isActive       = true;
        _isDragging     = false;
        _dragFingerId   = -1;
        _startedOnUI    = false;
        _currentWorldPos = startWorldPos;
    }

    public void Deactivate()
    {
        _isActive     = false;
        _isDragging   = false;
        _dragFingerId = -1;
        _startedOnUI  = false;
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

        if (Input.touchCount > 0)
            HandleTouchInput();
        else
            HandleMouseInput();
    }

    // ── PC: мышь ─────────────────────────────────────────────────────

    private void HandleMouseInput()
    {
        // Нажали ЛКМ
        if (Input.GetMouseButtonDown(0))
        {
            // Если нажали на UI (кнопки панели) — не двигаем голограмму
            if (IsScreenPosOverUI(Input.mousePosition)) return;

            Vector3 wp = ScreenToWorld(Input.mousePosition);
            if (wp == Vector3.zero) return;

            _isDragging      = true;
            _currentWorldPos = wp;
            OnDragMove?.Invoke(wp);
        }

        // Держим ЛКМ — двигаем голограмму
        if (_isDragging && Input.GetMouseButton(0))
        {
            Vector3 wp = ScreenToWorld(Input.mousePosition);
            if (wp == Vector3.zero) return;

            if (Vector3.Distance(wp, _currentWorldPos) > movementThreshold)
            {
                _currentWorldPos = wp;
                OnDragMove?.Invoke(wp);
            }
        }

        // Отпустили ЛКМ — показываем панель подтверждения
        if (_isDragging && Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
            OnDragReleased?.Invoke();
        }
    }

    // ── ANDROID/iOS: касания ──────────────────────────────────────────

    private void HandleTouchInput()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);

            // Если уже есть «наш» палец — обрабатываем только его
            if (_dragFingerId != -1 && t.fingerId != _dragFingerId)
                continue;

            switch (t.phase)
            {
                // ── Начало касания ────────────────────────────────────
                case TouchPhase.Began:
                {
                    bool overUI = IsScreenPosOverUI(t.position);

                    // Запоминаем этот палец в любом случае
                    _dragFingerId = t.fingerId;
                    _startedOnUI  = overUI;

                    if (overUI)
                    {
                        // Касание по кнопке — НЕ двигаем голограмму,
                        // ждём Ended чтобы сбросить _dragFingerId
                        break;
                    }

                    Vector3 wp = ScreenToWorld(t.position);
                    if (wp == Vector3.zero)
                    {
                        _startedOnUI = true; // обрабатываем как UI-касание (ничего не делаем)
                        break;
                    }

                    _isDragging      = true;
                    _currentWorldPos = wp;
                    OnDragMove?.Invoke(wp);
                    break;
                }

                // ── Движение пальца ───────────────────────────────────
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                {
                    // Если старт был на UI — игнорируем движение
                    if (_startedOnUI || !_isDragging) break;

                    Vector3 wp = ScreenToWorld(t.position);
                    if (wp == Vector3.zero) break;

                    if (Vector3.Distance(wp, _currentWorldPos) > movementThreshold)
                    {
                        _currentWorldPos = wp;
                        OnDragMove?.Invoke(wp);
                    }
                    break;
                }

                // ── Отпустили палец ───────────────────────────────────
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                {
                    bool wasReal = _isDragging && !_startedOnUI;

                    // Сброс
                    _isDragging   = false;
                    _dragFingerId = -1;
                    _startedOnUI  = false;

                    // Показываем панель только если это было реальное перемещение,
                    // а не касание по кнопке
                    if (wasReal)
                        OnDragReleased?.Invoke();

                    break;
                }
            }

            // Обработали «наш» палец — выходим из цикла
            if (_dragFingerId == -1 || t.fingerId == _dragFingerId)
                break;
        }
    }

    // ── Публичный метод для PlacementSystem ──────────────────────────

    public Vector3 GetSelectedMapPosition() => _currentWorldPos;

    /// <summary>
    /// Используется PlacementSystem для дополнительных проверок.
    /// </summary>
    public bool IsPointerOverUI()
    {
        if (Input.touchCount > 0)
            return IsScreenPosOverUI(Input.GetTouch(0).position);

        return IsScreenPosOverUI(Input.mousePosition);
    }

    // ── Утилиты ───────────────────────────────────────────────────────

    /// <summary>
    /// EventSystem.RaycastAll — единственный надёжный способ проверить UI
    /// на Android без зависимости от New Input System.
    /// </summary>
    private bool IsScreenPosOverUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        var pd = new PointerEventData(EventSystem.current) { position = screenPos };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pd, results);
        return results.Count > 0;
    }

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        if (_sceneCamera == null) return Vector3.zero;
        Ray ray = _sceneCamera.ScreenPointToRay(screenPos);
        return Physics.Raycast(ray, out RaycastHit hit, 100f, _placementLayermask)
            ? hit.point
            : Vector3.zero;
    }
}