using UnityEngine;

/// <summary>
/// Representa um ponto de encaixe físico na cena que possui um IdentifiyerEncaixe associado.
/// Responsável por instanciar os parafusos/conectores (minigame) quando a peça é colocada
/// e por repassar a notificação de conclusão ao IdentifiyerEncaixe/StepChecker.
/// </summary>
public class EncaixeBaseMontagem : MonoBehaviour
{
    #region Campos Serializados

    [SerializeField] private IdentifiyerEncaixe identifiyer;
    [SerializeField] private GameObject ScrewPrefab;

    #endregion

    #region Ciclo de Vida (Unity)

    private void Start()
    {
        ResolveIdentifier();

        if (identifiyer != null)
        {
            identifiyer.gameObject.SetActive(true);
        }
    }

    #endregion

    #region Resolução de Referências

    /// <summary>
    /// Garante que o campo "identifiyer" esteja preenchido, procurando no próprio objeto,
    /// nos pais e nos filhos, nessa ordem.
    /// </summary>
    private void ResolveIdentifier()
    {
        if (identifiyer != null) return;

        identifiyer = GetComponent<IdentifiyerEncaixe>();
        if (identifiyer == null)
        {
            identifiyer = GetComponentInParent<IdentifiyerEncaixe>();
        }
        if (identifiyer == null)
        {
            identifiyer = GetComponentInChildren<IdentifiyerEncaixe>();
        }
    }

    #endregion

    #region Minigame de Parafusos

    /// <summary>
    /// Instancia os conectores/parafusos nas posições configuradas em ScrewsPositions
    /// e os vincula a este encaixe, para que o jogador precise encaixá-los manualmente.
    /// </summary>
    public void MiniGameScrew()
    {
        ResolveIdentifier();

        if (identifiyer == null)
        {
            Debug.LogError($"[EncaixeBaseMontagem] Não foi possível localizar um IdentifiyerEncaixe em '{gameObject.name}'.");
            return;
        }

        if (ScrewPrefab == null)
        {
            Debug.LogError($"[EncaixeBaseMontagem] ScrewPrefab não foi atribuído em '{gameObject.name}'.");
            return;
        }

        if (identifiyer.ScrewsPositions == null || identifiyer.ScrewsPositions.Count == 0)
        {
            Debug.LogWarning($"[EncaixeBaseMontagem] '{identifiyer.name}' não tem ScrewsPositions configurados.");
            return;
        }

        Debug.Log("Tentou Spawnar");

        // Reinicializa a lista de progresso caso o tamanho não bata com o número de posições
        if (identifiyer.CompletedScrews.Count != identifiyer.ScrewsPositions.Count)
        {
            identifiyer.CompletedScrews.Clear();
            for (int i = 0; i < identifiyer.ScrewsPositions.Count; i++)
            {
                identifiyer.CompletedScrews.Add(false);
            }
        }

        // Instancia um conector para cada posição configurada, começando um pouco acima do alvo
        for (int i = 0; i < identifiyer.ScrewsPositions.Count; i++)
        {
            Transform posAlvo = identifiyer.ScrewsPositions[i];
            if (posAlvo == null) continue;

            GameObject objConector = Instantiate(ScrewPrefab);
            objConector.transform.position = posAlvo.position + new Vector3(0f, 1.5f, 0f);

            if (objConector.TryGetComponent<ConnectorMontagem>(out var conector))
            {
                conector.SetTarget(posAlvo.position, posAlvo.eulerAngles);
                conector.SetPlacement(identifiyer, i);
            }
        }

        Debug.Log("Terminou de spawnar");
    }

    #endregion

    #region Notificações de Progresso

    /// <summary>
    /// Notificação recebida do Conector ao ser totalmente encaixado.
    /// </summary>
    /// <param name="index">Índice do conector na lista de posições.</param>
    public void OnConectorPlaced(int index)
    {
        if (identifiyer != null)
        {
            identifiyer.NotifyConnectorPlaced(index);
        }

        // Notifica o StepChecker para validar o avanço de etapa
        if (StepChecker.Instance != null && identifiyer != null)
        {
            StepChecker.Instance.CheckConnectorProgress(identifiyer);
        }
    }

    #endregion
}
