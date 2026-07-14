using UnityEngine;

public class Tool : MonoBehaviour
{
    public GameObject owner;

    private void Start()
    {
        FindVillager();
    }

    private void Update()
    {
        if (owner == null)
        {
            FindVillager();
        }
    }

    private void FindVillager()
    {
        Villager[] villagers = FindObjectsOfType<Villager>();

        float minDistance = Mathf.Infinity;
        Villager nearestVillager = null;

        foreach (Villager villager in villagers)
        {
            if (villager.hasTool)
                continue;

            float distance = Vector2.Distance(
                transform.position,
                villager.transform.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestVillager = villager;
            }
        }

        if (nearestVillager != null)
        {
            owner = nearestVillager.gameObject;
            nearestVillager.TargetPosition = transform.position;
            nearestVillager.hasMoveCommand = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Villager villager = other.GetComponent<Villager>();

            if (villager != null)
            {
                villager.hasTool = true;
                villager.hasMoveCommand = false;
                Destroy(gameObject);
            }
        }
    }
}