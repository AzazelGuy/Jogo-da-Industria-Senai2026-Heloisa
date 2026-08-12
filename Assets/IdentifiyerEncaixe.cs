using UnityEngine;

public class IdentifiyerEncaixe : MonoBehaviour
{
    [SerializeField] private string myID;
    [SerializeField] private MeshFilter usedModel;

    public void UpdateModel(Mesh NewModel)
    {
        usedModel.mesh = NewModel;
    }

    public string getID => myID;


}
