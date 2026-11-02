/*
 * ======================================================================================
 * SCRIPT: GerenciadorTutorial.cs
 * PROJETO: Hyper Bytes (Tutorial de 0 a 4 com Fade Out Final)
 * ======================================================================================
 */

using System.Collections;
using UnityEngine;

public class GerenciadorTutorial : MonoBehaviour
{
    public static GerenciadorTutorial Instancia;

    [Header("--- PAINEIS DOS PASSOS (0 ao 4) ---")]
    public GameObject painelPasso0;
    public GameObject painelPasso1;
    public GameObject painelPasso2;
    public GameObject painelPasso3;
    public GameObject painelPasso4;

    [Header("--- CANVAS GROUPS DAS FALAS (FADE) ---")]
    public CanvasGroup[] canvasGroupPassos;
    public float duracaoFadeFala = 0.3f;

    [Header("--- HUDS ISOLADAS DO JOGO ---")]
    public GameObject containerHudDefeito;
    public GameObject containerHudPrincipal;
    public float duracaoFadeHud = 0.4f;

    [Header("--- BOTAO PROXIMO PASSO ---")]
    public GameObject botaoProximoPasso;

    private int passoAtual = 0;
    private bool estaEmTransicao = false;

    private CanvasGroup cgHudDefeito;
    private CanvasGroup cgHudPrincipal;

    private Coroutine corrotinaHudDefeito;
    private Coroutine corrotinaHudPrincipal;

    private void Awake()
    {
        Instancia = this;
    }

    private void Start()
    {
        GarantirCanvasGroups();
        InicializarTudoOculto();
        AtivarPassoInicial();
    }

    private void GarantirCanvasGroups()
    {
        if (containerHudDefeito != null)
        {
            cgHudDefeito = containerHudDefeito.GetComponent<CanvasGroup>();
            if (cgHudDefeito == null) cgHudDefeito = containerHudDefeito.AddComponent<CanvasGroup>();
        }

        if (containerHudPrincipal != null)
        {
            cgHudPrincipal = containerHudPrincipal.GetComponent<CanvasGroup>();
            if (cgHudPrincipal == null) cgHudPrincipal = containerHudPrincipal.AddComponent<CanvasGroup>();
        }
    }

    private void InicializarTudoOculto()
    {
        if (containerHudDefeito != null)
        {
            cgHudDefeito.alpha = 0f;
            containerHudDefeito.SetActive(false);
        }

        if (containerHudPrincipal != null)
        {
            cgHudPrincipal.alpha = 0f;
            containerHudPrincipal.SetActive(false);
        }
    }

    private void AtivarPassoInicial()
    {
        GameObject[] paineis = { painelPasso0, painelPasso1, painelPasso2, painelPasso3, painelPasso4 };

        for (int i = 0; i < paineis.Length; i++)
        {
            if (paineis[i] != null)
            {
                paineis[i].SetActive(i == 0);
                if (i < canvasGroupPassos.Length && canvasGroupPassos[i] != null)
                {
                    canvasGroupPassos[i].alpha = 0f;
                }
            }
        }

        if (paineis[0] != null && canvasGroupPassos.Length > 0 && canvasGroupPassos[0] != null)
        {
            StartCoroutine(FadeInComPausa(canvasGroupPassos[0]));
        }
    }

    public void AvancarPasso()
    {
        if (estaEmTransicao) return;
        StartCoroutine(ExecutarTransicaoSequencial());
    }

    private IEnumerator ExecutarTransicaoSequencial()
    {
        estaEmTransicao = true;
        GameObject[] paineis = { painelPasso0, painelPasso1, painelPasso2, painelPasso3, painelPasso4 };

        // Se ja estivermos no ultimo passo (Passo 4), o clique faz o Fade Out dele e encerra o tutorial
        if (passoAtual == 4)
        {
            if (passoAtual < canvasGroupPassos.Length && canvasGroupPassos[passoAtual] != null)
            {
                yield return StartCoroutine(FadeOut(canvasGroupPassos[passoAtual]));
            }
            paineis[passoAtual].SetActive(false);

            // Esconde o botao e destrava o jogo definitivamente
            if (botaoProximoPasso != null) botaoProximoPasso.SetActive(false);
            Time.timeScale = 1f;
            estaEmTransicao = false;
            yield break;
        }

        // 1. Fade Out da fala atual para os passos anteriores
        if (passoAtual < paineis.Length && paineis[passoAtual] != null)
        {
            if (passoAtual < canvasGroupPassos.Length && canvasGroupPassos[passoAtual] != null)
            {
                yield return StartCoroutine(FadeOut(canvasGroupPassos[passoAtual]));
            }
            paineis[passoAtual].SetActive(false);
        }

        Time.timeScale = 1f;

        // 2. Avanca o passo
        passoAtual++;
        if (passoAtual > 4) passoAtual = 4;

        // 3. Fade In imediato da proxima fala
        if (passoAtual < paineis.Length && paineis[passoAtual] != null)
        {
            GameObject novoPainel = paineis[passoAtual];
            novoPainel.SetActive(true);

            if (passoAtual < canvasGroupPassos.Length && canvasGroupPassos[passoAtual] != null)
            {
                yield return StartCoroutine(FadeInComPausa(canvasGroupPassos[passoAtual]));
            }
        }

        GerenciarVisibilidadeHuds();
        estaEmTransicao = false;
    }

    private void GerenciarVisibilidadeHuds()
    {
        // Passo 3: Exibe exclusivamente a HUD de Defeito
        if (passoAtual == 3)
        {
            OcultarHudImediato(containerHudPrincipal, cgHudPrincipal);
            MostrarHudComFade(containerHudDefeito, ref cgHudDefeito, ref corrotinaHudDefeito);
        }

        // Passo 4: Exibe exclusivamente a HUD Principal
        if (passoAtual == 4)
        {
            MostrarHudComFade(containerHudPrincipal, ref cgHudPrincipal, ref corrotinaHudPrincipal);
        }
    }

    private void OcultarHudImediato(GameObject obj, CanvasGroup cg)
    {
        if (obj != null && cg != null)
        {
            cg.alpha = 0f;
            obj.SetActive(false);
        }
    }

    private IEnumerator FadeInComPausa(CanvasGroup group)
    {
        Time.timeScale = 0f;

        float tempo = 0f;
        group.alpha = 0f;
        while (tempo < duracaoFadeFala)
        {
            tempo += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(0f, 1f, tempo / duracaoFadeFala);
            yield return null;
        }
        group.alpha = 1f;
    }

    private IEnumerator FadeOut(CanvasGroup group)
    {
        float tempo = 0f;
        float alphaInicial = group.alpha;
        while (tempo < duracaoFadeFala)
        {
            tempo += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(alphaInicial, 0f, tempo / duracaoFadeFala);
            yield return null;
        }
        group.alpha = 0f;
    }

    private void MostrarHudComFade(GameObject hudObj, ref CanvasGroup cg, ref Coroutine corrotinaRef)
    {
        if (hudObj == null || cg == null) return;
        hudObj.SetActive(true);
        if (corrotinaRef != null) StopCoroutine(corrotinaRef);
        corrotinaRef = StartCoroutine(RotinaFadeHud(cg, true, hudObj));
    }

    private IEnumerator RotinaFadeHud(CanvasGroup cg, bool mostrar, GameObject obj)
    {
        float alvoAlpha = mostrar ? 1f : 0f;
        float alphaInicial = cg.alpha;
        float tempo = 0f;

        while (tempo < duracaoFadeHud)
        {
            tempo += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(alphaInicial, alvoAlpha, tempo / duracaoFadeHud);
            yield return null;
        }
        cg.alpha = alvoAlpha;

        if (!mostrar)
        {
            obj.SetActive(false);
        }
    }
}