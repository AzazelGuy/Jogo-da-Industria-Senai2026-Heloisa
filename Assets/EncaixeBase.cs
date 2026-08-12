using UnityEngine;

public class EncaixeBase : MonoBehaviour, ISelectable
{
    [SerializeField]IdentifiyerEncaixe identifiyer;

    private void Start()
    {
        if (identifiyer == null)
        {
            identifiyer = GetComponent<IdentifiyerEncaixe>();
        }
    }
    public void OnSelect()
    {
        ComponenteBase componenteBase = GameManager.Instance.GetSelectedObject.GetComponent<ComponenteBase>();

        if (componenteBase != null && componenteBase.myID == identifiyer.getID)
        {
            componenteBase.gameObject.transform.position = transform.position;
            GameManager.Instance.ClearObject();
            OnDeselect();
        }
    }

    public void OnDeselect()
    {

    }
}
