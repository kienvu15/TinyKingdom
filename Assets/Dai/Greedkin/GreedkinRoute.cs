using UnityEngine;

[DisallowMultipleComponent]
public sealed class GreedkinRoute : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;

    public int WaypointCount => waypoints == null ? 0 : waypoints.Length;

    public Transform GetWaypoint(int index)
    {
        return index >= 0 && index < WaypointCount ? waypoints[index] : null;
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null)
        {
            return;
        }

        Gizmos.color = Color.magenta;
        Transform previous = null;
        foreach (Transform waypoint in waypoints)
        {
            if (waypoint == null)
            {
                continue;
            }

            Gizmos.DrawWireSphere(waypoint.position, 0.2f);
            if (previous != null)
            {
                Gizmos.DrawLine(previous.position, waypoint.position);
            }
            previous = waypoint;
        }
    }
}
