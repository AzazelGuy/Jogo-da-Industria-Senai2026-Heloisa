using System.Collections.Generic;
using UnityEngine;

public class IdentifiyerEncaixe : MonoBehaviour
{
    [SerializeField] private string myID;
    [SerializeField] private MeshFilter usedModel;
    [SerializeField] private Tipo Tipo = Tipo.CPU;

    [Header("Conectores & Parafusos")]
    [SerializeField] public List<Transform> ScrewsPositions = new List<Transform>();
    [SerializeField] public List<bool> CompletedScrews = new List<bool>();

    [Header("Minigame / Interação")]
    public bool hasScrewMiniStep = true;

    private void Start()
    {
        UpdateModel(null);
    }

    public void UpdateModel(Mesh NewModel)
    {
        if (usedModel != null)
        {
            usedModel.mesh = NewModel;
        }
    }

    // Recebe a confirmação de que um conector/parafuso específico foi colocado
    public void NotifyConnectorPlaced(int index)
    {
        if (index >= 0 && index < CompletedScrews.Count)
        {
            CompletedScrews[index] = true;

            // Notifica o StepChecker sobre o progresso dos conectores
            if (StepChecker.Instance != null)
            {
                StepChecker.Instance.CheckConnectorProgress(this);
            }
        }
    }

    public int GetCompletedCount()
    {
        int count = 0;
        foreach (bool completed in CompletedScrews)
        {
            if (completed) count++;
        }
        return count;
    }

    public bool IsFullyAssembled()
    {
        return GetCompletedCount() == CompletedScrews.Count && CompletedScrews.Count > 0;
    }

    public string getID => myID;
    public bool hasScrew => hasScrewMiniStep;
}