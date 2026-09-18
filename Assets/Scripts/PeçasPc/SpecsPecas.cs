using UnityEngine;

public class SpecsPecas : MonoBehaviour
{
    public static SpecsPecas Instance { get; private set; }

    public PieceData pecaAtiva;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ExibirFicha(PieceData peca)
    {
        FecharFichaAtual();
        pecaAtiva = peca;

        if (pecaAtiva != null && pecaAtiva.imagemFichaCanva != null)
        {
            pecaAtiva.imagemFichaCanva.SetActive(true);
        }
    }

    public void FecharFichaAtual()
    {
        if (pecaAtiva != null && pecaAtiva.imagemFichaCanva != null)
        {
            pecaAtiva.imagemFichaCanva.SetActive(false);
        }
        pecaAtiva = null;
    }

    public void ColocarComponenteNoPC()
    {
        if (pecaAtiva == null)
        {
            Debug.LogError("Erro: Nenhuma pecaAtiva selecionada no SpecsPecas!");
            return;
        }

        if (pecaAtiva.modelo3DGabinete != null)
        {
            // Reativa a peça no 3D
            PecaInterativa pecaScript = pecaAtiva.modelo3DGabinete.GetComponent<PecaInterativa>();

            if (pecaScript != null)
            {
                pecaScript.ReativarPecaNoPC();
            }
            else
            {
                pecaAtiva.modelo3DGabinete.SetActive(true);
            }

            // Remove do inventário
            if (InventarioManager.Instance != null)
            {
                InventarioManager.Instance.RemoverPeca(pecaAtiva);
            }

            // Esconde a ficha visual
            FecharFichaAtual();
        }
    }
}