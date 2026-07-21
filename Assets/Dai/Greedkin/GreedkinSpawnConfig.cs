using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TinyKingdom/Enemies/Greedkin Spawn Config", fileName = "GreedkinSpawnConfig")]
public sealed class GreedkinSpawnConfig : ScriptableObject
{
    public List<GreedkinSpawnEntry> entries = new List<GreedkinSpawnEntry>();
}

[Serializable]
public sealed class GreedkinSpawnEntry
{
    [Tooltip("A label for this controlled spawn entry; it does not affect gameplay.")]
    public string label;
    public GameObject prefab;
    [Min(0)] public int count = 1;
    [Min(0f)] public float startDelay;
    [Min(0f)] public float spawnInterval = 0.75f;
}
