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

    private Coroutine spawnRoutine;

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
                GameObject enemy = Instantiate(entry.prefab, origin.position, Quaternion.identity);
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
