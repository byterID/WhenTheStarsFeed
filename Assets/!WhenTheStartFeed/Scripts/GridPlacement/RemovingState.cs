using UnityEngine;

public class RemovingState : IBuildingState
{
    private int gameObjectIndex = -1; // Индекс объекта, который нужно удалить
    Grid grid;
    PreviewSystem previewSystem;
    GridData floorData;
    GridData towersData;
    ObjectPlacer objectPlacer;
    SoundFeedback soundFeedback; 

    public RemovingState(Grid grid, // При создании сразу включаем предпросмотр для удаления
                         PreviewSystem previewSystem,
                         GridData floorData,
                         GridData towersData,
                         ObjectPlacer objectPlacer,
                         SoundFeedback soundFeedback)
    {
        this.grid = grid;
        this.previewSystem = previewSystem;
        this.floorData = floorData;
        this.towersData = towersData;
        this.objectPlacer = objectPlacer;
        this.soundFeedback = soundFeedback;
        previewSystem.StartShowingRemovePreview();
    }

    public void EndState()
    {
        previewSystem.StopShowingPreview();
    }

    public void OnAction(Vector3Int gridPosition)   //Удаление
    {
        GridData selectedData = null;
        if(towersData.CanPlaceObejctAt(gridPosition,Vector2Int.one) == false)
        {
            selectedData = towersData;
        }
        else if(floorData.CanPlaceObejctAt(gridPosition, Vector2Int.one) == false)
        {
            selectedData = floorData;
        }

        if(selectedData == null)
        {
            //нет объекта — ошибка
            soundFeedback.PlaySound(SoundType.wrongPlacement);
        }
        else
        {
            soundFeedback.PlaySound(SoundType.Remove);                          // Есть объект — удаляем
            gameObjectIndex = selectedData.GetRepresentationIndex(gridPosition);// Получаем индекс объекта, который нужно удалить из сцены
            if (gameObjectIndex == -1)      // Если индекс некорректный — выходим
                return;
            selectedData.RemoveObjectAt(gridPosition);              // Удаляем объект из данных сетки
            objectPlacer.RemoveObjectAt(gameObjectIndex);           // Удаляем объект из сцены
        }
        Vector3 cellPosition = grid.CellToWorld(gridPosition);      // обновляем предпросмотр на текущей клетке
        //previewSystem.UpdatePosition(cellPosition, CheckIfSelectionIsValid(gridPosition));
    }

    // Проверяем, можно ли удалить объект
    // Возвращает true, если клетка занята (а значит её можно удалять)
    private bool CheckIfSelectionIsValid(Vector3Int gridPosition)
    {
        // Если обе сетки говорят "тут пусто" => нельзя удалять
        // Инверсия делает valid = true только если есть объект
        return !(towersData.CanPlaceObejctAt(gridPosition, Vector2Int.one) &&
            floorData.CanPlaceObejctAt(gridPosition, Vector2Int.one));
    }

    public void UpdateState(Vector3Int gridPosition) // Когда игрок двигает курсор — обновляем подсветку
    {
        bool validity = CheckIfSelectionIsValid(gridPosition);
        //previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), validity);// Переносим предпросмотр на новую клетку
    }
}