using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemiesDatabaseSO", menuName = "Scriptable Objects/EnemiesDatabaseSO")]
public class EnemiesDatabaseSO : ScriptableObject
{
    public List<EnemiesData> enemiesData;
}

[Serializable]
public class EnemiesData
{
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public int ID { get; private set; }
    [field: SerializeField] public float HP { get; private set; }
    [field: SerializeField] public float MoveSpeed { get; private set; }
    [field: SerializeField] public GameObject Prefab { get; private set; }
}
