using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Indicador de progresso em bolinhas (º----º----º) feito com sprites.
/// Cria uma bolinha por etapa do StepChecker, com uma linha entre elas, e mostra o nome da etapa atual.
/// 
/// Setup: o container precisa ter um Horizontal Layout Group e começar vazio.
/// </summary>
public class IndicadorEtapas : MonoBehaviour
{
    #region Campos Serializados

    [Header("Onde as bolinhas nascem (Horizontal Layout Group)")]
    [SerializeField] private RectTransform container;

    [Header("Prefabs (um GameObject só com Image em cada)")]
    [SerializeField] private Image prefabBolinha;
    [SerializeField] private Image prefabLinha;

    [Header("Sprites da bolinha")]
    [SerializeField] private Sprite spriteConcluida;
    [SerializeField] private Sprite spriteAtual;
    [SerializeField] private Sprite spritePendente;

    [Header("Linha entre as bolinhas")]
    [SerializeField] private Color corLinhaConcluida = Color.white;
    [SerializeField] private Color corLinhaPendente = new Color(1f, 1f, 1f, 0.3f);

    [Header("Tamanhos")]
    [SerializeField] private Vector2 tamanhoBolinha = new Vector2(32f, 32f);
    [SerializeField] private float alturaLinha = 4f;
    [Tooltip("A bolinha da etapa atual fica um pouco maior para destacar.")]
    [SerializeField] private float escalaDaAtual = 1.25f;

    [Header("Nome da etapa atual")]
    [SerializeField] private TMP_Text textoNomeEtapa;
    [SerializeField] private string textoConcluido = "Montagem concluída!";

    #endregion

    #region Estado Interno

    private readonly List<Image> bolinhas = new List<Image>();
    private readonly List<Image> linhas = new List<Image>();

    #endregion

    #region Ciclo de Vida (Unity)

    private void OnEnable()
    {
        StepChecker.OnEtapaMudou += AoMudarEtapa;
    }

    private void Start()
    {
        StepChecker sc = StepChecker.Instance;

        // Sem StepChecker ou no modo antigo (sem etapas) não há o que indicar.
        if (sc == null || !sc.UsaEtapas)
        {
            gameObject.SetActive(false);
            return;
        }

        Construir(sc.TotalEtapas);
        Atualizar(sc.IndiceEtapaAtual, sc.EtapaAtual);
    }

    private void OnDisable()
    {
        StepChecker.OnEtapaMudou -= AoMudarEtapa;
    }

    #endregion

    #region Construção

    private void Construir(int total)
    {
        bolinhas.Clear();
        linhas.Clear();

        for (int i = 0; i < total; i++)
        {
            Image bolinha = Instantiate(prefabBolinha, container);
            bolinha.name = $"Bolinha {i + 1}";
            ConfigurarLayout(bolinha.gameObject, tamanhoBolinha.x, tamanhoBolinha.y, 0f);
            bolinhas.Add(bolinha);

            // Entre uma bolinha e a próxima vai uma linha que estica para ocupar o espaço livre.
            if (i < total - 1)
            {
                Image linha = Instantiate(prefabLinha, container);
                linha.name = $"Linha {i + 1}";
                ConfigurarLayout(linha.gameObject, 8f, alturaLinha, 1f);
                linhas.Add(linha);
            }
        }
    }

    private static void ConfigurarLayout(GameObject alvo, float largura, float altura, float larguraFlexivel)
    {
        LayoutElement le = alvo.GetComponent<LayoutElement>();
        if (le == null) le = alvo.AddComponent<LayoutElement>();

        le.preferredWidth = largura;
        le.preferredHeight = altura;
        le.flexibleWidth = larguraFlexivel;
    }

    #endregion

    #region Atualização

    private void AoMudarEtapa(int indice, EtapaMontagem etapa)
    {
        if (bolinhas.Count == 0) return;
        Atualizar(indice, etapa);
    }

    private void Atualizar(int indiceAtual, EtapaMontagem etapa)
    {
        for (int i = 0; i < bolinhas.Count; i++)
        {
            bool concluida = i < indiceAtual;
            bool atual = i == indiceAtual;

            Sprite sprite = concluida ? spriteConcluida : (atual ? spriteAtual : spritePendente);
            if (sprite != null) bolinhas[i].sprite = sprite;

            bolinhas[i].transform.localScale = Vector3.one * (atual ? escalaDaAtual : 1f);
        }

        // A linha i liga a etapa i à i+1: fica "acesa" quando a etapa i já foi concluída.
        for (int i = 0; i < linhas.Count; i++)
        {
            linhas[i].color = i < indiceAtual ? corLinhaConcluida : corLinhaPendente;
        }

        if (textoNomeEtapa == null) return;

        // etapa == null significa que todas foram concluídas.
        if (etapa == null)
        {
            textoNomeEtapa.text = textoConcluido;
        }
        else
        {
            textoNomeEtapa.text = string.IsNullOrWhiteSpace(etapa.nome) ? $"Etapa {indiceAtual + 1}" : etapa.nome;
        }
    }

    #endregion
}
