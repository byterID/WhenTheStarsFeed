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
    [field: SerializeField] public string enemyName { get; private set; }
    [field: SerializeField] public int ID { get; private set; }
    [field: SerializeField] public float health { get; private set; }
    [field: SerializeField] public float resistance { get; private set; }
    [field: SerializeField] public float moveSpeed { get; private set; }
    [field: SerializeField] public GameObject prefab { get; private set; }
    [field: SerializeField] public int minWave { get; private set; } // с какой волны может появляться
    [field: SerializeField] public int maxWave { get; private set; } // до какой волны
}
