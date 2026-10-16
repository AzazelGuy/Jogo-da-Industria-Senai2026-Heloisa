using System.Collections;
using UnityEngine;

public class CliqueComputador : MonoBehaviour
{
    [Header("Configurações do PC na Tela")]
    public float distanciaDaCamera = 1.8f;
    public float alturaOffset = -0.2f;
    public float tempoAnimacao = 1.0f;

    [Header("Efeito de Flutuação (Hover)")]
    public float velocidadeFlutuacao = 1.5f;
    public float alturaFlutuacao = 0.05f;

    [Header("Controle da Câmera")]
    public MonoBehaviour scriptCamera;

    private Vector3 posicaoOriginal;
    private Quaternion rotacaoOriginal;
    private Transform parentOriginal;

    private Vector3 posicaoAlvoBase;
    private bool estaInspecionando = false;
    private bool emAnimacao = false;
    private Transform pcAtual;

    void Update()
    {
        // Clique no PC para inspecionar
        if (!estaInspecionando && !emAnimacao && Input.GetMouseButtonDown(0))
        {
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.transform.CompareTag("Computador"))
                    {
                        pcAtual = hit.transform;
                        InspecionarComputador();
                    }
                }
            }
        }

        // Aperta ESC para devolver o PC à bancada
        if (estaInspecionando && !emAnimacao && Input.GetKeyDown(KeyCode.Escape))
        {
            DevolverComputador();
        }

        // Aplica o balanço leve (hover) enquanto o PC estiver em inspeção
        if (estaInspecionando && !emAnimacao && pcAtual != null)
        {
            float novoY = Mathf.Sin(Time.time * velocidadeFlutuacao) * alturaFlutuacao;
            pcAtual.localPosition = posicaoAlvoBase + new Vector3(0, novoY, 0);
        }
    }

    void InspecionarComputador()
    {
        posicaoOriginal = pcAtual.position;
        rotacaoOriginal = pcAtual.rotation;
        parentOriginal = pcAtual.parent;

        if (scriptCamera != null) scriptCamera.enabled = false;

        Transform cam = Camera.main.transform;

        Vector3 posAlvoWorld = cam.position + (cam.forward * distanciaDaCamera) + (cam.up * alturaOffset);
        Quaternion rotAlvoWorld = Quaternion.LookRotation(-cam.forward, cam.up);

        StartCoroutine(Animar360EPosicao(pcAtual, posAlvoWorld, rotAlvoWorld, true));
    }

    void DevolverComputador()
    {
        InspecaoEImaPC scriptInspecao = pcAtual.GetComponent<InspecaoEImaPC>();
        if (scriptInspecao != null)
        {
            scriptInspecao.enabled = false;
        }

        StartCoroutine(Animar360EPosicao(pcAtual, posicaoOriginal, rotacaoOriginal, false));
    }

    private IEnumerator Animar360EPosicao(Transform pc, Vector3 posFinal, Quaternion rotFinal, bool entrando)
    {
        emAnimacao = true;
        float tempo = 0f;

        Vector3 posInicial = pc.position;
        Quaternion rotInicial = pc.rotation;

        while (tempo < tempoAnimacao)
        {
            tempo += Time.deltaTime;
            float t = tempo / tempoAnimacao;

            t = t * t * (3f - 2f * t);

            pc.position = Vector3.Lerp(posInicial, posFinal, t);

            float anguloExtra = Mathf.Lerp(0f, 360f, t);
            Quaternion giroEixo = Quaternion.AngleAxis(anguloExtra, Vector3.up);

            pc.rotation = Quaternion.Lerp(rotInicial, rotFinal, t) * giroEixo;

            yield return null;
        }

        pc.position = posFinal;
        pc.rotation = rotFinal;

        if (entrando)
        {
            pc.SetParent(Camera.main.transform);
            posicaoAlvoBase = pc.localPosition;
            estaInspecionando = true;

            InspecaoEImaPC scriptInspecao = pc.GetComponent<InspecaoEImaPC>();
            if (scriptInspecao != null)
            {
                scriptInspecao.enabled = true;
            }
        }
        else
        {
            pc.SetParent(parentOriginal);
            estaInspecionando = false;
            if (scriptCamera != null) scriptCamera.enabled = true;
        }

        emAnimacao = false;
    }
}