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

    // ── Поворот голограммы — устанавливается из PlacementSystem ──────
    // PlacementSystem вызывает SetPlacementRotation() перед OnAction,
    // чтобы передать поворот который игрок выставил кнопкой Rotate.
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
        bool placementValidity = CheckPlacementValidity(gridPosition, selectedObjectIndex);
        if (!placementValidity)
        {
            soundFeedback.PlaySound(SoundType.wrongPlacement);
            return;
        }

        int towerCost = database.objectsData[selectedObjectIndex].Cost;
        if (!moneyManager.TrySpend(towerCost))
        {
            Debug.Log("Недостаточно денег! Нужно: " + towerCost + ", есть: " + moneyManager.CurrentMoney);
            soundFeedback.PlaySound(SoundType.wrongPlacement);
            return;
        }

        int index = objectPlacer.PlaceObject(
            database.objectsData[selectedObjectIndex].Prefab,
            grid.CellToWorld(gridPosition),
            database.objectsData[selectedObjectIndex].ID,
            gridPosition,
            _placementRotation);   // ← передаём поворот голограммы

        GridData selectedData = towersData;
        selectedData.AddObjectAt(
            gridPosition,
            database.objectsData[selectedObjectIndex].Size,
            database.objectsData[selectedObjectIndex].ID,
            index);

        previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), false);
        soundFeedback.PlaySound(SoundType.Place);

        Debug.Log("Башня куплена! Осталось денег: " + moneyManager.CurrentMoney);
    }

    private bool CheckPlacementValidity(Vector3Int gridPosition, int selectedObjectIndex)
    {
        Vector2Int size = database.objectsData[selectedObjectIndex].Size;

        if (!towersData.CanPlaceObejctAt(gridPosition, size))
            return false;

        if (!floorData.CanPlaceObejctAt(gridPosition, size))
            return false;

        return true;
    }

    public void UpdateState(Vector3Int gridPosition)
    {
        bool placementValidity = CheckPlacementValidity(gridPosition, selectedObjectIndex);
        previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), placementValidity);
    }
}
