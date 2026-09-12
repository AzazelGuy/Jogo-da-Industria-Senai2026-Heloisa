using System.Collections.Generic;
using UnityEngine;

public class IdentifiyerEncaixe : MonoBehaviour
{
    [SerializeField] private string myID;
    [SerializeField] private MeshFilter usedModel;
    [SerializeField] private Tipo Tipo = Tipo.CPU;

    [Header("Compatibilidade de Pe�a")]
    [SerializeField] private List<string> acceptedPieceIDs = new List<string>();
    [SerializeField] private List<Tipo> acceptedTypes = new List<Tipo>();

    [Header("Conectores & Parafusos")]
    [SerializeField] public List<Transform> ScrewsPositions = new List<Transform>();
    [SerializeField] public List<bool> CompletedScrews = new List<bool>();

    [Header("Minigame / Intera��o")]
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

    // Recebe a confirma��o de que um conector/parafuso espec�fico foi colocado
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

        if (IsFullyAssembled())
        {
            CameraController.instance.ReturnToOverview();
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

    public void SetSocketVisible(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            if (collider != null)
            {
                collider.enabled = visible;
            }
        }
    }

    public bool CanAcceptPiece(ComponenteBase piece)
    {
        if (piece == null) return false;

        string pieceId = piece.myID;
        if (!string.IsNullOrEmpty(pieceId) && acceptedPieceIDs.Contains(pieceId))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(pieceId) && !string.IsNullOrEmpty(myID) && pieceId == myID)
        {
            return true;
        }

        if (piece.GetComponent<ComponenteBase>() == null)
        {
            return false;
        }

        SOPieceData data = piece.GetComponent<ComponenteBase>().GetComponent<ComponenteBase>() == null ? null : null;

        if (data != null)
        {
            if (acceptedPieceIDs.Contains(data.ID))
            {
                return true;
            }
        }

        if (acceptedTypes.Count == 0)
        {
            return string.IsNullOrEmpty(myID) == false && myID == pieceId;
        }

        return acceptedTypes.Contains(Tipo);
    }

    public bool CanAcceptPieceId(string pieceId)
    {
        if (string.IsNullOrEmpty(pieceId)) return false;
        if (acceptedPieceIDs.Contains(pieceId)) return true;
        if (!string.IsNullOrEmpty(myID) && myID == pieceId) return true;
        return false;
    }

    public void AddAcceptedPieceId(string pieceId)
    {
        if (string.IsNullOrEmpty(pieceId) || acceptedPieceIDs.Contains(pieceId)) return;
        acceptedPieceIDs.Add(pieceId);
    }

    public void AddAcceptedType(Tipo type)
    {
        if (!acceptedTypes.Contains(type))
        {
            acceptedTypes.Add(type);
        }
    }

    public List<string> AcceptedPieceIDs => acceptedPieceIDs;
    public List<Tipo> AcceptedTypes => acceptedTypes;
    public string getID => myID;
    public bool hasScrew => hasScrewMiniStep;
}