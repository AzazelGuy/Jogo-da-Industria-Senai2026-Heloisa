using UnityEngine;

/// <summary>
/// SOLUÇÃO TEMPORÁRIA para ações de montagem que no futuro serão feitas com ferramentas da hotbar
/// (ex.: aplicar pasta térmica no processador). Enquanto a ferramenta não existe, o jogador
/// apenas clica no objeto marcado e a ação é registrada no StepChecker.
///
/// Como usar:
///  1. Crie um objeto filho da peça alvo (ex.: o processador) com um Collider (pode ser trigger).
///     NÃO coloque dentro de um IdentifiyerEncaixe nem na lista LocaisEncaixe da peça,
///     pois esses scripts ligam/desligam os colliders dos filhos.
///  2. Adicione este componente, defina o acaoID (o mesmo que está na lista "acoes" da etapa
///     no StepChecker) e arraste a peça alvo.
///  3. Opcional: indicador (marcador visual enquanto disponível) e resultado visual (ex.: a mancha de pasta).
///
/// Quando a ferramenta da hotbar ficar pronta, basta ela chamar
/// StepChecker.Instance.RegistrarAcao("pasta_termica") e este componente pode ser removido.
/// </summary>
[RequireComponent(typeof(Collider))]
public class AcaoTemporariaMontagem : MonoBehaviour
{
    #region Campos Serializados

    [Header("Ação")]
    [Tooltip("Mesmo ID usado na lista 'acoes' da etapa no StepChecker. Ex.: pasta_termica")]
    [SerializeField] private string acaoID = "pasta_termica";

    [Tooltip("Peça que precisa estar instalada (e parafusada) para a ação ser possível. Ex.: o processador.")]
    [SerializeField] private ComponenteBaseMontagem pecaAlvo;

    [Header("Feedback Visual (opcionais)")]
    [Tooltip("Mostrado apenas enquanto a ação está disponível (ex.: um marcador sobre o processador).")]
    [SerializeField] private GameObject indicadorDisponivel;

    [Tooltip("Ativado quando a ação é concluída (ex.: a mancha de pasta térmica sobre o processador).")]
    [SerializeField] private GameObject resultadoVisual;

    #endregion

    #region Estado Interno

    private Collider col;
    private bool disponivel;
    private bool concluida;

    #endregion

    #region Ciclo de Vida (Unity)

    private void Awake()
    {
        col = GetComponent<Collider>();
        if (resultadoVisual != null) resultadoVisual.SetActive(false);
        AplicarDisponibilidade(false);
    }

    private void Update()
    {
        if (concluida) return;

        bool pode = PodeExecutar();
        if (pode != disponivel)
        {
            AplicarDisponibilidade(pode);
        }
    }

    // Requer um Collider ativo e a câmera principal (usa o sistema de Input antigo, como o resto do projeto).
    private void OnMouseDown()
    {
        if (!disponivel || concluida) return;
        if (PauseMenuActions.instance != null && PauseMenuActions.instance.Paused) return;
        if (StepChecker.Instance == null) return;

        if (StepChecker.Instance.RegistrarAcao(acaoID))
        {
            concluida = true;
            AplicarDisponibilidade(false);

            if (resultadoVisual != null) resultadoVisual.SetActive(true);
        }
    }

    #endregion

    #region Disponibilidade

    /// <summary>
    /// A ação só é possível quando a peça alvo está pronta e é a vez dessa ação na sequência.
    /// </summary>
    private bool PodeExecutar()
    {
        return pecaAlvo != null
            && pecaAlvo.IsFullyAssembledWithScrews
            && StepChecker.Instance != null
            && StepChecker.Instance.AcaoDisponivel(acaoID);
    }

    private void AplicarDisponibilidade(bool valor)
    {
        disponivel = valor;
        col.enabled = valor;

        if (indicadorDisponivel != null)
        {
            indicadorDisponivel.SetActive(valor);
        }
    }

    #endregion
}
