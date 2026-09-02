using UnityEngine;

public enum InsertionType
{
    MoveToPosition,
    PlayAnimation,
    Instant
}

public class Connector : MonoBehaviour, ISelectable
{
    [Header("Configurações Gerais")]
    [SerializeField] private InsertionType insertionType = InsertionType.MoveToPosition;
    [SerializeField] private IdentifiyerEncaixe desiredPlacement;
    [SerializeField] private int connectorIndex = 0; // Índice da lista no IdentifiyerEncaixe

    [Header("Modo Movimento")]
    [SerializeField] private Vector3 desiredPos;
    [SerializeField] private float insertionSpeed = 1.5f;
    [SerializeField] private float completionDistance = 0.001f;

    [Header("Modo Animação")]
    [SerializeField] private Animator animator;
    [SerializeField] private string animTriggerName = "Insert";
    [SerializeField] private float holdTimeRequired = 1.5f;

    private bool isPlaced;
    private float holdTimer;

    public void OnSelect() { }
    public void OnDeselect() { }
    public void OnDoubleClick() { }

    public void OnHold()
    {
        if (isPlaced) return;

        switch (insertionType)
        {
            case InsertionType.MoveToPosition:
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    desiredPos,
                    insertionSpeed * Time.deltaTime
                );

                if (Vector3.Distance(transform.position, desiredPos) <= completionDistance)
                {
                    LockInPlace();
                }
                break;

            case InsertionType.PlayAnimation:
                holdTimer += Time.deltaTime;
                if (holdTimer >= holdTimeRequired)
                {
                    if (animator != null) animator.SetTrigger(animTriggerName);
                    LockInPlace();
                }
                break;

            case InsertionType.Instant:
                LockInPlace();
                break;
        }
    }

    private void LockInPlace()
    {
        isPlaced = true;

        if (insertionType == InsertionType.MoveToPosition || insertionType == InsertionType.Instant)
        {
            transform.position = desiredPos;
        }

        // Notifica a base informando a conclusão no seu índice correspondente
        if (desiredPlacement != null)
        {
            desiredPlacement.NotifyConnectorPlaced(connectorIndex);
        }

        if (TryGetComponent<Collider>(out var col))
        {
            col.enabled = false;
        }
    }

    public void SetTarget(Vector3 target) => desiredPos = target;
    public void SetPlacement(IdentifiyerEncaixe target, int index = 0)
    {
        desiredPlacement = target;
        connectorIndex = index;
    }
}