/*
 * ======================================================================================
 * SCRIPT: CliqueComputador.cs
 * PROJETO: Hyper Bytes
 * ======================================================================================
 */

using System.Collections;
using UnityEngine;

public class CliqueComputador : MonoBehaviour
{
    [Header("Configuracoes do PC na Tela")]
    public float distanciaDaCamera = 3.5f;
    public float alturaOffset = -0.2f;
    public float tempoAnimacao = 1.0f;

    [Header("Ajuste de Rotacao da Tampa (Ex: 0, 90, 180 ou 270)")]
    public float ajusteRotacaoTampaY = 90f;

    [Header("--- CONTROLE DA TAMPA LATERAL ---")]
    [Tooltip("Nome exato do objeto da tampa lateral dentro do prefab/objeto do PC.")]
    public string nomeObjetoTampa = "TampaLateral";
    [Tooltip("Tempo que a tampa leva para sumir ou reaparecer.")]
    public float tempoEncolherTampa = 0.4f;

    private GameObject objTampaLateral;
    private Vector3 escalaOriginalTampa;

    [Header("Efeito de Flutuacao (Hover)")]
    public float velocidadeFlutuacao = 1.5f;
    public float alturaFlutuacao = 0.05f;

    [Header("Controle da Camera")]
    public MonoBehaviour scriptCamera;

    [Header("--- INTERFACE DA HUD (SLOTS - ESQUERDA/LADO) ---")]
    public RectTransform painelSlotsPecas;
    public float tempoDeslize = 0.5f;
    public float posXVisivelSlots = 50f;
    public float posXEscondidoSlots = -500f;

    [Header("--- INTERFACE DA HUD (SINTOMAS - DIREITA) ---")]
    public RectTransform painelSintomas;
    public float posXEscondidoSintomas = 257f;
    public float posXParcialSintomas = 450f;
    public float posXVisivelSintomas = 0f;

    private Vector3 posicaoOriginal;
    private Quaternion rotacaoOriginal;
    private Transform parentOriginal;

    private Vector3 posicaoAlvoBase;
    private bool estaInspecionando = false;
    private bool emAnimacao = false;
    private Transform pcAtual;

    private void Start()
    {
        if (painelSlotsPecas != null)
        {
            Vector3 pos = painelSlotsPecas.anchoredPosition;
            pos.x = posXEscondidoSlots;
            painelSlotsPecas.anchoredPosition = pos;
            painelSlotsPecas.gameObject.SetActive(false);
        }

        if (painelSintomas != null)
        {
            Vector3 pos = painelSintomas.anchoredPosition;
            pos.x = posXEscondidoSintomas;
            painelSintomas.anchoredPosition = pos;
            painelSintomas.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
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
                        EncontrarTampaLateral(pcAtual);
                        InspecionarComputador();
                    }
                }
            }
        }

        if (estaInspecionando && !emAnimacao && Input.GetKeyDown(KeyCode.Escape))
        {
            DevolverComputador();
        }

        if (estaInspecionando && !emAnimacao && pcAtual != null)
        {
            float novoY = Mathf.Sin(Time.time * velocidadeFlutuacao) * alturaFlutuacao;
            pcAtual.localPosition = posicaoAlvoBase + new Vector3(0, novoY, 0);
        }
    }

    private void EncontrarTampaLateral(Transform pc)
    {
        objTampaLateral = null;
        Transform[] filhos = pc.GetComponentsInChildren<Transform>(true);
        foreach (Transform filho in filhos)
        {
            if (filho.name == nomeObjetoTampa)
            {
                objTampaLateral = filho.gameObject;
                escalaOriginalTampa = objTampaLateral.transform.localScale;
                break;
            }
        }
    }

    private void InspecionarComputador()
    {
        posicaoOriginal = pcAtual.position;
        rotacaoOriginal = pcAtual.rotation;
        parentOriginal = pcAtual.parent;

        if (scriptCamera != null) scriptCamera.enabled = false;

        Transform cam = Camera.main.transform;

        Vector3 posAlvoWorld = cam.position + (cam.forward * distanciaDaCamera) + (cam.up * alturaOffset);
        Quaternion rotAlvoWorld = Quaternion.LookRotation(-cam.forward, cam.up) * Quaternion.Euler(0, ajusteRotacaoTampaY, 0);

        StopAllCoroutines();

        if (painelSlotsPecas != null)
        {
            painelSlotsPecas.gameObject.SetActive(true);
            StartCoroutine(DeslizarPainel(painelSlotsPecas, posXEscondidoSlots, posXVisivelSlots));
        }

        if (painelSintomas != null)
        {
            painelSintomas.gameObject.SetActive(true);
            StartCoroutine(DeslizarPainel(painelSintomas, posXEscondidoSintomas, posXParcialSintomas));
        }

        StartCoroutine(Animar360EPosicao(pcAtual, posAlvoWorld, rotAlvoWorld, true));
    }

    private void DevolverComputador()
    {
        StopAllCoroutines();

        // 1. FAZ A TAMPA REAPARECER CRESCENDO SUAVEMENTE ANTES DE VOLTAR
        if (objTampaLateral != null)
        {
            objTampaLateral.SetActive(true);
            // Comeca da escala zero e cresce ate a escala original da tampa
            StartCoroutine(AnimarEscalaTampa(Vector3.zero, escalaOriginalTampa, desativarNoFim: false));
        }

        InspecaoEImaPC scriptInspecao = pcAtual.GetComponent<InspecaoEImaPC>();
        if (scriptInspecao != null)
        {
            scriptInspecao.enabled = false;
        }

        if (painelSlotsPecas != null)
        {
            StartCoroutine(DeslizarPainelEDesativar(painelSlotsPecas, painelSlotsPecas.anchoredPosition.x, posXEscondidoSlots));
        }

        if (painelSintomas != null)
        {
            StartCoroutine(DeslizarPainelEDesativar(painelSintomas, painelSintomas.anchoredPosition.x, posXEscondidoSintomas));
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

            // Quando entra no zoom, a tampa diminui ate sumir
            if (objTampaLateral != null)
            {
                StartCoroutine(AnimarEscalaTampa(escalaOriginalTampa, Vector3.zero, desativarNoFim: true));
            }

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

    private IEnumerator AnimarEscalaTampa(Vector3 escalaInicial, Vector3 escalaFinal, bool desativarNoFim)
    {
        if (objTampaLateral == null) yield break;

        float tempo = 0f;
        while (tempo < tempoEncolherTampa)
        {
            tempo += Time.deltaTime;
            float t = tempo / tempoEncolherTampa;
            t = t * t * (3f - 2f * t);

            objTampaLateral.transform.localScale = Vector3.Lerp(escalaInicial, escalaFinal, t);
            yield return null;
        }

        objTampaLateral.transform.localScale = escalaFinal;

        if (desativarNoFim)
        {
            objTampaLateral.SetActive(false);
        }
    }

    public IEnumerator DeslizarPainel(RectTransform painel, float xInicio, float xFim)
    {
        float tempoDecorrido = 0f;
        Vector3 pos = painel.anchoredPosition;

        while (tempoDecorrido < tempoDeslize)
        {
            tempoDecorrido += Time.deltaTime;
            float t = tempoDecorrido / tempoDeslize;
            t = t * t * (3f - 2f * t);

            pos.x = Mathf.Lerp(xInicio, xFim, t);
            painel.anchoredPosition = pos;

            yield return null;
        }

        pos.x = xFim;
        painel.anchoredPosition = pos;
    }

    private IEnumerator DeslizarPainelEDesativar(RectTransform painel, float xInicio, float xFim)
    {
        yield return StartCoroutine(DeslizarPainel(painel, xInicio, xFim));
        painel.gameObject.SetActive(false);
    }
}