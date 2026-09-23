using UnityEngine;

public class TextoPulsar : MonoBehaviour
{
    [Header("Configurações do Pulsar")]
    public float escalaMinima = 0.95f; // Tamanho mínimo (95% do original)
    public float escalaMaxima = 1.08f; // Tamanho máximo (108% do original)
    public float velocidade = 3f;      // Velocidade do pulsar

    private Vector3 escalaOriginal;

    void Start()
    {
        escalaOriginal = transform.localScale;
    }

    void Update()
    {
        // Calcula a variação usando a onda Senoidal (Mathf.Sin)
        float fator = (Mathf.Sin(Time.time * velocidade) + 1f) / 2f;
        float escalaAtual = Mathf.Lerp(escalaMinima, escalaMaxima, fator);

        transform.localScale = escalaOriginal * escalaAtual;
    }
}