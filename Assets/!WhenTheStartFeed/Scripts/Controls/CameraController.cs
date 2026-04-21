using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
/// <summary>
/// Единый контроллер камеры для Tower Defence.
/// Заменяет четыре старых скрипта:
///   CameraDragDesktop, CameraZoomDesktop, CameraDragMobile, CameraZoomMobile.
///
/// ═══════════════════════ ФУНКЦИОНАЛ ════════════════════════════════
///   Desktop:
///     • ПКМ / средняя кнопка — перетаскивание карты
///     • Колесо мыши — зум (высота камеры по Y)
///     • WASD / стрелки — перемещение клавиатурой
///     • Края экрана — прокрутка при наведении (опционально)
///   Mobile:
///     • 1 палец — перетаскивание карты
///     • 2 пальца — pinch-зум
///   Оба:
///     • Границы карты — камера не выходит за пределы
///     • Плавное следование (Lerp) — мягкое движение
///     • Заморозка при GameOver
///
/// ═══════════════════════ НАСТРОЙКА ══════════════════════════════════
///   Иерархия на сцене:
///     CameraRig (пустой объект, горизонтальное перемещение)
///     └── CameraPivot (пустой объект, высота = зум)
///         └── Main Camera (смотрит вниз под углом)
///
///   Компоненты:
///     • CameraController ставится на CameraRig.
///     • _cameraRig → сам CameraRig (transform этого объекта, можно не назначать)
///     • _cameraTransform → CameraPivot (вложенный объект, двигается по Y для зума)
///
///   Параметры в Inspector:
///     • Map Bounds — задайте Rect с границами карты (x,y — мин XZ, width/height — размер)
///     • Use Map Bounds — включить/выключить ограничение
/// </summary>
public class CameraController : MonoBehaviour
{
    // ── Ссылки ────────────────────────────────────────────────────────
    [Header("Ссылки (оставьте пустыми если скрипт на CameraRig)")]
    [SerializeField] private Transform _cameraRig;        // корневой объект (перемещение XZ)
    [SerializeField] private Transform _cameraTransform;  // внутренний объект (высота Y = зум)

    // ── Drag ──────────────────────────────────────────────────────────
    [Header("Перетаскивание")]
    [SerializeField] private float _dragSpeed = 0.025f;
    [SerializeField] private bool _invertDrag = false;
    [SerializeField] private MouseButton _desktopDragButton = MouseButton.Right; // ПКМ по умолчанию

    // ── WASD ──────────────────────────────────────────────────────────
    [Header("Клавиатура (Desktop)")]
    [SerializeField] private float _keyboardSpeed = 15f;
    [SerializeField] private bool _useKeyboard = true;

    // ── Edge scroll ───────────────────────────────────────────────────
    [Header("Прокрутка краями экрана (Desktop)")]
    [SerializeField] private bool _useEdgeScroll = false;
    [SerializeField] private float _edgeScrollSpeed = 20f;
    [SerializeField][Range(1f, 60f)] private float _edgeScrollZone = 20f; // пикселей от края

    // ── Зум ───────────────────────────────────────────────────────────
    [Header("Зум")]
    [SerializeField] private float _zoomSpeed = 8f;
    [SerializeField] private float _minZoom = 5f;
    [SerializeField] private float _maxZoom = 40f;
    [SerializeField] private float _mobileZoomSpeed = 0.05f;

    // ── Плавность ─────────────────────────────────────────────────────
    [Header("Сглаживание движения")]
    [SerializeField] private float _moveLerpSpeed = 12f;   // скорость следования позиции
    [SerializeField] private float _zoomLerpSpeed = 10f;   // скорость следования зума

    // ── Границы карты ─────────────────────────────────────────────────
    [Header("Границы карты")]
    [SerializeField] private bool _useMapBounds = false;
    [SerializeField] private Rect _mapBounds = new Rect(-20f, -20f, 40f, 40f);
    // X = минимальный X мировых координат, Y = минимальный Z
    // Width = ширина по X, Height = длина по Z

    // ── Внутреннее состояние ──────────────────────────────────────────
    private Vector3 _targetPosition;    // куда движется rig (lerp цель)
    private float _targetZoom;        // целевой зум (lerp цель)

    private bool _isDragging;
    private Vector2 _dragStartScreen;   // экранная позиция начала drag

    // Мобильный pinch
    private float _pinchPrevDistance;
    private bool _isPinching;

    private bool _inputLocked;       // при GameOver = true

    // ── Lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        // Авто-заполнение ссылок
        if (_cameraRig == null)
            _cameraRig = transform;

        // _cameraTransform — ищем первого дочернего если не назначен
        if (_cameraTransform == null && _cameraRig.childCount > 0)
            _cameraTransform = _cameraRig.GetChild(0);

        if (_cameraTransform == null)
        {
            Debug.LogError("[CameraController] _cameraTransform не назначен! " +
                           "Создайте дочерний объект CameraPivot и назначьте его.");
        }
    }

    private void Start()
    {
        // Инициализируем цели из текущего положения
        _targetPosition = _cameraRig.position;
        _targetZoom = _cameraTransform != null
            ? _cameraTransform.localPosition.y
            : (_minZoom + _maxZoom) * 0.5f;

        // Подписка на GameOver
        if (EndGameManager.Instance != null)
            EndGameManager.Instance.OnStateChanged += OnGameStateChanged;
    }

    private void OnDestroy()
    {
        if (EndGameManager.Instance != null)
            EndGameManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    // ── Update ────────────────────────────────────────────────────────

    private void LateUpdate()
    {
        if (_inputLocked) return;

        if (DeviceDetector.CurrentDevice == DeviceType.Mobile)
        {
            HandleMobileInput();
        }
        else
        {
            HandleDesktopDrag();
            HandleDesktopZoom();
            if (_useKeyboard) HandleKeyboard();
            if (_useEdgeScroll) HandleEdgeScroll();
        }

        // Применяем плавное движение
        ApplyMovement();
    }

    // ═══════════════════════ DESKTOP ═════════════════════════════════

    private void HandleDesktopDrag()
    {
        if (Mouse.current == null) return;

        bool buttonDown = IsDesktopDragButtonDown();
        bool buttonHeld = IsDesktopDragButtonHeld();
        bool buttonUp = IsDesktopDragButtonUp();

        if (buttonDown)
        {
            // Не начинаем drag если нажали на UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            _isDragging = true;
            _dragStartScreen = Mouse.current.position.ReadValue();
        }

        if (buttonHeld && _isDragging)
        {
            Vector2 currentScreen = Mouse.current.position.ReadValue();
            Vector2 screenDelta = currentScreen - _dragStartScreen;

            // Переводим экранное смещение в мировое
            Vector3 move = ScreenDeltaToWorld(screenDelta);
            if (_invertDrag) move = -move;

            _targetPosition -= move;
            _dragStartScreen = currentScreen;
        }

        if (buttonUp)
            _isDragging = false;
    }

    private void HandleDesktopZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f)) return;

        _targetZoom -= scroll * _zoomSpeed;
        _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
    }

    private void HandleKeyboard()
    {
        // Поддержка как старого Input.GetAxis, так и WASD вручную
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (Mathf.Approximately(h, 0f) && Mathf.Approximately(v, 0f)) return;

        // Двигаемся относительно ориентации камеры
        Vector3 forward = GetCameraForwardFlat();
        Vector3 right = GetCameraRightFlat();

        Vector3 move = (right * h + forward * v) * (_keyboardSpeed * Time.unscaledDeltaTime);
        _targetPosition += move;
    }

    private void HandleEdgeScroll()
    {
        if (!Application.isFocused) return;

        Vector2 mouse = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Vector2.zero;

        float w = Screen.width;
        float h = Screen.height;
        float z = _edgeScrollZone;
        float speed = _edgeScrollSpeed * Time.unscaledDeltaTime;

        Vector3 move = Vector3.zero;

        if (mouse.x < z) move += -GetCameraRightFlat();
        if (mouse.x > w - z) move += GetCameraRightFlat();
        if (mouse.y < z) move += -GetCameraForwardFlat();
        if (mouse.y > h - z) move += GetCameraForwardFlat();

        _targetPosition += move * speed;
    }

    // ═══════════════════════ MOBILE ══════════════════════════════════

    private void HandleMobileInput()
    {
        int touchCount = Input.touchCount;

        if (touchCount == 0)
        {
            _isDragging = false;
            _isPinching = false;
            return;
        }

        if (touchCount == 1)
        {
            _isPinching = false;
            HandleMobileDrag();
        }
        else if (touchCount >= 2)
        {
            _isDragging = false;
            HandleMobilePinch();
        }
    }

    private void HandleMobileDrag()
    {
        Touch touch = Input.GetTouch(0);

        // Игнорируем касание по UI
        if (touch.phase == UnityEngine.TouchPhase.Began)
        {
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                return;

            _isDragging = true;
            _dragStartScreen = touch.position;
        }

        if (touch.phase == UnityEngine.TouchPhase.Moved && _isDragging)
        {
            Vector2 screenDelta = touch.position - _dragStartScreen;
            Vector3 move = ScreenDeltaToWorld(screenDelta);
            if (_invertDrag) move = -move;

            _targetPosition -= move;
            _dragStartScreen = touch.position;
        }

        if (touch.phase == UnityEngine.TouchPhase.Ended || touch.phase == UnityEngine.TouchPhase.Canceled)
            _isDragging = false;
    }

    private void HandleMobilePinch()
    {
        // Используем новый InputSystem если доступен, иначе fallback на старый
        if (Touchscreen.current != null && Touchscreen.current.touches.Count >= 2)
        {
            HandlePinchNewInputSystem();
        }
        else
        {
            HandlePinchLegacy();
        }
    }

    private void HandlePinchNewInputSystem()
    {
        TouchControl t0 = Touchscreen.current.touches[0];
        TouchControl t1 = Touchscreen.current.touches[1];

        Vector2 pos0 = t0.position.ReadValue();
        Vector2 pos1 = t1.position.ReadValue();

        float currentDist = Vector2.Distance(pos0, pos1);

        if (!_isPinching)
        {
            _pinchPrevDistance = currentDist;
            _isPinching = true;
            return;
        }

        float delta = currentDist - _pinchPrevDistance;
        _targetZoom -= delta * _mobileZoomSpeed;
        _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);

        _pinchPrevDistance = currentDist;
    }

    private void HandlePinchLegacy()
    {
        Touch t0 = Input.GetTouch(0);
        Touch t1 = Input.GetTouch(1);

        Vector2 prevPos0 = t0.position - t0.deltaPosition;
        Vector2 prevPos1 = t1.position - t1.deltaPosition;

        float prevDist = Vector2.Distance(prevPos0, prevPos1);
        float currDist = Vector2.Distance(t0.position, t1.position);
        float delta = currDist - prevDist;

        _targetZoom -= delta * _mobileZoomSpeed;
        _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
    }

    // ═══════════════════════ ПРИМЕНЕНИЕ ДВИЖЕНИЯ ═════════════════════

    private void ApplyMovement()
    {
        // Ограничение границами карты
        if (_useMapBounds)
            ClampToBounds();

        // Плавное движение Rig по XZ
        _cameraRig.position = Vector3.Lerp(
            _cameraRig.position,
            _targetPosition,
            _moveLerpSpeed * Time.unscaledDeltaTime);

        // Плавный зум по Y (только внутренний transform)
        if (_cameraTransform != null)
        {
            Vector3 localPos = _cameraTransform.localPosition;
            float newY = Mathf.Lerp(localPos.y, _targetZoom, _zoomLerpSpeed * Time.unscaledDeltaTime);
            _cameraTransform.localPosition = new Vector3(localPos.x, newY, localPos.z);
        }
    }

    private void ClampToBounds()
    {
        float x = Mathf.Clamp(_targetPosition.x,
            _mapBounds.x, _mapBounds.x + _mapBounds.width);
        float z = Mathf.Clamp(_targetPosition.z,
            _mapBounds.y, _mapBounds.y + _mapBounds.height);
        _targetPosition = new Vector3(x, _targetPosition.y, z);
    }

    // ═══════════════════════ УТИЛИТЫ ════════════════════════════════

    /// <summary>Переводит смещение в экранных пикселях в мировое смещение на плоскости XZ</summary>
    private Vector3 ScreenDeltaToWorld(Vector2 screenDelta)
    {
        Vector3 forward = GetCameraForwardFlat();
        Vector3 right = GetCameraRightFlat();

        return right * screenDelta.x * _dragSpeed
             + forward * screenDelta.y * _dragSpeed;
    }

    private Vector3 GetCameraForwardFlat()
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.forward;
        return Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
    }

    private Vector3 GetCameraRightFlat()
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.right;
        return Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
    }

    // ── Кнопки мыши через новый Input System ─────────────────────────

    private bool IsDesktopDragButtonDown()
    {
        return _desktopDragButton switch
        {
            MouseButton.Left => Mouse.current.leftButton.wasPressedThisFrame,
            MouseButton.Right => Mouse.current.rightButton.wasPressedThisFrame,
            MouseButton.Middle => Mouse.current.middleButton.wasPressedThisFrame,
            _ => false
        };
    }

    private bool IsDesktopDragButtonHeld()
    {
        return _desktopDragButton switch
        {
            MouseButton.Left => Mouse.current.leftButton.isPressed,
            MouseButton.Right => Mouse.current.rightButton.isPressed,
            MouseButton.Middle => Mouse.current.middleButton.isPressed,
            _ => false
        };
    }

    private bool IsDesktopDragButtonUp()
    {
        return _desktopDragButton switch
        {
            MouseButton.Left => Mouse.current.leftButton.wasReleasedThisFrame,
            MouseButton.Right => Mouse.current.rightButton.wasReleasedThisFrame,
            MouseButton.Middle => Mouse.current.middleButton.wasReleasedThisFrame,
            _ => false
        };
    }

    // ── GameOver ──────────────────────────────────────────────────────

    private void OnGameStateChanged(GameState state)
    {
        _inputLocked = (state == GameState.GameOver);
    }

    // ── Gizmos (отображение границ в редакторе) ───────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!_useMapBounds) return;

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.35f);

        Vector3 center = new Vector3(
            _mapBounds.x + _mapBounds.width * 0.5f,
            0f,
            _mapBounds.y + _mapBounds.height * 0.5f);

        Vector3 size = new Vector3(_mapBounds.width, 0.1f, _mapBounds.height);
        Gizmos.DrawCube(center, size);

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.9f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}

/// <summary>Выбор кнопки мыши для перетаскивания</summary>
public enum MouseButton
{
    Left,
    Right,
    Middle
}
