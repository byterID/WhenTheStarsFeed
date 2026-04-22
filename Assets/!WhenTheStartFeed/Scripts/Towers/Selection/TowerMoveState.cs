using UnityEngine;

public class TowerMoveState : IBuildingState
{
    private int selectedObjectIndex = -1;
    private readonly int _towerID;
    private readonly Grid _grid;
    private readonly PreviewSystem _preview;
    private readonly TowersDatabaseSO _database;
    private readonly GridData _floorData;
    private readonly GridData _towersData;
    private readonly ObjectPlacer _objectPlacer;
    private readonly SoundFeedback _soundFeedback;

    private readonly TowerClickHandler _sourceTower;
    private readonly Vector3Int _originalCell;
    private readonly int _originalObjectIndex;
    private readonly Vector2Int _originalSize; // запомним оригинальный размер для возврата

    private bool _confirmed = false;

    // Поворот — устанавливается из PlacementSystem перед OnAction
    private Quaternion _placementRotation = Quaternion.identity;

    public void SetPlacementRotation(Quaternion rotation)
    {
        _placementRotation = rotation;
    }

    public TowerMoveState(TowerClickHandler sourceTower,
                          int originalObjectIndex,
                          Grid grid,
                          PreviewSystem preview,
                          TowersDatabaseSO database,
                          GridData floorData,
                          GridData towersData,
                          ObjectPlacer objectPlacer,
                          SoundFeedback soundFeedback)
    {
        _sourceTower = sourceTower;
        _originalCell = sourceTower.gridCell;
        _originalObjectIndex = originalObjectIndex;
        _grid = grid;
        _preview = preview;
        _database = database;
        _floorData = floorData;
        _towersData = towersData;
        _objectPlacer = objectPlacer;
        _soundFeedback = soundFeedback;

        _towerID = sourceTower.towerID;
        selectedObjectIndex = database.objectsData.FindIndex(d => d.ID == _towerID);

        if (selectedObjectIndex < 0)
            throw new System.Exception($"TowerMoveState: no tower with ID {_towerID}");

        _originalSize = database.objectsData[selectedObjectIndex].Size;

        _towersData.RemoveObjectAtSafe(_originalCell);
        _sourceTower.gameObject.SetActive(false);

        _preview.StartShowingPlacementPreview(
            database.objectsData[selectedObjectIndex].Prefab,
            _originalSize);

        // Голограмма открывается с текущим поворотом башни
        _preview.SetPreviewRotation(sourceTower.transform.rotation);
    }

    public void EndState()
    {
        _preview.StopShowingPreview();

        if (!_confirmed && _sourceTower != null)
        {
            _sourceTower.gameObject.SetActive(true);
            // Возвращаем с оригинальным размером (до любых поворотов в этой сессии)
            _towersData.AddObjectAtSafe(
                _originalCell,
                _originalSize,
                _towerID,
                _originalObjectIndex);
        }
    }

    public void OnAction(Vector3Int gridPosition)
    {
        Vector2Int currentSize = _preview.GetCurrentSize();

        bool valid = CheckValidity(gridPosition, currentSize);
        if (!valid)
        {
            _soundFeedback.PlaySound(SoundType.wrongPlacement);
            return;
        }

        _confirmed = true;

        _sourceTower.transform.position = _grid.CellToWorld(gridPosition);
        _sourceTower.transform.rotation = _placementRotation;
        _sourceTower.gridCell = gridPosition;
        _sourceTower.gameObject.SetActive(true);

        // Записываем повёрнутый размер
        _towersData.AddObjectAtSafe(
            gridPosition,
            currentSize,
            _towerID,
            _originalObjectIndex);

        _soundFeedback.PlaySound(SoundType.Place);
    }

    public void UpdateState(Vector3Int gridPosition)
    {
        Vector2Int currentSize = _preview.GetCurrentSize();
        bool valid = CheckValidity(gridPosition, currentSize);
        _preview.UpdatePosition(_grid.CellToWorld(gridPosition), valid);
    }

    private bool CheckValidity(Vector3Int gridPosition, Vector2Int size)
    {
        return _towersData.CanPlaceObejctAt(gridPosition, size)
            && _floorData.CanPlaceObejctAt(gridPosition, size);
    }
}