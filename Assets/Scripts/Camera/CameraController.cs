using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Positions")]
    public Transform position1;
    public Transform position2;

    [Header("Horizontal Mouse Sensor")]
    [Range(0f, 0.2f)]
    public float edgeThreshold = 0.05f;

    [Header("Left Look")]
    public float leftLookAngle = 15f;

    [Header("Vertical Look")]
    [Range(0f, 0.2f)]
    public float verticalThreshold = 0.08f;

    public float lookUpAngle = 12f;
    public float lookDownAngle = 12f;

    [Header("Movement")]
    public float positionSpeed = 5f;
    public float rotationSpeed = 4f;

    [Header("Natural Camera Movement")]
    public float bobAmount = 0.03f;
    public float bobSpeed = 0.8f;
    public float swayAmount = 1f;

    private Vector3 targetPosition;
    private Quaternion horizontalRotation;

    private float verticalAngle = 0f;
    private bool lookingRight = false;

    void Start()
    {
        if (position1 == null || position2 == null)
        {
            Debug.LogError("Configure Position 1 e Position 2 no Inspector.");
            return;
        }

        transform.position = position1.position;
        transform.rotation = position1.rotation;

        targetPosition = position1.position;
        horizontalRotation = position1.rotation;
    }

    void Update()
    {
        CheckMousePosition();
        MoveCamera();
    }

    void CheckMousePosition()
    {
        float mouseX = Input.mousePosition.x / Screen.width;
        float mouseY = Input.mousePosition.y / Screen.height;

        // ==========================================
        // HORIZONTAL
        // ==========================================

        if (mouseX > 1f - edgeThreshold)
        {
            lookingRight = true;

            targetPosition = position2.position;
            horizontalRotation = position2.rotation;
        }
        else if (mouseX < edgeThreshold)
        {
            lookingRight = false;

            targetPosition = position1.position;

            horizontalRotation = position1.rotation
                * Quaternion.Euler(0f, -leftLookAngle, 0f);
        }
        else if (!lookingRight)
        {
            targetPosition = position1.position;
            horizontalRotation = position1.rotation;
        }


        // ==========================================
        // VERTICAL
        // ==========================================

        verticalAngle = 0f;

        if (mouseY > 1f - verticalThreshold)
        {
            float amount =
                (mouseY - (1f - verticalThreshold))
                / verticalThreshold;

            verticalAngle = Mathf.Lerp(
                0f,
                -lookUpAngle,
                amount
            );
        }
        else if (mouseY < verticalThreshold)
        {
            float amount =
                (verticalThreshold - mouseY)
                / verticalThreshold;

            verticalAngle = Mathf.Lerp(
                0f,
                lookDownAngle,
                amount
            );
        }
    }

    void MoveCamera()
    {
        // ==========================================
        // MOVIMENTO NATURAL
        // ==========================================

        float bob =
            Mathf.Sin(Time.time * bobSpeed)
            * bobAmount;

        float sway =
            Mathf.Sin(Time.time * bobSpeed * 0.7f)
            * swayAmount;


        // ==========================================
        // POSIÇÃO
        // ==========================================

        Vector3 naturalPosition = targetPosition;

        naturalPosition += Vector3.up * bob;


        // ==========================================
        // ROTAÇÃO
        // ==========================================

        // Primeiro aplica a rotação horizontal.
        Quaternion finalRotation = horizontalRotation;

        // Depois aplica APENAS a inclinação vertical.
        finalRotation *= Quaternion.Euler(
            verticalAngle,
            0f,
            0f
        );

        // Por último aplica o movimento natural.
        finalRotation *= Quaternion.Euler(
            bob * 10f,
            sway,
            sway * 0.3f
        );


        // ==========================================
        // MOVIMENTO SUAVE
        // ==========================================

        transform.position = Vector3.Lerp(
            transform.position,
            naturalPosition,
            positionSpeed * Time.deltaTime
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            finalRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}