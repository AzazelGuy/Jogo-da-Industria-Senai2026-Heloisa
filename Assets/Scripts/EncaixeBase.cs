using UnityEngine;

public class EncaixeBase : MonoBehaviour
{
    [SerializeField] private IdentifiyerEncaixe identifiyer;
    [SerializeField] private GameObject ScrewPrefab;

    private void Start()
    {
        ResolveIdentifier();

        if (identifiyer != null)
        {
            identifiyer.gameObject.SetActive(true);
        }
    }

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

    /// <summary>
    /// Instancia os conectores/parafusos e os vincula a este encaixe.
    /// </summary>
    public void MiniGameScrew()
    {
        ResolveIdentifier();

        if (identifiyer == null)
        {
            Debug.LogError($"[EncaixeBase] Não foi possível localizar um IdentifiyerEncaixe em '{gameObject.name}'.");
            return;
        }

        if (ScrewPrefab == null)
        {
            Debug.LogError($"[EncaixeBase] ScrewPrefab não foi atribuído em '{gameObject.name}'.");
            return;
        }

        if (identifiyer.ScrewsPositions == null || identifiyer.ScrewsPositions.Count == 0)
        {
            Debug.LogWarning($"[EncaixeBase] '{identifiyer.name}' não tem ScrewsPositions configurados.");
            return;
        }

        Debug.Log("Tentou Spawnar");

        if (identifiyer.CompletedScrews.Count != identifiyer.ScrewsPositions.Count)
        {
            identifiyer.CompletedScrews.Clear();
            for (int i = 0; i < identifiyer.ScrewsPositions.Count; i++)
            {
                identifiyer.CompletedScrews.Add(false);
            }
        }

        for (int i = 0; i < identifiyer.ScrewsPositions.Count; i++)
        {
            Transform posAlvo = identifiyer.ScrewsPositions[i];
            if (posAlvo == null) continue;

            GameObject objConector = Instantiate(ScrewPrefab);
            objConector.transform.position = posAlvo.position + new Vector3(0f, 1.5f, 0f);

            if (objConector.TryGetComponent<Connector>(out var conector))
            {
                conector.SetTarget(posAlvo.position, posAlvo.eulerAngles);
                conector.SetPlacement(identifiyer, i);
            }
        }

        Debug.Log("Terminou de spawnar");
    }

    /// Notifica��o recebida do Conector ao ser totalmente encaixado.
    /// �ndice do conector na lista de posi��es.</param
    public void OnConectorPlaced(int index)
    {
        if (identifiyer != null)
        {
            identifiyer.NotifyConnectorPlaced(index);
        }

        // Notifica o StepChecker para validar o avan�o de etapa
        if (StepChecker.Instance != null && identifiyer != null)
        {
            StepChecker.Instance.CheckConnectorProgress(identifiyer);
        }
    }
}