using UnityEngine;

public enum JobType
{
    Person,
    Villager,
    Archer,
    Spearman
    // Thêm job mới ở đây trong tương lai
}
[CreateAssetMenu(fileName = "JobData", menuName = "Game/Job Data")]
public class JobData : ScriptableObject
{
    [Header("Identity")]
    public JobType jobType = JobType.Person;
    public string jobName;
    public Sprite jobIcon;
    [Header("Movement")]
    public float speed = 3f;
}
// JobData toolData;
// private void Start()
// {
//     FindVillager();
// }
//
// private void Update()
// {
//     if (owner == null)
//     {
//         FindVillager();
//     }
// }
//
// private void FindVillager()
// {
//     Person[] villagers = FindObjectsOfType<Person>();
//
//     float minDistance = Mathf.Infinity;
//     Person nearestVillager = null;
//
//     foreach (Person villager in villagers)
//     {
//         if (villager == null) return;
//         if (villager.hasTool)
//             continue;
//
//         float distance = Vector2.Distance(
//             transform.position,
//             villager.transform.position);
//
//         if (distance < minDistance)
//         {
//             minDistance = distance;
//             nearestVillager = villager;
//         }
//     }
//
//     if (nearestVillager != null)
//     {
//         owner = nearestVillager.gameObject;
//         nearestVillager.TargetPosition = transform.position;
//         nearestVillager.hasMoveCommand = true;
//     }
// }