using UnityEngine;

public class FlutuarLogo : MonoBehaviour
{
    [Header("Configurações da Flutuação")]
    public float amplitude = 15f;  // O quanto sobe e desce (distância)
    public float velocidade = 2f; // A velocidade do movimento
    public float desvioTempo = 0f; // Para intercalar o movimento entre HYPER e BYTE

    private Vector3 posicaoInicial;

    void Start()
    {
        posicaoInicial = transform.localPosition;
    }

    void Update()
    {
        // Calcula o movimento suave usando onda Senoidal (Mathf.Sin)
        float novoY = posicaoInicial.y + Mathf.Sin((Time.time + desvioTempo) * velocidade) * amplitude;
        transform.localPosition = new Vector3(posicaoInicial.x, novoY, posicaoInicial.z);
    }
}