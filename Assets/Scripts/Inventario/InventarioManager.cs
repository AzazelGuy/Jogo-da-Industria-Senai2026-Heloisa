using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventarioManager : MonoBehaviour
{
    public static InventarioManager Instance { get; private set; }

    [Header("Configurações do Inventário")]
    public GameObject painelInventario;
    public Transform gridSlots;       // Objeto ContainerGrade
    public GameObject slotPrefab;     // Prefab do SlotUI

    [Header("Animação de Fade")]
    public CanvasGroup canvasGroupInventario;
    public float duracaoFade = 0.25f;  // Tempo da transição em segundos

    [Header("Lista de Itens")]
    public List<PieceData> pecasNoInventario = new List<PieceData>();

    private Coroutine coroutineFade;
    private bool inventarioAberto = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Retorna se o inventário está aberto para bloquear os cliques no 3D
    public bool IsInventarioAberto()
    {
        return inventarioAberto;
    }

    private void Start()
    {
        // Se o CanvasGroup não for atribuído manualmente, tenta pegar no painel
        if (canvasGroupInventario == null && painelInventario != null)
        {
            canvasGroupInventario = painelInventario.GetComponent<CanvasGroup>();
        }

        // Garante que o inventário comece invisível e fechado ao iniciar
        if (painelInventario != null)
        {
            if (canvasGroupInventario != null)
            {
                canvasGroupInventario.alpha = 0f;
                canvasGroupInventario.interactable = false;
                canvasGroupInventario.blocksRaycasts = false;
            }
            painelInventario.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            AlternarInventario();
        }
    }

    public void AdicionarPeca(PieceData peca)
    {
        if (peca != null && !pecasNoInventario.Contains(peca))
        {
            pecasNoInventario.Add(peca);
            AtualizarInterfaceInventario();
        }
    }

    public void RemoverPeca(PieceData peca)
    {
        if (pecasNoInventario.Contains(peca))
        {
            pecasNoInventario.Remove(peca);
            AtualizarInterfaceInventario();
        }
    }

    public void AtualizarInterfaceInventario()
    {
        if (gridSlots == null) return;

        // Limpa os slots instanciados anteriormente
        foreach (Transform child in gridSlots)
        {
            Destroy(child.gameObject);
        }

        // Instancia novos slots
        foreach (PieceData peca in pecasNoInventario)
        {
            GameObject novoSlot = Instantiate(slotPrefab, gridSlots);
            SlotUI slotScript = novoSlot.GetComponent<SlotUI>();

            if (slotScript != null)
            {
                slotScript.ConfigurarSlot(peca);
            }
        }
    }

    public void AlternarInventario()
    {
        inventarioAberto = !inventarioAberto;

        if (coroutineFade != null)
        {
            StopCoroutine(coroutineFade);
        }

        coroutineFade = StartCoroutine(ExecutarFade(inventarioAberto));
    }

    public void FecharInventario()
    {
        if (inventarioAberto)
        {
            inventarioAberto = false;
            if (coroutineFade != null) StopCoroutine(coroutineFade);
            coroutineFade = StartCoroutine(ExecutarFade(false));
        }
    }

    private IEnumerator ExecutarFade(bool abrir)
    {
        if (painelInventario == null || canvasGroupInventario == null) yield break;

        float alphaInicial = canvasGroupInventario.alpha;
        float alphaFinal = abrir ? 1f : 0f;

        if (abrir)
        {
            painelInventario.SetActive(true);
        }

        float tempo = 0f;
        while (tempo < duracaoFade)
        {
            tempo += Time.deltaTime;
            canvasGroupInventario.alpha = Mathf.Lerp(alphaInicial, alphaFinal, tempo / duracaoFade);
            yield return null;
        }

        canvasGroupInventario.alpha = alphaFinal;

        // Ativa ou desativa raycasts e interações
        canvasGroupInventario.interactable = abrir;
        canvasGroupInventario.blocksRaycasts = abrir;

        if (!abrir)
        {
            painelInventario.SetActive(false);
        }
    }
}