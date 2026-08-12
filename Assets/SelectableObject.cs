using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SelectableObject : MonoBehaviour, ISelectable
{
    private Renderer meshRenderer;
    private Color originalColor;

    private void Awake()
    {
        meshRenderer = GetComponent<Renderer>();
        if (meshRenderer != null)
        {
            originalColor = meshRenderer.material.color;
        }
    }

    public void OnSelect()
    {
        Debug.Log($"Selected: {gameObject.name}");
        if (meshRenderer != null)
        {
            meshRenderer.material.color = Color.yellow; // Highlight visual feedback
        }
    }

    public void OnDeselect()
    {
        Debug.Log($"Deselected: {gameObject.name}");
        if (meshRenderer != null)
        {
            meshRenderer.material.color = originalColor;
        }
    }
}