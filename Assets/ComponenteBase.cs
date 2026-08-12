using UnityEngine;

public class ComponenteBase : MonoBehaviour, ISelectable
{
    [SerializeField] private Animator anim;

    private Vector3 OriginalPosition;

    [HideInInspector]public enum State
    {
        Normal,
        Selected
    }

    [SerializeField] private string expectedID;

    [SerializeField ] private State cur_state;

    [SerializeField] private MeshFilter myModel;

    private void Awake()
    {
        if (anim == null)
        {
            anim = GetComponent<Animator>();
        }
        OriginalPosition = transform.position;
    }

    private void Update()
    {
        if (cur_state == State.Normal)
        {
            transform.position = OriginalPosition;
        }
        else if (cur_state == State.Selected)
        {
            transform.position = Input.mousePosition;
        }
    }

    public void OnSelect()
    {
        Debug.Log($"Selected: {gameObject.name}");
        if (anim != null && cur_state != State.Selected)
        {
            anim.SetTrigger("OnSelect");
            cur_state = State.Selected;

            GameManager.Instance.SelectObject(gameObject);
            IdentifiyerEncaixe[] slots = FindObjectsByType<IdentifiyerEncaixe>(FindObjectsSortMode.None);

            foreach (IdentifiyerEncaixe slot in slots)
            {
                if (slot.getID == expectedID)
                {
                    slot.UpdateModel(myModel.mesh);
                    break;
                }
            }
        }
    }

    public void OnDeselect()
    {
        Debug.Log($"Selected: {gameObject.name}");
        if (anim != null)
        {
            anim.SetTrigger("OnDeselect");
            cur_state = State.Normal;

            IdentifiyerEncaixe[] slots = FindObjectsByType<IdentifiyerEncaixe>(FindObjectsSortMode.None);

            foreach (IdentifiyerEncaixe slot in slots)
            {
                if (slot.getID == expectedID)
                {
                    slot.UpdateModel(null);
                    break;
                }
            }
        }
    }

    public string myID => expectedID;
}
