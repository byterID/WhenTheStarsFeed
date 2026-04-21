using System.Collections.Generic;
using UnityEngine;

public class ObjectPlacer : MonoBehaviour
{
    [SerializeField] private List<GameObject> placedGameObjects = new();
    [SerializeField] private Transform _Dynamic;

    private const string TOWER_LAYER_NAME = "Default";

    // ── Сигнатура с towerID, gridCell и rotation ─────────────────────
    // rotation — поворот из голограммы (игрок мог нажать кнопку Rotate).
    // Quaternion.identity = поворот по умолчанию из префаба.
    public int PlaceObject(GameObject prefab, Vector3 position, int towerID, Vector3Int gridCell,
                           Quaternion rotation = default)
    {
        // default(Quaternion) == (0,0,0,0) — не валидный кватернион, заменяем на identity
        if (rotation == default)
            rotation = Quaternion.identity;

        GameObject newObject = Instantiate(prefab, position, rotation);
        newObject.transform.SetParent(_Dynamic);

        SetLayerRecursively(newObject, LayerMask.NameToLayer(TOWER_LAYER_NAME));
        EnableAllTowerComponents(newObject);

        // Хэндлер ТОЛЬКО на корневом объекте — не AddComponent на детях
        TowerClickHandler handler = newObject.GetComponent<TowerClickHandler>();
        if (handler == null)
            handler = newObject.AddComponent<TowerClickHandler>();

        handler.towerID = towerID;
        handler.gridCell = gridCell;

        Debug.Log($"PlaceObject: towerID={towerID}, gridCell={gridCell}, rotation={rotation.eulerAngles}, handler на объекте={handler.gameObject.name}");

        placedGameObjects.Add(newObject);
        return placedGameObjects.Count - 1;
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, newLayer);
    }

    private void EnableAllTowerComponents(GameObject obj)
    {
        Tower tower = obj.GetComponent<Tower>();
        if (tower != null)
            tower.enabled = true;

        MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>();
        foreach (var script in scripts)
            script.enabled = true;

        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
            collider.enabled = true;
    }

    internal void RemoveObjectAt(int gameObjectIndex)
    {
        if (placedGameObjects.Count <= gameObjectIndex
            || placedGameObjects[gameObjectIndex] == null)
            return;

        Destroy(placedGameObjects[gameObjectIndex]);
        placedGameObjects[gameObjectIndex] = null;
    }
}
