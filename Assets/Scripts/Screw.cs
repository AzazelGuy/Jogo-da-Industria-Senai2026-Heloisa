using UnityEngine;

public class Screw : MonoBehaviour, ISelectable
{
    [SerializeField] private Vector3 desiredPos;
    [SerializeField] private EncaixeBase desiredPlacement;
    [SerializeField] private float insertionSpeed = 1.5f;
    [SerializeField] private float completionDistance = 0.001f;

    private bool isPlaced;

    public void OnSelect() { }
    public void OnDeselect() { }
    public void OnDoubleClick() { }

    public void OnHold()
    {
        if (isPlaced) return;

        // Smooth frame-rate independent movement
        transform.position = Vector3.MoveTowards(
            transform.position,
            desiredPos,
            insertionSpeed * Time.deltaTime
        );

        // Check if fully inserted
        if (Vector3.Distance(transform.position, desiredPos) <= completionDistance)
        {
            LockInPlace();
        }
    }

    private void LockInPlace()
    {
        isPlaced = true;
        transform.position = desiredPos; // Snap precisely

        // 1. Notify the socket slot
        if (desiredPlacement != null)
        {
            //desiredPlacement.OnScrewPlaced(this);
        }

        // 2. Disable collider so Raycasts ignore it in the future
        if (TryGetComponent<Collider>(out var col))
        {
            col.enabled = false;
        }
    }

    public void SetTarget(Vector3 target) => desiredPos = target;
    public void SetPlacement(EncaixeBase target) => desiredPlacement = target;
}