using SeriousGame.Hardware;
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

    public void MiniGameScrew()
    {
        foreach (Transform s in identifiyer.ScrewsPositions)
        {
            GameObject screw = Instantiate(ScrewPrefab);

            screw.transform.position = s.position + new Vector3(0f, 1.5f, 0);
            screw.GetComponent<Screw>().SetTarget(s.position);
        }
    }
}