using UnityEngine;

public class PlacementState : IBuildingState
{
    private int selectedObjectIndex = -1;
    int ID;
    Grid grid;
    PreviewSystem previewSystem;
    TowersDatabaseSO database;
    GridData floorData;
    GridData towersData;
    ObjectPlacer objectPlacer;
    SoundFeedback soundFeedback;
    MoneyManager moneyManager;

    private Quaternion _placementRotation = Quaternion.identity;

    public void SetPlacementRotation(Quaternion rotation)
    {
        _placementRotation = rotation;
    }

    public PlacementState(int iD,
                          Grid grid,
                          PreviewSystem previewSystem,
                          TowersDatabaseSO database,
                          GridData floorData,
                          GridData towersData,
                          ObjectPlacer objectPlacer,
                          SoundFeedback soundFeedback,
                          MoneyManager moneyManager)
    {
        ID = iD;
        this.grid = grid;
        this.previewSystem = previewSystem;
        this.database = database;
        this.floorData = floorData;
        this.towersData = towersData;
        this.objectPlacer = objectPlacer;
        this.soundFeedback = soundFeedback;
        this.moneyManager = moneyManager;

        selectedObjectIndex = database.objectsData.FindIndex(data => data.ID == ID);
        if (selectedObjectIndex > -1)
        {
            previewSystem.StartShowingPlacementPreview(
                database.objectsData[selectedObjectIndex].Prefab,
                database.objectsData[selectedObjectIndex].Size);
        }
        else
            throw new System.Exception($"No object with ID {iD}");
    }

    public void EndState()
    {
        previewSystem.StopShowingPreview();
    }

    public void OnAction(Vector3Int gridPosition)
    {
        // Берём актуальный размер с учётом поворота
        Vector2Int currentSize = previewSystem.GetCurrentSize();

        bool placementValidity = CheckPlacementValidity(gridPosition, currentSize);
        if (!placementValidity)
        {
            soundFeedback.PlaySound(SoundType.wrongPlacement);
            return;
        }

        int towerCost = database.objectsData[selectedObjectIndex].Cost;
        if (!moneyManager.TrySpend(towerCost))
        {
            soundFeedback.PlaySound(SoundType.wrongPlacement);
            return;
        }

        int index = objectPlacer.PlaceObject(
            database.objectsData[selectedObjectIndex].Prefab,
            grid.CellToWorld(gridPosition),
            database.objectsData[selectedObjectIndex].ID,
            gridPosition,
            _placementRotation);

        // Записываем повёрнутый размер — именно он определяет занятые клетки
        towersData.AddObjectAt(
            gridPosition,
            currentSize,
            database.objectsData[selectedObjectIndex].ID,
            index);

        previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), false);
        soundFeedback.PlaySound(SoundType.Place);
    }

    private bool CheckPlacementValidity(Vector3Int gridPosition, Vector2Int size)
    {
        if (!towersData.CanPlaceObejctAt(gridPosition, size))
            return false;
        if (!floorData.CanPlaceObejctAt(gridPosition, size))
            return false;
        return true;
    }

    public void UpdateState(Vector3Int gridPosition)
    {
        // Берём актуальный размер с учётом поворота для проверки валидности
        Vector2Int currentSize = previewSystem.GetCurrentSize();
        bool placementValidity = CheckPlacementValidity(gridPosition, currentSize);
        previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), placementValidity);
    }
}