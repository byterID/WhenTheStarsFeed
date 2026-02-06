using System;
using System.Collections.Generic;
using UnityEngine;

public class GridData
{
    // Словарь, который хранит данные о всех установленных объектах
    // Ключ = позиция клетки в сетке (Vector3Int)
    // Значение = информация о том, какие клетки занимает объект, его ID и индекс в ObjectPlacer
    Dictionary<Vector3Int, PlacementData> placedObjects = new();

    // Добавляет объект на сетку
    public void AddObjectAt(Vector3Int gridPosition,
                            Vector2Int objectSize,
                            int ID,
                            int placedObjectIndex)
    {
        List<Vector3Int> positionToOccupy = CalculatePositions(gridPosition, objectSize);   // Вычисляем все клетки, которые объект займёт
        PlacementData data = new PlacementData(positionToOccupy, ID, placedObjectIndex);    // Создаём объект данных для всех этих клеток
        foreach (var pos in positionToOccupy) // Добавляем в словарь
        {
            if (placedObjects.ContainsKey(pos))
                throw new Exception($"Dictionary already contains this cell positiojn {pos}"); // Защита: если клетка уже занята, выбрасываем исключение
            placedObjects[pos] = data;
        }
    }

    private List<Vector3Int> CalculatePositions(Vector3Int gridPosition, Vector2Int objectSize) // Вычисляет список клеток, которые займёт объект
    {
        List<Vector3Int> returnVal = new();
        for (int x = 0; x < objectSize.x; x++)
        {
            for (int y = 0; y < objectSize.y; y++)
            {
                returnVal.Add(gridPosition + new Vector3Int(x, 0, y));
            }
        }
        return returnVal;
    }

    public bool CanPlaceObejctAt(Vector3Int gridPosition, Vector2Int objectSize)    // Проверяет, можно ли разместить объект в данной позиции
    {
        List<Vector3Int> positionToOccupy = CalculatePositions(gridPosition, objectSize);
        foreach (var pos in positionToOccupy)   // Если хотя бы одна клетка занята — нельзя ставить
        {
            if (placedObjects.ContainsKey(pos))
                return false;
        }
        return true;
    }

    internal int GetRepresentationIndex(Vector3Int gridPosition)    //Проверка: есть ли объект на клетке
    {                                                               //Если есть → возвращает индекс GameObject, чтобы его удалить или взаимодействовать
        if (placedObjects.ContainsKey(gridPosition) == false)       //Если нет → возвращает -1 (нет объекта)
            return -1;                                              
        return placedObjects[gridPosition].PlacedObjectIndex;
    }

    internal void RemoveObjectAt(Vector3Int gridPosition)   // Удаляет объект из всех клеток, которые он занимает
    {
        foreach (var pos in placedObjects[gridPosition].occupiedPositions)
        {
            placedObjects.Remove(pos);
        }
    }
}

public class PlacementData  // Данные о размещённом объекте
{
    public List<Vector3Int> occupiedPositions; // Список всех клеток, которые занимает объект
    public int ID { get; private set; }
    public int PlacedObjectIndex { get; private set; }

    public PlacementData(List<Vector3Int> occupiedPositions, int iD, int placedObjectIndex)
    {
        this.occupiedPositions = occupiedPositions;
        ID = iD;
        PlacedObjectIndex = placedObjectIndex;
    }
}