using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Контроллер камеры. Работает на PC и Android/iOS без New Input System.
/// Использует только Legacy Input (Input.GetTouch, Input.mousePosition) —
/// он работает везде и не требует настройки Input Actions.
///
/// НАСТРОЙКА ИЕРАРХИИ:
///   CameraRig          ← этот скрипт здесь
///   └── CameraPivot    ← пустой объект, его Y = уровень зума
///       └── Main Camera
///
/// ПОЛЯ INSPECTOR:
///   Camera Pivot       → перетащи CameraPivot
///   Min Zoom           → минимальная высота камеры (ближе к земле)
///   Max Zoom           → максимальная высота камеры (дальше от земли)
///   Drag Speed         → скорость перетаскивания (0.02 - 0.05)
///   Mobile Pinch Speed → скорость зума пальцами (0.02 - 0.08)
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Ссылки")]
    [Tooltip("Объект CameraPivot — дочерний объект CameraRig, его Y двигается для зума")]
    [SerializeField] private Transform _cameraPivot;

    [Header("Зум")]
    [SerializeField] private float _minZoom = 5f;
    [SerializeField] private float _maxZoom = 40f;
    [SerializeField] private float _desktopScrollSpeed = 5f;
    [SerializeField] private float _mobilePinchSpeed   = 0.04f;

    [Header("Перемещение")]
    [SerializeField] private float _dragSpeed = 0.035f;
    [Tooltip("Инвертировать направление перетаскивания")]
    [SerializeField] private bool  _invertDrag = false;

    [Header("Сглаживание")]
    [SerializeField] private float _posLerp  = 12f;
    [SerializeField] private float _zoomLerp = 10f;

    [Header("Границы карты (опционально)")]
    [SerializeField] private bool _useBounds = false;
    [SerializeField] private Rect _bounds    = new Rect(-20f, -20f, 40f, 40f);

    // ── внутреннее состояние ──────────────────────────────────────────
    private Vector3 _targetPos;
    private float   _targetZoom;

    // drag одним пальцем / мышью
    private bool    _isDragging;
    private Vector2 _dragPrevScreen;
    private int     _dragFingerId = -1;

    // pinch двумя пальцами
    private bool  _isPinching;
    private float _pinchPrevDist;

    private bool _locked; // GameOver — ввод заморожен

    // ── lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        if (_cameraPivot == null && transform.childCount > 0)
            _cameraPivot = transform.GetChild(0);

        if (_cameraPivot == null)
            Debug.LogError("[CameraController] Не назначен CameraPivot!", this);
    }

    private void Start()
    {
        _targetPos  = transform.position;
        _targetZoom = _cameraPivot != null ? _cameraPivot.localPosition.y : (_minZoom + _maxZoom) * 0.5f;

        if (EndGameManager.Instance != null)
            EndGameManager.Instance.OnStateChanged += OnStateChanged;
    }

    private void OnDestroy()
    {
        if (EndGameManager.Instance != null)
            EndGameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    private void LateUpdate()
    {
        if (_locked) return;

        int touchCount = Input.touchCount;

        if (touchCount == 0)
        {
            // ── DESKTOP: мышь ─────────────────────────────────────────
            HandleMouseDrag();
            HandleMouseZoom();
        }
        else if (touchCount == 1)
        {
            // ── MOBILE: 1 палец = drag ────────────────────────────────
            _isPinching = false;
            HandleOneFinger();
        }
        else
        {
            // ── MOBILE: 2+ пальца = pinch-zoom ───────────────────────
            _isDragging   = false;
            _dragFingerId = -1;
            HandlePinch();
        }

        ApplyMovement();
    }

    // ═══════════════════ МЫШЬ (PC) ═══════════════════════════════════

    private void HandleMouseDrag()
    {
        // ПКМ или средняя кнопка — перетаскивание
        bool down = Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
        bool held = Input.GetMouseButton(1)     || Input.GetMouseButton(2);
        bool up   = Input.GetMouseButtonUp(1)   || Input.GetMouseButtonUp(2);

        if (down)
        {
            // Не двигаем камеру если нажали на UI
            if (IsScreenPosOverUI(Input.mousePosition)) return;
            _isDragging    = true;
            _dragPrevScreen = Input.mousePosition;
        }

        if (held && _isDragging)
        {
            Vector2 cur   = Input.mousePosition;
            Vector2 delta = cur - _dragPrevScreen;
            MoveByScreenDelta(delta);
            _dragPrevScreen = cur;
        }

        if (up)
            _isDragging = false;
    }

    private void HandleMouseZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.001f) return;

        _targetZoom -= scroll * _desktopScrollSpeed * 10f;
        _targetZoom  = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
    }

    // ═══════════════════ КАСАНИЯ (ANDROID/IOS) ════════════════════════

    private void HandleOneFinger()
    {
        Touch t = Input.GetTouch(0);

        switch (t.phase)
        {
            case TouchPhase.Began:
                // Игнорируем касание по UI-элементам
                if (IsScreenPosOverUI(t.position)) return;

                _isDragging    = true;
                _dragFingerId  = t.fingerId;
                _dragPrevScreen = t.position;
                break;

            case TouchPhase.Moved:
                if (!_isDragging || t.fingerId != _dragFingerId) return;

                MoveByScreenDelta(t.position - _dragPrevScreen);
                _dragPrevScreen = t.position;
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (t.fingerId == _dragFingerId)
                {
                    _isDragging   = false;
                    _dragFingerId = -1;
                }
                break;
        }
    }

    private void HandlePinch()
    {
        if (Input.touchCount < 2) return;

        Touch t0 = Input.GetTouch(0);
        Touch t1 = Input.GetTouch(1);

        float dist = Vector2.Distance(t0.position, t1.position);

        // При начале pinch — запоминаем расстояние, не двигаем
        if (!_isPinching || t1.phase == TouchPhase.Began)
        {
            _pinchPrevDist = dist;
            _isPinching    = true;
            return;
        }

        float delta = dist - _pinchPrevDist;
        _targetZoom    -= delta * _mobilePinchSpeed;
        _targetZoom     = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
        _pinchPrevDist  = dist;
    }

    // ═══════════════════ ПРИМЕНЕНИЕ ДВИЖЕНИЯ ═════════════════════════

    private void MoveByScreenDelta(Vector2 screenDelta)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 fwd   = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cam.transform.right,   Vector3.up).normalized;

        Vector3 move = (right * screenDelta.x + fwd * screenDelta.y) * _dragSpeed;
        if (_invertDrag) move = -move;

        _targetPos -= move;
    }

    private void ApplyMovement()
    {
        if (_useBounds)
        {
            _targetPos.x = Mathf.Clamp(_targetPos.x, _bounds.x, _bounds.x + _bounds.width);
            _targetPos.z = Mathf.Clamp(_targetPos.z, _bounds.y, _bounds.y + _bounds.height);
        }

        transform.position = Vector3.Lerp(
            transform.position, _targetPos, _posLerp * Time.unscaledDeltaTime);

        if (_cameraPivot != null)
        {
            Vector3 lp = _cameraPivot.localPosition;
            lp.y = Mathf.Lerp(lp.y, _targetZoom, _zoomLerp * Time.unscaledDeltaTime);
            _cameraPivot.localPosition = lp;
        }
    }

    // ═══════════════════ ПРОВЕРКА UI ═════════════════════════════════

    /// <summary>
    /// Возвращает true если точка screenPos попадает на любой UI-элемент.
    /// Использует EventSystem.RaycastAll — работает на Android и iOS
    /// без зависимости от New Input System или Simulate Mouse Cursor.
    /// </summary>
    private bool IsScreenPosOverUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        var pd = new PointerEventData(EventSystem.current) { position = screenPos };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pd, results);
        return results.Count > 0;
    }

    // ═══════════════════ GAME OVER ════════════════════════════════════

    private void OnStateChanged(GameState state)
    {
        _locked = (state == GameState.GameOver);
    }

    // ═══════════════════ GIZMOS ══════════════════════════════════════

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!_useBounds) return;
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        var c = new Vector3(_bounds.x + _bounds.width * 0.5f, 0, _bounds.y + _bounds.height * 0.5f);
        var s = new Vector3(_bounds.width, 0.1f, _bounds.height);
        Gizmos.DrawCube(c, s);
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.9f);
        Gizmos.DrawWireCube(c, s);
    }
#endif
}