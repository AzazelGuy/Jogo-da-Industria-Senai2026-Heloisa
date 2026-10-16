using UnityEngine;

public class FocusPoint : MonoBehaviour
{
    public enum TargetType
    {
        Socket, // Foca usando a referência/posição do slot de encaixe
        Piece   // Foca na própria peça
    }

    [Header("Configurações do Zoom")]
    [Tooltip("Define se o foco será calculado a partir da peça ou do local de encaixe.")]
    public TargetType zoomTarget = TargetType.Piece;

    [Header("Configurações de Foco")]
    [Tooltip("Offset relativo à posição do alvo (Peça ou Socket) para a câmera.")]
    public Vector3 cameraOffset = new Vector3(0, 0.15f, -0.25f);

    [Tooltip("Rotação desejada da câmera ao focar.")]
    public Vector3 targetEulerAngles = new Vector3(30f, 0f, 0f);

    [Tooltip("Campo de visão (FOV) para aproximar a imagem.")]
    public float focusedFOV = 35f;

    /// <summary>
    /// Retorna a posição do mundo onde a câmera ficará, ajustada com base no alvo escolhido.
    /// </summary>
    public Vector3 GetWorldTargetPosition(Transform targetOverride = null)
    {
        Transform baseTransform = (zoomTarget == TargetType.Socket && targetOverride != null)
            ? targetOverride
            : transform;

        return baseTransform.position + baseTransform.TransformDirection(cameraOffset);
    }

    public Quaternion GetWorldTargetRotation()
    {
        return Quaternion.Euler(targetEulerAngles);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 worldPos = GetWorldTargetPosition();
        Gizmos.DrawWireSphere(worldPos, 0.03f);
        Gizmos.DrawLine(transform.position, worldPos);
    }
}