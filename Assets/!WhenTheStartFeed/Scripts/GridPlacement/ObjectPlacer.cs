using System.Collections.Generic;
using UnityEngine;

public class ObjectPlacer : MonoBehaviour
{
    // Список всех установленных объектов в сцене.
    // Хранит ссылки, чтобы можно было удалить объект по индексу.
    [SerializeField] private List<GameObject> placedGameObjects = new();
    [SerializeField] private Transform _Dynamic; //тут будут лежать все объекты, создаваемые во время игры

    public int PlaceObject(GameObject prefab, Vector3 position)// Создаёт объект по префабу в заданной позиции.
    {
        GameObject newObject = Instantiate(prefab);
        newObject.transform.position = position;
        newObject.transform.SetParent(_Dynamic);
        placedGameObjects.Add(newObject);// Сохраняем объект в список, чтобы потом можно было его удалить по индексу
        return placedGameObjects.Count - 1; // Возвращаем индекс созданного объекта
    }

    internal void RemoveObjectAt(int gameObjectIndex) // Удаляет GameObject по его индексу из списка placedGameObjects
    {
        if (placedGameObjects.Count <= gameObjectIndex      // проверяем, что индекс, который мы получили, не выходит за пределы списка
            || placedGameObjects[gameObjectIndex] == null) // что в списке есть объект по этому индексу.
            return;
        Destroy(placedGameObjects[gameObjectIndex]);
        placedGameObjects[gameObjectIndex] = null;      // Зануляем ссылку, чтобы не ломать индексы других объектов
    }
}