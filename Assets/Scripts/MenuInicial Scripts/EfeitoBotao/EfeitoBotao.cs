using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class EfeitoBotao : MonoBehaviour
{
    [System.Serializable]
    public struct ObjetoAnimado
    {
        public Transform objeto;
        public Vector3 deslocamento;
    }

    [Header("Gatilho de Detecção")]
    [Tooltip("Arraste aqui o Texto (TMP) ou elemento que detectará o mouse")]
    public GameObject gatilhoHover;

    [Header("Configurações Gerais")]
    public float velocidade = 10f;

    [Header("Lista de Objetos para Animar")]
    public List<ObjetoAnimado> objetosParaMover = new List<ObjetoAnimado>();

    private List<Vector3> posicoesOriginais = new List<Vector3>();
    private List<Vector3> posicoesAlvo = new List<Vector3>();

    void Start()
    {
        // Salva as posições iniciais de cada objeto
        for (int i = 0; i < objetosParaMover.Count; i++)
        {
            if (objetosParaMover[i].objeto != null)
            {
                Vector3 posOriginal = objetosParaMover[i].objeto.localPosition;
                posicoesOriginais.Add(posOriginal);
                posicoesAlvo.Add(posOriginal);
            }
            else
            {
                posicoesOriginais.Add(Vector3.zero);
                posicoesAlvo.Add(Vector3.zero);
            }
        }

        // Define o alvo de detecção (usa o gatilhoHover ou o próprio objeto)
        GameObject alvo = gatilhoHover != null ? gatilhoHover : gameObject;
        AdicionarEventosMouse(alvo);
    }

    void AdicionarEventosMouse(GameObject obj)
    {
        EventTrigger trigger = obj.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = obj.AddComponent<EventTrigger>();
        }

        // Configura a entrada do mouse (Hover In)
        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => { MouseEntrou(); });
        trigger.triggers.Add(entryEnter);

        // Configura a saída do mouse (Hover Out)
        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { MouseSaiu(); });
        trigger.triggers.Add(entryExit);
    }

    void Update()
    {
        for (int i = 0; i < objetosParaMover.Count; i++)
        {
            if (objetosParaMover[i].objeto != null)
            {
                objetosParaMover[i].objeto.localPosition = Vector3.Lerp(
                    objetosParaMover[i].objeto.localPosition,
                    posicoesAlvo[i],
                    Time.deltaTime * velocidade
                );
            }
        }
    }

    public void MouseEntrou()
    {
        for (int i = 0; i < objetosParaMover.Count; i++)
        {
            if (objetosParaMover[i].objeto != null)
            {
                posicoesAlvo[i] = posicoesOriginais[i] + objetosParaMover[i].deslocamento;
            }
        }
    }

    public void MouseSaiu()
    {
        for (int i = 0; i < objetosParaMover.Count; i++)
        {
            if (objetosParaMover[i].objeto != null)
            {
                posicoesAlvo[i] = posicoesOriginais[i];
            }
        }
    }
}