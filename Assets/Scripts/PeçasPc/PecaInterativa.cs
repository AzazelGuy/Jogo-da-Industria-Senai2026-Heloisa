/*
 * ======================================================================================
 * SCRIPT: PecaInterativa.cs
 * PROJETO: Hyper Bytes
 * ======================================================================================
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PecaInterativa : MonoBehaviour
{
    [Header("--- CONFIGURAÇÃO DA PEÇA ---")]
    public string nomeDaPeca = "Peça do PC";
    public bool ehAPecaComDefeito = false;

    [Header("--- REFERÊNCIA DO BOOT (UI) ---")]
    [Tooltip("Arraste o objeto 'Botao_Boot_Verde' da sua UI para cá.")]
    public GameObject botaoBootVerde;

    [Header("--- CONFIGURAÇÃO DE ANIMAÇÃO ---")]
    public float tempoDesaparecer = 0.2f;

    [HideInInspector]
    public bool foiRemovida = false;

    private Collider colisorPeca;

    private void Awake()
    {
        colisorPeca = GetComponent<Collider>();
        foiRemovida = false;
    }

    public void DesmontarPeca()
    {
        Debug.Log(">>> TENTATIVA DE REMOÇÃO DA PEÇA: [" + gameObject.name + "]");

        if (botaoBootVerde != null && botaoBootVerde.activeSelf)
        {
            Debug.Log("BLOQUEADO: O PC está ligado!");
            if (GerenciadorBootEMascote.Instancia != null)
            {
                GerenciadorBootEMascote.Instancia.TentarInteragirComPeca();
            }
            return;
        }

        foiRemovida = true;

        if (GerenciadorBootEMascote.Instancia != null)
        {
            GerenciadorBootEMascote.Instancia.ValidarPecaRemovida(ehAPecaComDefeito);
        }

        if (colisorPeca != null)
        {
            colisorPeca.enabled = false;
        }

        StartCoroutine(AnimarEEncolher());
    }

    private IEnumerator AnimarEEncolher()
    {
        Vector3 escalaInicial = transform.localScale;
        float tempoDecorrido = 0f;

        while (tempoDecorrido < tempoDesaparecer)
        {
            tempoDecorrido += Time.deltaTime;
            float progresso = tempoDecorrido / tempoDesaparecer;
            transform.localScale = Vector3.Lerp(escalaInicial, Vector3.zero, progresso);
            yield return null;
        }

        gameObject.SetActive(false);
    }
}