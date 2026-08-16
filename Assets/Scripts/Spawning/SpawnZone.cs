using UnityEngine;

// Marks a named, axis-aligned area in the scene that ObjectSpawner can drop prefabs into.
public class SpawnZone : MonoBehaviour
{
    [SerializeField] private string zoneName;
    [SerializeField] private Vector3 size = new Vector3(10f, 1f, 10f);

    public string ZoneName => zoneName;

    public Vector3 GetRandomPoint()
    {
        Vector3 halfExtents = size * 0.5f;
        Vector3 offset = new Vector3(
            Random.Range(-halfExtents.x, halfExtents.x),
            Random.Range(-halfExtents.y, halfExtents.y),
            Random.Range(-halfExtents.z, halfExtents.z));

        return transform.position + offset;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, size);
    }
}
