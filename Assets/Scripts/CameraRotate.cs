using UnityEngine;

public class CameraRotate : MonoBehaviour
{
    [SerializeField] float velocidade = 5f;
    float angulo;

    void LateUpdate()
    {
        transform.Rotate(0f, velocidade * Time.unscaledDeltaTime, 0f, Space.World);
    }
}