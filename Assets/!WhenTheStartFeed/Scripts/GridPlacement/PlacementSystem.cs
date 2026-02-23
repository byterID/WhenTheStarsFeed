using UnityEngine;

public class PlacementSystem : MonoBehaviour //
{
    [SerializeField] private InputManager inputManager; // управляет получением позиции курсора, кликами, выходом из режима и т.д.
    [SerializeField] private Grid grid;

    [SerializeField] private TowersDatabaseSO database; // база всех объектов для установки

    [SerializeField] private GameObject gridVisualization; // визуализирует сетку для размещения

    private GridData floorData, furnitureData; // данные о занятости клеток: отдельно для пола и мебели

    [SerializeField] private PreviewSystem preview;  // предпросмотр объекта/курсор

    private Vector3Int lastDetectedPosition = Vector3Int.zero;  // последняя клетка, в которой был курсор

    [SerializeField] private ObjectPlacer objectPlacer;  // создаёт/удаляет реальные GameObject в мире

    IBuildingState buildingState; // текущее состояние (режим): размещение или удаление

    [SerializeField] private SoundFeedback soundFeedback; //звуки

    [SerializeField] private MoneyManager moneyManager;//деньги

    private void Start()
    {
        gridVisualization.SetActive(false); // Грид скрыт, пока игрок не начнёт размещение
        floorData = new();
        furnitureData = new();
    }

    public void StartPlacement(int ID)  //запуск режима размещения
    {
        StopPlacement();                                        // Останавливаем предыдущий режим (если был)
        gridVisualization.SetActive(true);                      // Показываем сетку на полу
        buildingState = new PlacementState(ID,                   // Создаём новое состояние PlacementState
                                           grid,
                                           preview,
                                           database,
                                           floorData,
                                           furnitureData,
                                           objectPlacer,
                                           soundFeedback,
                                           moneyManager);
        inputManager.OnClicked += PlaceStructure;               // подписываемся на событие установки объекта
        inputManager.OnExit += StopPlacement;                   // подписываемся на кнопку выхода
    }

    public void StartRemoving()//Удаление структуры(включение режима удаления)
    {
        StopPlacement();                                        // Чистим предыдущий режим
        gridVisualization.SetActive(true);
        buildingState = new RemovingState(grid, preview, floorData, furnitureData, objectPlacer, soundFeedback); // устанавливаем новое состояние — RemovingState
        inputManager.OnClicked += PlaceStructure;               // Подписываем те же события установки/выхода (здесь установка считается удалением)
        inputManager.OnExit += StopPlacement;
    }

    private void PlaceStructure()
    {
        if (inputManager.IsPointerOverUI())
            return;

        Vector3 mousePosition = inputManager.GetSelectedMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);

        buildingState.OnAction(gridPosition);
    }
    private void StopPlacement()//Завершение режима(на ESC)
    {
        soundFeedback.PlaySound(SoundType.Click);
        if (buildingState == null)
            return;
        gridVisualization.SetActive(false);
        buildingState.EndState();                   // Сообщаем state, что он завершает работу
        inputManager.OnClicked -= PlaceStructure;   // Отписываемся от событий
        inputManager.OnExit -= StopPlacement;
        lastDetectedPosition = Vector3Int.zero;     // Сбрасываем последнюю позицию
        buildingState = null;                       // Очищаем состояние
    }

    private void Update()//обновление предпросмотра на каждый кадр
    {
        if (buildingState == null) // Если сейчас нет режима — ничего не делаем
            return;
        Vector3 mousePosition = inputManager.GetSelectedMapPosition();  // Получаем мировую позицию курсора
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);      // Переводим в клетку сетки
        if(lastDetectedPosition != gridPosition)                        // Обновляем только если клетка изменилась
        {
            buildingState.UpdateState(gridPosition);
            lastDetectedPosition = gridPosition;
        }
        
    }
}