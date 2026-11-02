using UnityEngine;

public class AnimacaoMascote : MonoBehaviour
{
    [Header("Mão do Mascote")]
    public RectTransform maoMascote;

    [Header("Configurações do Corpo")]
    public float floatCorpo = 10f;
    public float velCorpo = 1.5f;

    [Header("Configurações da Mão")]
    public float floatMao = 15f;
    public float velMao = 2.5f;
    public float anguloRotacaoMao = 8f; // Graus de inclinação

    private Vector3 posInicialCorpo;
    private Vector3 posInicialMao;

    void Start()
    {
        posInicialCorpo = transform.localPosition;
        if (maoMascote != null)
        {
            posInicialMao = maoMascote.localPosition;
        }
    }

    void Update()
    {
        // 1. Movimento leve do Corpo (Cima/Baixo)
        float novoYCorpo = posInicialCorpo.y + Mathf.Sin(Time.time * velCorpo) * floatCorpo;
        transform.localPosition = new Vector3(posInicialCorpo.x, novoYCorpo, posInicialCorpo.z);

        // 2. Movimento e Inclinação da Mão
        if (maoMascote != null)
        {
            // Flutuação Y da mão
            float novoYMao = posInicialMao.y + Mathf.Sin(Time.time * velMao) * floatMao;
            maoMascote.localPosition = new Vector3(posInicialMao.x, novoYMao, posInicialMao.z);

            // Rotação/Inclinação em Graus (Z)
            float rotacaoZ = Mathf.Sin(Time.time * velMao) * anguloRotacaoMao;
            maoMascote.localRotation = Quaternion.Euler(0, 0, rotacaoZ);
        }
    }
}