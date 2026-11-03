using UnityEngine;

public class FundoInfinito : MonoBehaviour
{
    [Header("Configurações do Movimento")]
    public float velocidade = 2f;
    public Vector2 direcao = new Vector2(-1f, 0f); // (-1, 0) para esquerda, (0, -1) para baixo

    [Header("Imagem Seguidora")]
    public Transform segundoFundo; // Arraste a segunda imagem idêntica aqui

    private float tamanhoImagem;
    private Vector3 posicaoInicial;

    void Start()
    {
        posicaoInicial = transform.localPosition;

        // Obtém a largura da imagem baseada no RectTransform (UI)
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            tamanhoImagem = rect.rect.width;
        }
    }

    void Update()
    {
        // Move o objeto principal na direção configurada
        transform.Translate(direcao * velocidade * Time.deltaTime);

        // Quando a imagem anda o tamanho exato de sua largura, reseta a posição
        if (Vector3.Distance(posicaoInicial, transform.localPosition) >= tamanhoImagem)
        {
            transform.localPosition = posicaoInicial;
        }
    }
}