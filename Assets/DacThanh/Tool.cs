using UnityEngine;

public class Tool : MonoBehaviour
{
    public GameObject owner;
    JobData toolData;
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
        Person[] villagers = FindObjectsOfType<Person>();

        float minDistance = Mathf.Infinity;
        Person nearestVillager = null;

        foreach (Person villager in villagers)
        {
            if (villager == null) return;
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
            Person villager = other.GetComponent<Person>();

            if (villager != null)
            {
                villager.hasTool = true;
                villager.hasMoveCommand = false;
                Destroy(gameObject);
            }
        }
    }
}