using System.Collections;
using UnityEngine;

public class ClickFicha : MonoBehaviour
{
    [Header("Configurações da UI")]
    public RectTransform fichaUI;
    public float posicaoEscondidaY = -1200f;
    public float posicaoVisivelY = 0f;
    public float tempoAnimacao = 0.35f;

    [Header("Controle do Jogador/Câmera")]
    public MonoBehaviour scriptCamera; // Coloque o script de movimentação/olhar aqui

    private bool estaAberta = false;
    private bool emAnimacao = false;

    void Start()
    {
        if (fichaUI != null)
        {
            Vector2 pos = fichaUI.anchoredPosition;
            pos.y = posicaoEscondidaY;
            fichaUI.anchoredPosition = pos;
        }
    }

    void Update()
    {
        if (!estaAberta && !emAnimacao && Input.GetMouseButtonDown(0))
        {
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    {
                        AbrirFicha();
                    }
                }
            }
        }

        if (estaAberta && !emAnimacao && Input.GetKeyDown(KeyCode.Escape))
        {
            FecharFicha();
        }
    }

    public void AbrirFicha()
    {
        StartCoroutine(MoverFicha(posicaoEscondidaY, posicaoVisivelY));
        estaAberta = true;

        // Desativa o controle da câmera
        if (scriptCamera != null)
        {
            scriptCamera.enabled = false;
        }

        // Libera o ponteiro do mouse
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void FecharFicha()
    {
        StartCoroutine(MoverFicha(posicaoVisivelY, posicaoEscondidaY));
        estaAberta = false;

        // Reativa o controle da câmera
        if (scriptCamera != null)
        {
            scriptCamera.enabled = true;
        }
    }

    private IEnumerator MoverFicha(float inicioY, float fimY)
    {
        emAnimacao = true;
        float tempo = 0f;
        Vector2 pos = fichaUI.anchoredPosition;

        while (tempo < tempoAnimacao)
        {
            tempo += Time.deltaTime;
            float t = tempo / tempoAnimacao;
            t = t * t * (3f - 2f * t);

            pos.y = Mathf.Lerp(inicioY, fimY, t);
            fichaUI.anchoredPosition = pos;
            yield return null;
        }

        pos.y = fimY;
        fichaUI.anchoredPosition = pos;
        emAnimacao = false;
    }
}