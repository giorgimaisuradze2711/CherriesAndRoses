using UnityEngine;
using System.Collections.Generic;

public class CameraObstructionFade : MonoBehaviour
{
    public Transform target;
    public LayerMask obstructionMask;

    [Header("Fade Material")]
    public Material fadeMaterial;

    private class ObstructedObject
    {
        public Renderer renderer;
        public Material[] originalMaterials;
    }

    private List<ObstructedObject> fadedObjects = new List<ObstructedObject>();

    void LateUpdate()
    {
        RestoreFadedObjects();

        Vector3 direction = target.position - transform.position;
        float distance = Vector3.Distance(transform.position, target.position);

        RaycastHit[] hits = Physics.RaycastAll(
            transform.position,
            direction,
            distance,
            obstructionMask
        );

        foreach (RaycastHit hit in hits)
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();

            if (rend != null)
            {
                FadeObject(rend);
            }
        }
    }

    void FadeObject(Renderer rend)
    {
        // Already faded
        foreach (var obj in fadedObjects)
        {
            if (obj.renderer == rend)
                return;
        }

        ObstructedObject obstructed = new ObstructedObject();
        obstructed.renderer = rend;

        // Save original materials
        obstructed.originalMaterials = rend.materials;

        fadedObjects.Add(obstructed);

        // Create array filled with fade material
        Material[] fadeMats = new Material[rend.materials.Length];

        for (int i = 0; i < fadeMats.Length; i++)
        {
            // Create instance so alpha changes don't affect all objects
            Material instance = new Material(fadeMaterial);

            Color c = instance.color;
            c.a = 0.6f;
            instance.color = c;

            fadeMats[i] = instance;
        }

        // Apply fade material
        rend.materials = fadeMats;
    }

    void RestoreFadedObjects()
    {
        foreach (var obj in fadedObjects)
        {
            if (obj.renderer != null)
            {
                // Restore original materials
                obj.renderer.materials = obj.originalMaterials;
            }
        }

        fadedObjects.Clear();
    }
}