using UnityEngine;

public class PreviewSystem : MonoBehaviour
{
    [SerializeField] private float previewYOffset = 0.06f;
    [SerializeField] private GameObject cellIndicator;
    private GameObject previewObject;

    [SerializeField] private Material previewMaterialPrefab;
    private Material previewMaterialInstance;

    private Renderer cellIndicatorRenderer;
    [SerializeField] private Transform _Dynamic;

    // ── Ссылка на Grid для точного расчёта смещения курсора ──────────
    // Grid.CellToWorld() возвращает УГОЛ клетки (левый нижний corner).
    // Курсор (cellIndicator) масштабируется от своего центра, поэтому
    // его нужно сдвинуть на половину занимаемого прямоугольника.
    // Формула: offset = (size - 1) * cellSize * 0.5
    [SerializeField] private Grid _grid;

    private const string PREVIEW_LAYER_NAME = "Preview";

    // ── Поворот и размер ──────────────────────────────────────────────
    private Quaternion _currentRotation = Quaternion.identity;
    private Vector2Int _baseSize = Vector2Int.one;
    private Vector2Int _currentRotatedSize = Vector2Int.one;

    private void Start()
    {
        previewMaterialInstance = new Material(previewMaterialPrefab);
        cellIndicator.SetActive(false);
        cellIndicatorRenderer = cellIndicator.GetComponentInChildren<Renderer>();
    }

    public void StartShowingPlacementPreview(GameObject prefab, Vector2Int size)
    {
        previewObject = Instantiate(prefab);

        SetLayerRecursively(previewObject, LayerMask.NameToLayer(PREVIEW_LAYER_NAME));
        DisableAllTowerComponents(previewObject);
        PreparePreview(previewObject);
        previewObject.transform.SetParent(_Dynamic);

        _currentRotation = Quaternion.identity;
        _baseSize = size;
        _currentRotatedSize = size;

        previewObject.transform.rotation = _currentRotation;

        PrepareCursor(size);
        cellIndicator.SetActive(true);
    }

    // ── Поворот ───────────────────────────────────────────────────────

    public void RotatePreview()
    {
        if (previewObject == null) return;

        _currentRotation *= Quaternion.Euler(0f, 90f, 0f);
        previewObject.transform.rotation = _currentRotation;

        _currentRotatedSize = GetRotatedSize(_currentRotation);
        UpdateCursorSize(_currentRotatedSize);
    }

    public void SetPreviewRotation(Quaternion rotation)
    {
        if (previewObject == null) return;

        _currentRotation = rotation;
        previewObject.transform.rotation = _currentRotation;

        _currentRotatedSize = GetRotatedSize(_currentRotation);
        UpdateCursorSize(_currentRotatedSize);
    }

    public Quaternion GetPreviewRotation() => _currentRotation;
    public Vector2Int GetCurrentSize() => _currentRotatedSize;

    // ── Позиция ───────────────────────────────────────────────────────

    public void UpdatePosition(Vector3 position, bool validity)
    {
        if (previewObject != null)
        {
            MovePreview(position);
            ApplyFeedbackToPreview(validity);
        }
        MoveCursor(position);
        ApplyFeedbackToCursor(validity);
    }

    // ── Остановка ─────────────────────────────────────────────────────

    public void StopShowingPreview()
    {
        cellIndicator.SetActive(false);
        if (previewObject != null)
            Destroy(previewObject);

        _currentRotation = Quaternion.identity;
        _currentRotatedSize = Vector2Int.one;
    }

    public void StartShowingRemovePreview()
    {
        cellIndicator.SetActive(true);
        PrepareCursor(Vector2Int.one);
        ApplyFeedbackToCursor(false);
    }

    public Transform GetPreviewTransform()
    {
        return previewObject != null ? previewObject.transform : null;
    }

    // ── Приватные методы ──────────────────────────────────────────────

    private Vector2Int GetRotatedSize(Quaternion rotation)
    {
        float yAngle = rotation.eulerAngles.y;
        int steps = Mathf.RoundToInt(yAngle / 90f) % 4;
        if (steps < 0) steps += 4;
        bool swapAxes = (steps % 2) != 0;
        return swapAxes
            ? new Vector2Int(_baseSize.y, _baseSize.x)
            : new Vector2Int(_baseSize.x, _baseSize.y);
    }

    private void PrepareCursor(Vector2Int size)
    {
        UpdateCursorSize(size);
    }

    private void UpdateCursorSize(Vector2Int size)
    {
        cellIndicator.transform.localScale = new Vector3(size.x, 1, size.y);
        cellIndicatorRenderer.material.mainTextureScale = size;
    }

    private void PreparePreview(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
                materials[i] = previewMaterialInstance;
            renderer.materials = materials;
        }
    }

    private void ApplyFeedbackToPreview(bool validity)
    {
        Color c = validity ? Color.white : Color.red;
        c.a = 0.5f;
        previewMaterialInstance.color = c;
    }

    private void ApplyFeedbackToCursor(bool validity)
    {
        Color c = validity ? Color.white : Color.red;
        c.a = 0.5f;
        cellIndicatorRenderer.material.color = c;
    }

    private void MoveCursor(Vector3 position)
    {
        // CellToWorld() даёт угол клетки.
        // cellIndicator масштабируется от своего центра → нужно сдвинуть
        // на (size-1)*cellSize/2 чтобы угол курсора совпал с углом клетки.
        //
        // Пример: башня 2×3, cellSize=1
        //   без поворота:  offset = ((2-1)/2, 0, (3-1)/2) = (0.5, 0, 1.0)
        //   после 90°:     size становится 3×2
        //                  offset = ((3-1)/2, 0, (2-1)/2) = (1.0, 0, 0.5)
        Vector3 cellSize = _grid != null ? _grid.cellSize : Vector3.one;

        Vector3 offset = new Vector3(
            (_currentRotatedSize.x - 1) * cellSize.x * 0.5f,
            0f,
            (_currentRotatedSize.y - 1) * cellSize.z * 0.5f);

        cellIndicator.transform.position = position + offset;
    }

    private void MovePreview(Vector3 position)
    {
        // Модель ставится в ту же точку что и курсор —
        // с тем же смещением к центру занимаемого прямоугольника.
        // Pivot префаба должен быть в геометрическом центре модели.
        Vector3 cellSize = _grid != null ? _grid.cellSize : Vector3.one;

        Vector3 offset = new Vector3(
            (_currentRotatedSize.x - 1) * cellSize.x * 0.5f,
            previewYOffset,
            (_currentRotatedSize.y - 1) * cellSize.z * 0.5f);

        previewObject.transform.position = position + offset;
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, newLayer);
    }

    private void DisableAllTowerComponents(GameObject obj)
    {
        Tower tower = obj.GetComponent<Tower>();
        if (tower != null)
            tower.enabled = false;

        MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != this)
                script.enabled = false;
        }

        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
            collider.enabled = false;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;
    }
}