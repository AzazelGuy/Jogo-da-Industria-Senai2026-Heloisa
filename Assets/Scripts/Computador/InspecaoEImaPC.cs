using UnityEngine;

public class InspecaoEImaPC : MonoBehaviour
{
    [Header("Sensibilidade e Suavização")]
    public float sensibilidadeMouse = 5f;
    public float velocidadeIma = 10f;

    [Header("Configurações de Zoom")]
    public float velocidadeZoom = 5f;
    public float fovMinimo = 25f;    // Zoom Máximo (Aproximação)
    public float fovMaximo = 60f;    // Zoom Normal / Visão Padrão
    public float velocidadeSuavizacaoZoom = 8f;

    [Header("Camadas de Interação")]
    public LayerMask camadaPecas = ~0; // Padrão: aceita todas as camadas (Everything)

    private bool arrastando = false;
    private bool jaInteragiu = false;

    private Camera camPrincipal;
    private float fovAlvo;

    private readonly Quaternion[] rotacoesDasFaces = new Quaternion[]
    {
        Quaternion.Euler(0, 0, 0),       // Frente
        Quaternion.Euler(0, 180, 0),     // Trás / Fundo
        Quaternion.Euler(0, 90, 0),      // Esquerda
        Quaternion.Euler(0, -90, 0),     // Direita
        Quaternion.Euler(90, 0, 0),      // Cima / Topo
        Quaternion.Euler(-90, 0, 0)      // Baixo / Base
    };

    void Awake()
    {
        camPrincipal = Camera.main;
        if (camPrincipal != null)
        {
            fovAlvo = camPrincipal.fieldOfView;
        }
    }

    void OnEnable()
    {
        arrastando = false;
        jaInteragiu = false;

        if (camPrincipal == null) camPrincipal = Camera.main;
        if (camPrincipal != null)
        {
            fovAlvo = camPrincipal.fieldOfView;
        }
    }

    void OnDisable()
    {
        arrastando = false;
        jaInteragiu = false;

        // Garante que o Zoom volte ao padrão ao sair do modo de inspeção
        if (camPrincipal != null)
        {
            camPrincipal.fieldOfView = fovMaximo;
        }
    }

    void Update()
    {
        if (camPrincipal == null) return;

        // 1. Tecla ESC reseta o zoom de volta à visão padrão
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ResetarZoom();
        }

        // 2. Leitura do Scroll do Mouse para Zoom dinâmico
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            fovAlvo -= scroll * velocidadeZoom * 10f;
            fovAlvo = Mathf.Clamp(fovAlvo, fovMinimo, fovMaximo);
        }

        // Aplica o Zoom suavemente (Imune ao Time.timeScale = 0 do Pause)
        camPrincipal.fieldOfView = Mathf.Lerp(camPrincipal.fieldOfView, fovAlvo, Time.unscaledDeltaTime * velocidadeSuavizacaoZoom);

        // 3. Rotação do Gabinete com o Botão Esquerdo do Mouse
        if (Input.GetMouseButtonDown(0))
        {
            arrastando = true;
            jaInteragiu = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            arrastando = false;
        }

        if (arrastando)
        {
            float mouseX = -Input.GetAxis("Mouse X") * sensibilidadeMouse;
            float mouseY = Input.GetAxis("Mouse Y") * sensibilidadeMouse;

            Transform camTransform = camPrincipal.transform;
            transform.Rotate(camTransform.up, mouseX, Space.World);
            transform.Rotate(camTransform.right, mouseY, Space.World);
        }
        else if (jaInteragiu)
        {
            // Encaixe magnético nas 6 direções
            AplicarImaGlobal();
        }

        // 4. Remoção de Peça com o Botão Direito do Mouse
        if (Input.GetMouseButtonDown(1))
        {
            TentarRemoverPeca();
        }
    }

    public void ResetarZoom()
    {
        fovAlvo = fovMaximo;
    }

    void AplicarImaGlobal()
    {
        Quaternion rotAtual = transform.localRotation;
        Quaternion faceMaisProxima = Quaternion.identity;
        float menorAngulo = float.MaxValue;

        foreach (Quaternion rotFace in rotacoesDasFaces)
        {
            float angulo = Quaternion.Angle(rotAtual, rotFace);

            if (angulo < menorAngulo)
            {
                menorAngulo = angulo;
                faceMaisProxima = rotFace;
            }
        }

        transform.localRotation = Quaternion.Slerp(transform.localRotation, faceMaisProxima, Time.unscaledDeltaTime * velocidadeIma);
    }

    private void TentarRemoverPeca()
    {
        Ray ray = camPrincipal.ScreenPointToRay(Input.mousePosition);

        // Atravessa o Box Collider do pai e atinge as peças filhas
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f, camadaPecas);

        foreach (RaycastHit hit in hits)
        {
            PecaInterativa peca = hit.collider.GetComponentInParent<PecaInterativa>();
            if (peca != null)
            {
                peca.GuardarNoInventario();
                break; // Interrompe para remover apenas a primeira peça interativa encontrada
            }
        }
    }
}