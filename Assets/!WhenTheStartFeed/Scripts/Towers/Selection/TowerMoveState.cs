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

    private bool _confirmed = false;

    // ── Поворот голограммы — устанавливается из PlacementSystem ──────
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

        // Запоминаем текущий поворот башни как стартовый поворот голограммы
        _placementRotation = sourceTower.transform.rotation;

        // ── Освобождаем клетки ДО показа голограммы ───────────────────
        _towersData.RemoveObjectAtSafe(_originalCell);

        // Скрываем оригинальную башню
        _sourceTower.gameObject.SetActive(false);

        // Показываем голограмму с тем же поворотом что была у башни
        _preview.StartShowingPlacementPreview(
            database.objectsData[selectedObjectIndex].Prefab,
            database.objectsData[selectedObjectIndex].Size);

        // Применяем начальный поворот к голограмме
        Transform ghost = _preview.GetPreviewTransform();
        if (ghost != null)
            ghost.rotation = _placementRotation;
    }

    public void EndState()
    {
        _preview.StopShowingPreview();

        if (!_confirmed && _sourceTower != null)
        {
            _sourceTower.gameObject.SetActive(true);
            _towersData.AddObjectAtSafe(
                _originalCell,
                _database.objectsData[selectedObjectIndex].Size,
                _towerID,
                _originalObjectIndex);
        }
    }

    public void OnAction(Vector3Int gridPosition)
    {
        bool valid = CheckValidity(gridPosition);
        if (!valid)
        {
            _soundFeedback.PlaySound(SoundType.wrongPlacement);
            return;
        }

        _confirmed = true;

        Vector3 newWorldPos = _grid.CellToWorld(gridPosition);

        // Применяем новую позицию И поворот из голограммы
        _sourceTower.transform.position = newWorldPos;
        _sourceTower.transform.rotation = _placementRotation;
        _sourceTower.gridCell = gridPosition;
        _sourceTower.gameObject.SetActive(true);

        _towersData.AddObjectAtSafe(
            gridPosition,
            _database.objectsData[selectedObjectIndex].Size,
            _towerID,
            _originalObjectIndex);

        _soundFeedback.PlaySound(SoundType.Place);
    }

    public void UpdateState(Vector3Int gridPosition)
    {
        bool valid = CheckValidity(gridPosition);
        _preview.UpdatePosition(_grid.CellToWorld(gridPosition), valid);
    }

    private bool CheckValidity(Vector3Int gridPosition)
    {
        Vector2Int size = _database.objectsData[selectedObjectIndex].Size;
        return _towersData.CanPlaceObejctAt(gridPosition, size)
            && _floorData.CanPlaceObejctAt(gridPosition, size);
    }
}
