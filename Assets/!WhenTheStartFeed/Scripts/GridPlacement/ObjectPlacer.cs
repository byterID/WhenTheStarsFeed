using System.Collections.Generic;
using UnityEngine;

public class ObjectPlacer : MonoBehaviour
{
    [SerializeField] private List<GameObject> placedGameObjects = new();
    [SerializeField] private Transform _Dynamic;

    // Добавляем константу с именем слоя для настоящих башен
    private const string TOWER_LAYER_NAME = "Default"; // или создайте свой слой "Tower"

    public int PlaceObject(GameObject prefab, Vector3 position)
    {
        GameObject newObject = Instantiate(prefab);
        newObject.transform.position = position;
        newObject.transform.SetParent(_Dynamic);

        // ВАЖНО: Устанавливаем правильный слой для настоящей башни
        SetLayerRecursively(newObject, LayerMask.NameToLayer(TOWER_LAYER_NAME));

        // Включаем все компоненты (они могли быть отключены в превью)
        EnableAllTowerComponents(newObject);

        placedGameObjects.Add(newObject);
        return placedGameObjects.Count - 1;
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    private void EnableAllTowerComponents(GameObject obj)
    {
        // Включаем скрипт башни
        Tower tower = obj.GetComponent<Tower>();
        if (tower != null)
            tower.enabled = true;

        // Включаем все MonoBehaviour
        MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>();
        foreach (var script in scripts)
        {
            script.enabled = true;
        }

        // Включаем коллайдеры
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = true;
        }
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