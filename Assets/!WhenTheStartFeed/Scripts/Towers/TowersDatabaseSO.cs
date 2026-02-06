using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TowersDatabaseSO", menuName = "Scriptable Objects/TowersDatabaseSO")]
public class TowersDatabaseSO : ScriptableObject
{
    public List<TowersData> objectsData;
}

[Serializable]
public class TowersData
{
    [field:SerializeField] 
    public string Name {get; private set; }
    [field:SerializeField] 
    public int ID {get; private set; }
    [field:SerializeField] 
    public Vector2Int Size {get; private set; } = Vector2Int.one;
    [field:SerializeField] 
    public GameObject Prefab {get; private set; }
}