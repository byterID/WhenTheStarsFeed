using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    public static PlacementSystem Instance { get; private set; }

    [SerializeField] private BuildingInputManager inputManager;
    [SerializeField] private Grid grid;
    [SerializeField] private TowersDatabaseSO database;
    [SerializeField] private GameObject gridVisualization;

    private GridData floorData, furnitureData;

    [SerializeField] private PreviewSystem preview;
    [SerializeField] private ObjectPlacer objectPlacer;
    [SerializeField] private SoundFeedback soundFeedback;
    [SerializeField] private MoneyManager moneyManager;
    [SerializeField] private Transform blockedAreasParent;
    [SerializeField] private PlacementConfirmUI confirmUI;
    [SerializeField] private Transform mapCenter;

    private IBuildingState buildingState;
    private Vector3Int lastDetectedPosition = Vector3Int.zero;
    private bool _isDragging = false;

    private int _currentTowerID = -1;

    public bool IsPlacing => buildingState != null;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        gridVisualization.SetActive(false);
        floorData = new();
        furnitureData = new();
        RegisterBlockedAreas();
    }

    // ── Перемещение ────────────────────────────────────────────────────

    public void StartMoving(TowerClickHandler tower)
    {
        TowerClickHandler.DeselectAll();
        _currentTowerID = tower.towerID;
        StopPlacement();
        gridVisualization.SetActive(true);

        int objectIndex = furnitureData.GetRepresentationIndex(tower.gridCell);
        if (objectIndex == -1)
            objectIndex = floorData.GetRepresentationIndex(tower.gridCell);

        // Передаём preview — TowerMoveState сам установит начальный поворот
        buildingState = new TowerMoveState(
            tower, objectIndex, grid, preview, database,
            floorData, furnitureData, objectPlacer, soundFeedback);

        Vector3Int startCell = tower.gridCell;
        buildingState.UpdateState(startCell);
        lastDetectedPosition = startCell;

        inputManager.Activate(grid.CellToWorld(startCell));
        inputManager.OnDragMove += OnDragMove;
        inputManager.OnDragReleased += OnDragReleased;
        inputManager.OnExit += StopPlacement;
    }

    // ── Placement ──────────────────────────────────────────────────────

    public void StartPlacement(int ID)
    {
        _currentTowerID = ID;
        StopPlacement();
        gridVisualization.SetActive(true);

        buildingState = new PlacementState(
            ID, grid, preview, database,
            floorData, furnitureData,
            objectPlacer, soundFeedback, moneyManager);

        Vector3 centerPos = mapCenter != null ? mapCenter.position : Vector3.zero;
        Vector3Int centerCell = grid.WorldToCell(centerPos);
        buildingState.UpdateState(centerCell);
        lastDetectedPosition = centerCell;

        inputManager.Activate(grid.CellToWorld(centerCell));
        inputManager.OnDragMove += OnDragMove;
        inputManager.OnDragReleased += OnDragReleased;
        inputManager.OnExit += StopPlacement;
    }

    public void StartRemoving()
    {
        StopPlacement();
        gridVisualization.SetActive(true);
        buildingState = new RemovingState(
            grid, preview, floorData, furnitureData,
            objectPlacer, soundFeedback);

        inputManager.Activate(Vector3.zero);
        inputManager.OnDragMove += OnDragMove;
        inputManager.OnDragReleased += OnDragReleased;
        inputManager.OnExit += StopPlacement;
    }

    // ── Drag callbacks ─────────────────────────────────────────────────

    private void OnDragMove(Vector3 worldPos)
    {
        if (buildingState == null) return;

        _isDragging = true;
        confirmUI.Hide();

        Vector3Int gridPos = grid.WorldToCell(worldPos);
        if (gridPos != lastDetectedPosition)
        {
            buildingState.UpdateState(gridPos);
            lastDetectedPosition = gridPos;
        }
    }

    private void OnDragReleased()
    {
        if (buildingState == null) return;
        _isDragging = false;

        Transform ghostTransform = preview.GetPreviewTransform();

        bool canRotate = buildingState is PlacementState || buildingState is TowerMoveState;

        confirmUI.Show(
            ghostTransform,
            onConfirm: ConfirmPlacement,
            onCancel:  StopPlacement,
            canRotate: canRotate,
            onRotate:  OnRotateGhost);
    }

    // ── Поворот голограммы ─────────────────────────────────────────────

    private void OnRotateGhost()
    {
        preview.RotatePreview();
    }

    // ── Confirm / Cancel ───────────────────────────────────────────────

    private void ConfirmPlacement()
    {
        if (buildingState == null) return;

        // Берём поворот прямо из PreviewSystem — он всегда актуален
        Quaternion rotation = preview.GetPreviewRotation();

        if (buildingState is PlacementState ps)
            ps.SetPlacementRotation(rotation);
        else if (buildingState is TowerMoveState tms)
            tms.SetPlacementRotation(rotation);

        Vector3Int gridPos = grid.WorldToCell(inputManager.GetSelectedMapPosition());
        buildingState.OnAction(gridPos);
        StopPlacement();
    }

    private void StopPlacement()
    {
        TowerClickHandler.DeselectAll();
        soundFeedback.PlaySound(SoundType.Click);
        if (buildingState == null) return;

        gridVisualization.SetActive(false);
        buildingState.EndState();

        inputManager.OnDragMove -= OnDragMove;
        inputManager.OnDragReleased -= OnDragReleased;
        inputManager.OnExit -= StopPlacement;
        inputManager.Deactivate();

        confirmUI.Hide();

        lastDetectedPosition = Vector3Int.zero;
        _isDragging = false;
        buildingState = null;
    }

    // ── RegisterBlockedAreas ───────────────────────────────────────────

    private void RegisterBlockedAreas()
    {
        foreach (Transform child in blockedAreasParent)
        {
            BlockedAreaMarker marker = child.GetComponent<BlockedAreaMarker>();
            if (marker == null) continue;

            Vector3 bottomLeft = child.position
                - new Vector3(marker.size.x / 2f, 0, marker.size.y / 2f);
            Vector3Int startCell = grid.WorldToCell(bottomLeft);

            for (int x = 0; x < marker.size.x; x++)
                for (int z = 0; z < marker.size.y; z++)
                {
                    Vector3Int cell = startCell + new Vector3Int(x, 0, z);
                    floorData.AddObjectAt(cell, Vector2Int.one, -1, -1);
                }
        }
    }
}