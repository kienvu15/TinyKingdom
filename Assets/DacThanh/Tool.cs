using UnityEngine;

public class Tool : MonoBehaviour
{
    public GameObject owner;
    public ToolData toolData;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // if (owner != null) return;
            Person villager = other.GetComponent<Person>();

            if (villager != null)
            {
                villager.hasTool = true;
                villager.hasMoveCommand = false;
                Debug.Log("Old Job: " + villager.jobType);

                villager.jobType = toolData.resultingJob.jobType;
                villager.GetComponent<SpriteRenderer>().sprite = toolData.resultingJob.jobIcon;
                Debug.Log("New Job: " + villager.jobType);
                transform.SetParent(villager.transform);
                transform.localPosition = new Vector2(0,0);
            }
        }
    }
}