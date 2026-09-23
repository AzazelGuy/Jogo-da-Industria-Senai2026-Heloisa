using UnityEngine;

public class TesteInventario : MonoBehaviour
{
    [Header("Configure a Peça de Teste")]
    public PieceData pecaParaTestar;

    private void Update()
    {
        // Pressione T para simular o recebimento/coleta da peça
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (InventarioManager.Instance != null)
            {
                InventarioManager.Instance.AdicionarPeca(pecaParaTestar);
                Debug.Log("Peça de teste enviada ao estoque!");
            }
        }
    }
}