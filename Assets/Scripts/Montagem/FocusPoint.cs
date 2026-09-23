using UnityEngine;

/// <summary>
/// Define um ponto de foco para o CameraControllerMontagem: informa a posição, rotação
/// e FOV (zoom) que a câmera deve assumir ao focar nesta peça ou no encaixe associado.
/// </summary>
public class FocusPoint : MonoBehaviour
{
    #region Tipos

    /// <summary>
    /// Define a partir de qual transform o foco deve ser calculado.
    /// </summary>
    public enum TargetType
    {
        Socket, // Foca usando a referência/posição do slot de encaixe
        Piece   // Foca na própria peça
    }

    #endregion

    #region Campos Serializados

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

    #endregion

    #region API Pública

    /// <summary>
    /// Retorna a posição do mundo onde a câmera ficará, ajustada com base no alvo escolhido
    /// (peça ou socket) e no offset configurado.
    /// </summary>
    public Vector3 GetWorldTargetPosition(Transform targetOverride = null)
    {
        Transform baseTransform = (zoomTarget == TargetType.Socket && targetOverride != null)
            ? targetOverride
            : transform;

        return baseTransform.position + baseTransform.TransformDirection(cameraOffset);
    }

    /// <summary>
    /// Retorna a rotação desejada da câmera ao focar neste ponto.
    /// </summary>
    public Quaternion GetWorldTargetRotation()
    {
        return Quaternion.Euler(targetEulerAngles);
    }

    #endregion

    #region Gizmos (Editor)

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 worldPos = GetWorldTargetPosition();
        Gizmos.DrawWireSphere(worldPos, 0.03f);
        Gizmos.DrawLine(transform.position, worldPos);
    }

    #endregion
}
