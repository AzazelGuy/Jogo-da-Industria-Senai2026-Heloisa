using UnityEngine;

public class EncaixeBase : MonoBehaviour
{
    [SerializeField] private IdentifiyerEncaixe identifiyer;
    [SerializeField] private GameObject ScrewPrefab;

    private void Start()
    {
        if (identifiyer == null)
        {
            identifiyer = GetComponent<IdentifiyerEncaixe>();
        }
    }

    /// <summary>
    /// Instancia os conectores/parafusos e os vincula a este encaixe.
    /// </summary>
    public void MiniGameScrew()
    {
        if (identifiyer == null || ScrewPrefab == null) return;

        // Limpa e prepara a lista de verificação se estiver vazia ou desalinhada
        if (identifiyer.CompletedScrews.Count != identifiyer.ScrewsPositions.Count)
        {
            identifiyer.CompletedScrews.Clear();
            for (int i = 0; i < identifiyer.ScrewsPositions.Count; i++)
            {
                identifiyer.CompletedScrews.Add(false);
            }
        }

        // Instancia os conectores passando os alvos e seus respectivos índices
        for (int i = 0; i < identifiyer.ScrewsPositions.Count; i++)
        {
            Transform posAlvo = identifiyer.ScrewsPositions[i];
            GameObject objConector = Instantiate(ScrewPrefab);

            // Posiciona um pouco acima do slot inicial
            objConector.transform.position = posAlvo.position + new Vector3(0f, 1.5f, 0f);

            // Configura a referência no componente Conector
            if (objConector.TryGetComponent<Connector>(out var conector))
            {
                conector.SetTarget(posAlvo.position, posAlvo.eulerAngles);
                conector.SetPlacement(this, i);

                // Associa também a referência do Identifiyer
                conector.SetPlacement(identifiyer, i);
            }
        }
    }

    /// Notificação recebida do Conector ao ser totalmente encaixado.
    /// Índice do conector na lista de posições.</param
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
}