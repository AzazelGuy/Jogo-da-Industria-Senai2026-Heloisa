using System.Collections;
using UnityEngine;

public class PecaInterativa : MonoBehaviour
{
    [Header("Dados da Peça")]
    public PieceData dadosDaPeca;

    [Header("Configuração de Animação")]
    public float tempoDesaparecer = 0.2f;

    private Vector3 escalaOriginal;

    private void Awake()
    {
        // Guarda a escala original da peça para restaurar ao equipar
        escalaOriginal = transform.localScale;
    }

    private void OnMouseDown()
    {
        // Trava qualquer interação no 3D enquanto o inventário estiver visível na tela
        if (InventarioManager.Instance != null && InventarioManager.Instance.IsInventarioAberto())
        {
            return;
        }

        DesmontarPeca();
    }

    public void DesmontarPeca()
    {
        if (InventarioManager.Instance != null && dadosDaPeca != null)
        {
            InventarioManager.Instance.AdicionarPeca(dadosDaPeca);
        }

        // Inicia a animação de encolher em vez de sumir instantaneamente
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

            // Reduz a escala gradualmente de 100% até 0%
            transform.localScale = Vector3.Lerp(escalaInicial, Vector3.zero, progresso);
            yield return null;
        }

        // Esconde o objeto após a animação
        gameObject.SetActive(false);
    }

    public void GuardarNoInventario()
    {
        DesmontarPeca();
    }

    public void ReativarPecaNoPC()
    {
        // 1. Restaura a escala e ativa o GameObject
        transform.localScale = escalaOriginal;
        gameObject.SetActive(true);

        // 2. Reativa malhas 3D
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer mr in renderers)
        {
            mr.enabled = true;
        }

        // 3. Reativa colliders
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }
    }
}