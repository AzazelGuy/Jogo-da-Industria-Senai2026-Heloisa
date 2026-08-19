using System.Collections.Generic;
using UnityEngine;

public class IdentifiyerEncaixe : MonoBehaviour
{
    [SerializeField] private string myID;
    [SerializeField] private MeshFilter usedModel;
    [SerializeField] private Tipo Tipo = Tipo.CPU;
    [SerializeField] public List<Transform> ScrewsPositions = new List<Transform>();
    [SerializeField] public List<bool> CompletedScrews = new List<bool>();

    [Header("Minigame / Interação")]
    [Tooltip("Identificador para ativar a UI de parafusamento ao chegar no foco.")]
    public bool hasScrewMiniStep = true;
    public void UpdateModel(Mesh NewModel)
    {
        usedModel.mesh = NewModel;
    }

    
    public string getID => myID;
    public bool hasScrew => hasScrewMiniStep;

}
