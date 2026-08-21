using UnityEngine;

/// <summary>
/// Contrato para objetos interativos no jogo que respondem a ações do jogador.
/// </summary>
public interface ISelectable
{
    /// <summary>
    /// Chamado quando o objeto é selecionado.
    /// </summary>
    void OnSelect() { }

    /// <summary>
    /// Chamado quando a seleção do objeto é removida.
    /// </summary>
    void OnDeselect() { }

    /// <summary>
    /// Chamado quando o objeto recebe um duplo clique.
    /// </summary>
    void OnDoubleClick() { }

    /// <summary>
    /// Chamado enquanto o botão de seleção é mantido pressionado sobre o objeto.
    /// </summary>
    void OnHold() { }
}