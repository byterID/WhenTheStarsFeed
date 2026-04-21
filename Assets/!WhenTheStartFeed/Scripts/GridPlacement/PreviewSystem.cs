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

    // Добавляем переменную для хранения оригинального слоя
    private int originalLayer;
    // Константа с именем слоя (чтобы не ошибиться в написании)
    private const string PREVIEW_LAYER_NAME = "Preview";

    private void Start()
    {
        previewMaterialInstance = new Material(previewMaterialPrefab);
        cellIndicator.SetActive(false);
        cellIndicatorRenderer = cellIndicator.GetComponentInChildren<Renderer>();
    }

    public void StartShowingPlacementPreview(GameObject prefab, Vector2Int size)
    {
        // Создаём копию объекта
        previewObject = Instantiate(prefab);

        // СОХРАНЯЕМ ОРИГИНАЛЬНЫЙ СЛОЙ (на всякий случай)
        originalLayer = previewObject.layer;

        // УСТАНАВЛИВАЕМ СЛОЙ PREVIEW
        SetLayerRecursively(previewObject, LayerMask.NameToLayer(PREVIEW_LAYER_NAME));

        // ОТКЛЮЧАЕМ ВСЕ КОМПОНЕНТЫ С ЛОГИКОЙ (как дополнительная мера)
        DisableAllTowerComponents(previewObject);

        PreparePreview(previewObject);
        PrepareCursor(size);
        cellIndicator.SetActive(true);
        previewObject.transform.SetParent(_Dynamic);
    }

    // Рекурсивно устанавливает слой для объекта и всех его детей
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        // Устанавливаем слой для текущего объекта
        obj.layer = newLayer;

        // Устанавливаем для всех дочерних объектов
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    // Отключаем все компоненты башни, чтобы они точно не работали
    private void DisableAllTowerComponents(GameObject obj)
    {
        // Отключаем скрипт башни
        Tower tower = obj.GetComponent<Tower>();
        if (tower != null)
            tower.enabled = false;

        // Отключаем все остальные скрипты (опционально)
        MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>();
        foreach (var script in scripts)
        {
            // Не отключаем этот скрипт, если он есть на объекте (маловероятно)
            if (script != this)
                script.enabled = false;
        }

        // Отключаем коллайдеры (хотя слои уже должны работать)
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        // Отключаем Rigidbody если есть
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;
    }

    // ОСТАЛЬНЫЕ МЕТОДЫ БЕЗ ИЗМЕНЕНИЙ
    private void PrepareCursor(Vector2Int size)
    {
        if (size.x > 0 || size.y > 0)
        {
            cellIndicator.transform.localScale = new Vector3(size.x, 1, size.y);
            cellIndicatorRenderer.material.mainTextureScale = size;
        }
    }

    private void PreparePreview(GameObject previewObject)
    {
        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = previewMaterialInstance;
            }
            renderer.materials = materials;
        }
    }

    public void StopShowingPreview()
    {
        cellIndicator.SetActive(false);
        if (previewObject != null)
        {
            // Можно вернуть оригинальный слой, но мы всё равно удаляем объект
            Destroy(previewObject);
        }
    }

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
        cellIndicator.transform.position = position;
    }

    private void MovePreview(Vector3 position)
    {
        previewObject.transform.position = new Vector3(
            position.x,
            position.y + previewYOffset,
            position.z);
    }

    internal void StartShowingRemovePreview()
    {
        cellIndicator.SetActive(true);
        PrepareCursor(Vector2Int.one);
        ApplyFeedbackToCursor(false);
    }
    public Transform GetPreviewTransform()
    {
        return previewObject != null ? previewObject.transform : null;
    }
}