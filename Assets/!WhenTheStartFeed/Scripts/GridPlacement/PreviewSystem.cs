using UnityEngine;

public class PreviewSystem : MonoBehaviour
{
    [SerializeField] private float previewYOffset = 0.06f; // поднимаем чтобы призрак не клипался в земле

    [SerializeField] private GameObject cellIndicator;      // индикатор клетки — показывает занимаемую область
    private GameObject previewObject;                //призрак объекта

    [SerializeField] private Material previewMaterialPrefab;    //полупрозрачный шейдер
    private Material previewMaterialInstance;   //копия материала для предпросмотра

    private Renderer cellIndicatorRenderer;     //индикатор клетки-курсора

    [SerializeField] private Transform _Dynamic;

    private void Start()
    {
        previewMaterialInstance = new Material(previewMaterialPrefab);      // Создаём копию материала, чтобы менять цвет независимо от других объектов
        cellIndicator.SetActive(false);                                     // Скрываем индикатор, пока нет предпросмотра
        cellIndicatorRenderer = cellIndicator.GetComponentInChildren<Renderer>();
    }

    public void StartShowingPlacementPreview(GameObject prefab, Vector2Int size)
    {
        previewObject = Instantiate(prefab); // Создаём копию объекта, который будем показывать игроку
        PreparePreview(previewObject);      // Подготавливаем рендеры — задаём прозрачный материал предпросмотра
        PrepareCursor(size);                // Масштабируем индикатор клетки под размер объекта
        cellIndicator.SetActive(true);      // Показываем индикатор
        previewObject.transform.SetParent(_Dynamic);
    }

    private void PrepareCursor(Vector2Int size)
    {
        if(size.x > 0 || size.y > 0) // Меняем размер клетки под размер объекта
        {
            cellIndicator.transform.localScale = new Vector3(size.x, 1, size.y);
            cellIndicatorRenderer.material.mainTextureScale = size;     // настройка Tilling текстуры индикатора
        }
    }

    private void PreparePreview(GameObject previewObject)
    {
        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();   // Находим все Renderer в объекте и его детях, чтоб сделать прозрачными
        foreach(Renderer renderer in renderers)
        {
            Material[] materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)  // Каждый материал заменяем на полупрозрачный
            {
                materials[i] = previewMaterialInstance;
            }
            renderer.materials = materials;
        }
    }

    public void StopShowingPreview() //Остановка предпросмотра
    {
        cellIndicator.SetActive(false);
        if(previewObject!= null)
            Destroy(previewObject);
    }

    public void UpdatePosition(Vector3 position, bool validity)
    {
        if(previewObject != null)// Если существует объект предпросмотра — двигаем его и меняем цвет
        {
            MovePreview(position);
            ApplyFeedbackToPreview(validity);

        }

        MoveCursor(position);// Двигаем индикатор клетки
        ApplyFeedbackToCursor(validity);// Меняем цвет индикатора под текущую доступность
    }

    private void ApplyFeedbackToPreview(bool validity) //Подсветка предпросмотра
    {
        Color c = validity ? Color.white : Color.red; // Если true — белый; если false — красный
        
        c.a = 0.5f; //color.alpha (прозрачность)
        previewMaterialInstance.color = c;
    }

    private void ApplyFeedbackToCursor(bool validity) //Подсветка курсора
    {
        Color c = validity ? Color.white : Color.red;

        c.a = 0.5f;
        cellIndicatorRenderer.material.color = c;
    }

    private void MoveCursor(Vector3 position)//Передвижение индикатора клетки
    {
        cellIndicator.transform.position = position;
    }

    private void MovePreview(Vector3 position) //Передвижение предпросмотра
    {
        previewObject.transform.position = new Vector3(
            position.x, 
            position.y + previewYOffset, 
            position.z);
    }

    internal void StartShowingRemovePreview()
    {
        cellIndicator.SetActive(true);  // Включаем клетку-курсор
        PrepareCursor(Vector2Int.one);  // При удалении всегда размер курсора = 1×1
        ApplyFeedbackToCursor(false);   // Изначально показываем красную подсветку
    }
}