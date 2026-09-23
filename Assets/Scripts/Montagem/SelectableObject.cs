using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SelectableObject : MonoBehaviour, ISelectable
{
    [Header("Outline Visuals")]
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField] private float outlineScale = 1.05f; // Scale multiplier (5% larger)

    private GameObject outlineInstance;

    private void Awake()
    {
        CreateOutlineShell();
    }

    private void CreateOutlineShell()
    {
        MeshFilter sourceFilter = GetComponent<MeshFilter>();
        MeshRenderer sourceRenderer = GetComponent<MeshRenderer>();

        if (sourceFilter == null || sourceRenderer == null) return;

        // Canseira mas vamos lá, criamos um objeto filho para ser nosso contorno
        outlineInstance = new GameObject("OutlineShell");
        outlineInstance.transform.SetParent(transform, false);
        outlineInstance.transform.localPosition = Vector3.zero;
        outlineInstance.transform.localRotation = Quaternion.identity;
        outlineInstance.transform.localScale = Vector3.one * outlineScale;

        // Copiamos a mesha no contorno
        MeshFilter outlineFilter = outlineInstance.AddComponent<MeshFilter>();
        outlineFilter.sharedMesh = sourceFilter.sharedMesh;

        // Cria um material Unlit(não afetado por luz) já incluso na unity URP
        MeshRenderer outlineRenderer = outlineInstance.AddComponent<MeshRenderer>();
        Material outlineMaterial = new Material(Shader.Find("Unlit/Color"));
        outlineMaterial.color = outlineColor;

        // Renderiza atrás do original
        outlineRenderer.material = outlineMaterial;

        // Esconde o contorno por padrão.
        outlineInstance.SetActive(false);
    }

    public void OnSelect()
    {
        Debug.Log($"Selected: {gameObject.name}");
        if (outlineInstance != null)
        {
            outlineInstance.SetActive(true);
        }
    }

    public void OnDeselect()
    {
        Debug.Log($"Deselected: {gameObject.name}");
        if (outlineInstance != null)
        {
            outlineInstance.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Limpa o material feito para não causar problemas de memória
        if (outlineInstance != null)
        {
            MeshRenderer mr = outlineInstance.GetComponent<MeshRenderer>();
            if (mr != null && mr.material != null)
            {
                Destroy(mr.material);
            }
        }
    }
}