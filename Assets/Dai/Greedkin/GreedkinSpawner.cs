using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GreedkinSpawner : MonoBehaviour
{
    [SerializeField] private GreedkinSpawnConfig spawnConfig;
    [SerializeField] private GreedkinRoute route;
    [SerializeField] private Transform mainHouse;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField, Min(0f), Tooltip("Distance kept between enemies that are created by the same spawner. The first enemy remains exactly at Spawn Point.")]
    private float spawnSpacing = 0.65f;

    private Coroutine spawnRoutine;
    private int spawnedEnemyCount;

    private void Start()
    {
        if (spawnOnStart)
        {
            StartSpawning();
        }
    }

    [ContextMenu("Start Spawning")]
    public void StartSpawning()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
        }
        spawnRoutine = StartCoroutine(SpawnRoutine());
    }

    [ContextMenu("Stop Spawning")]
    public void StopSpawning()
    {
        if (spawnRoutine == null)
        {
            return;
        }
        StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    private IEnumerator SpawnRoutine()
    {
        if (spawnConfig == null || route == null || mainHouse == null)
        {
            Debug.LogWarning("GreedkinSpawner needs a Spawn Config, Route, and Main House reference.", this);
            yield break;
        }

        Transform origin = spawnPoint == null ? transform : spawnPoint;
        spawnedEnemyCount = 0;
        foreach (GreedkinSpawnEntry entry in spawnConfig.entries)
        {
            if (entry == null || entry.prefab == null || entry.count <= 0)
            {
                continue;
            }

            if (entry.startDelay > 0f)
            {
                yield return new WaitForSeconds(entry.startDelay);
            }

            for (int index = 0; index < entry.count; index++)
            {
                // Keep a deterministic queue behind the spawn point instead of
                // placing every configured enemy on top of the same sprite.
                Vector3 spawnPosition = origin.position + Vector3.left * (spawnedEnemyCount * spawnSpacing);
                GameObject enemy = Instantiate(entry.prefab, spawnPosition, Quaternion.identity);
                spawnedEnemyCount++;
                GreedkinEnemyBrain brain = enemy.GetComponent<GreedkinEnemyBrain>();
                if (brain == null)
                {
                    Debug.LogWarning($"Spawned prefab '{entry.prefab.name}' has no GreedkinEnemyBrain.", enemy);
                }
                else
                {
                    brain.Configure(route, mainHouse);
                }

                if (entry.spawnInterval > 0f && index < entry.count - 1)
                {
                    yield return new WaitForSeconds(entry.spawnInterval);
                }
            }
        }

        spawnRoutine = null;
    }
}
