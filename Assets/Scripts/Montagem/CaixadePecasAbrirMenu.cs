using UnityEngine;

/// <summary>
/// Item do cenário (a caixa física na bancada) que abre o menu da Caixa de Peças ao ser clicado.
/// O objeto precisa de um Collider numa layer incluída no "selectableLayer" do SelectionManager.
/// </summary>
public class CaixadePecasAbrirMenu : MonoBehaviour, ISelectable
{
    #region Campos Serializados

    [Tooltip("Se vazio, usa a CaixaPecasUi que existir na cena.")]
    [SerializeField] private CaixaPecasUi caixaUi;

    #endregion

    #region Implementação de ISelectable

    public void OnSelect()
    {
        if (caixaUi == null) caixaUi = CaixaPecasUi.Instance;

        if (caixaUi == null)
        {
            Debug.LogWarning("[CaixadePecasAbrirMenu] Nenhuma CaixaPecasUi encontrada na cena!", this);
            return;
        }

        caixaUi.AbrirCaixa();
    }

    #endregion
}
