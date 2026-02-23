using UnityEngine;

public class PlacementState : IBuildingState
{
    private int selectedObjectIndex = -1;       //индекс объекта в базе
    int ID;                                     // ID выбранного объекта, который пытаемся разместить
    Grid grid;
    PreviewSystem previewSystem;
    TowersDatabaseSO database;
    GridData floorData;
    GridData furnitureData;
    ObjectPlacer objectPlacer;
    SoundFeedback soundFeedback;
    MoneyManager moneyManager;

    public PlacementState(int iD,
                          Grid grid,
                          PreviewSystem previewSystem,
                          TowersDatabaseSO database,
                          GridData floorData,
                          GridData furnitureData,
                          ObjectPlacer objectPlacer,
                          SoundFeedback soundFeedback,
                          MoneyManager moneyManager)
    {
        ID = iD;                                //сохраняем id выбранного объекта
        this.grid = grid;                       //ссылки на нужные сущности
        this.previewSystem = previewSystem;
        this.database = database;
        this.floorData = floorData;
        this.furnitureData = furnitureData;
        this.objectPlacer = objectPlacer;
        this.soundFeedback = soundFeedback;
        this.moneyManager = moneyManager;

        selectedObjectIndex = database.objectsData.FindIndex(data => data.ID == ID);    //находим объект в базе по id
        if (selectedObjectIndex > -1)                                                   // Если объект найден, то запускаем предпросмотр
        {
            previewSystem.StartShowingPlacementPreview(
                database.objectsData[selectedObjectIndex].Prefab,
                database.objectsData[selectedObjectIndex].Size);
        }
        else
            throw new System.Exception($"No object with ID {iD}");
        
    }

    public void EndState()      // вызывается, когда режим размещения заканчивается
    {
        previewSystem.StopShowingPreview();
    }

    public void OnAction(Vector3Int gridPosition)
    {
        // Проверяем, можно ли разместить объект
        bool placementValidity = CheckPlacementValidity(gridPosition, selectedObjectIndex);

        if (placementValidity == false)
        {
            soundFeedback.PlaySound(SoundType.wrongPlacement);
            return;
        }

        // ПОЛУЧАЕМ ЦЕНУ БАШНИ
        int towerCost = database.objectsData[selectedObjectIndex].Cost;

        // ПРОВЕРЯЕМ, ХВАТАЕТ ЛИ ДЕНЕГ
        if (!moneyManager.TrySpend(towerCost))
        {
            Debug.Log("Недостаточно денег! Нужно: " + towerCost + ", есть: " + moneyManager.CurrentMoney);
            //soundFeedback.PlaySound(SoundType.wrongPlacement); звук для недостатка денег
            return;
        }

        // Размещаем объект
        int index = objectPlacer.PlaceObject(
            database.objectsData[selectedObjectIndex].Prefab,
            grid.CellToWorld(gridPosition));

        // Обновляем данные о занятости клеток
        GridData selectedData = furnitureData;
        selectedData.AddObjectAt(gridPosition,
            database.objectsData[selectedObjectIndex].Size,
            database.objectsData[selectedObjectIndex].ID,
            index);

        previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), false);

        // Звук успешной установки
        soundFeedback.PlaySound(SoundType.Place);

        Debug.Log("Башня куплена! Осталось денег: " + moneyManager.CurrentMoney);
    }

    private bool CheckPlacementValidity(Vector3Int gridPosition, int selectedObjectIndex)  // Проверяет, можно ли ставить объект в указанную позицию
    {
        GridData selectedData = database.objectsData[selectedObjectIndex].ID == 0 ? //ТОЖЕ ХУЙНЯ ИСПРАВИТЬ!
            floorData :
            furnitureData;

        return selectedData.CanPlaceObejctAt(gridPosition, database.objectsData[selectedObjectIndex].Size);
    }

    public void UpdateState(Vector3Int gridPosition) // Вызывается каждый кадр при перемещении курсора по сетке
    {
        bool placementValidity = CheckPlacementValidity(gridPosition, selectedObjectIndex);// Проверяем валидность размещения

        previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), placementValidity);// Обновляем предпросмотр
    }
}