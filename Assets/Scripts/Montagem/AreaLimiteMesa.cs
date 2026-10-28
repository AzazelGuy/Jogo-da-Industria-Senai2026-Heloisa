using UnityEngine;

/// <summary>
/// Define a área (em X e Z) onde as peças SOLTAS podem ficar na mesa.
/// Peças na mão do jogador não são afetadas: elas podem sair da área livremente.
/// Ao serem soltas fora dela, o ComponenteBaseMontagem as empurra de volta para a borda.
///
/// COMO USAR: crie um objeto vazio sobre a mesa, adicione este script (ele cria o BoxCollider)
/// e ajuste o tamanho do BoxCollider. Não rotacione o objeto (a área usa o bounds alinhado aos eixos)
/// e deixe-o numa layer que NÃO esteja no "selectableLayer" do SelectionManager.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class AreaLimiteMesa : MonoBehaviour
{
    #region Singleton

    public static AreaLimiteMesa Instance { get; private set; }

    #endregion

    #region Campos Serializados

    [SerializeField] private BoxCollider area;

    #endregion

    #region Ciclo de Vida (Unity)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (area == null) area = GetComponent<BoxCollider>();
        area.isTrigger = true; // só marca a área, não bloqueia a física
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnDrawGizmos()
    {
        BoxCollider box = area != null ? area : GetComponent<BoxCollider>();
        if (box == null) return;

        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.9f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
    }

    #endregion

    #region API Pública

    /// <summary>
    /// Devolve a posição "presa" dentro da área (só X e Z; o Y fica por conta da gravidade).
    /// </summary>
    public Vector3 Limitar(Vector3 posicao)
    {
        if (area == null) return posicao;

        Bounds b = area.bounds;
        posicao.x = Mathf.Clamp(posicao.x, b.min.x, b.max.x);
        posicao.z = Mathf.Clamp(posicao.z, b.min.z, b.max.z);
        return posicao;
    }

    /// <summary>
    /// A posição (em X e Z) está dentro da área?
    /// </summary>
    public bool EstaDentro(Vector3 posicao)
    {
        return Limitar(posicao) == posicao;
    }

    #endregion
}
