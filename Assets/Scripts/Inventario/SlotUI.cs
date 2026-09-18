using UnityEngine;
using UnityEngine.UI;

public class SlotUI : MonoBehaviour
{
    public Image iconeImagem;
    private PieceData pecaAtual;

    public void ConfigurarSlot(PieceData peca)
    {
        pecaAtual = peca;
        if (iconeImagem != null && pecaAtual != null)
        {
            iconeImagem.sprite = pecaAtual.iconePeca;
            iconeImagem.gameObject.SetActive(true);
        }
    }

    public void LimparSlot()
    {
        pecaAtual = null;
        if (iconeImagem != null)
        {
            iconeImagem.sprite = null;
            iconeImagem.gameObject.SetActive(false);
        }
    }

    // Função executada ao clicar no Slot do inventário
    public void AoClicarNoSlot()
    {
        if (pecaAtual != null && SpecsPecas.Instance != null)
        {
            SpecsPecas.Instance.ExibirFicha(pecaAtual);
        }
        else
        {
            Debug.LogWarning("Nenhuma peça associada a este Slot ou SpecsPecas não encontrado!");
        }
    }
}