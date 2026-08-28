using UnityEngine;

// Marks a predefined, designer-placed location where NetworkBootstrap spawns one
// per-player Holder. Purely a scene marker (position/rotation + gizmo) - mirrors SpawnZone.
public class HolderSpawnPoint : MonoBehaviour
{
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
