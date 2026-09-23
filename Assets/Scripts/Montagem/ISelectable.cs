using UnityEngine;

/// <summary>
/// Contrato para objetos interativos no jogo que respondem a ações do jogador
/// (seleção, duplo clique, hold e hover). Implementado por peças, conectores, etc.
/// </summary>
public interface ISelectable
{
    #region Seleção

    /// <summary>
    /// Chamado quando o objeto é selecionado.
    /// </summary>
    void OnSelect() { }

    /// <summary>
    /// Chamado quando a seleção do objeto é removida.
    /// </summary>
    void OnDeselect() { }

    #endregion

    #region Cliques

    /// <summary>
    /// Chamado quando o objeto recebe um duplo clique.
    /// </summary>
    void OnDoubleClick() { }

    /// <summary>
    /// Chamado enquanto o botão de seleção é mantido pressionado sobre o objeto.
    /// </summary>
    void OnHold() { }

    #endregion

    #region Hover (Ponteiro do Mouse)

    // Métodos para detecção de passagem do mouse (Hover)
    void OnPointerEnter() { }
    void OnPointerExit() { }

    #endregion
}
