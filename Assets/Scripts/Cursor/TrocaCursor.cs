using UnityEngine;
using UnityEngine.UI;

public class TrocaCursor : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    [Header("Cursor")]
    public Image aberta;
    public Image fechada;
    public Image aponta;
    public Image pasta;
    public Image chave;

    private string cursorSelecionado = "";

    void Start()
    {
        mainCamera = Camera.main;

        aberta.enabled = true;
        fechada.enabled = false;
        aponta.enabled = false;
        pasta.enabled = false;
        chave.enabled = false;
    }

    void Update()
    {
        Ray raio = mainCamera.ScreenPointToRay(Input.mousePosition);

        bool acertouObjeto = Physics.Raycast(raio, out RaycastHit informacao);

        // =====================================================
        // PASTA OU CHAVE JÁ ESTÃO SELECIONADAS
        // =====================================================

        if (cursorSelecionado == "Pasta")
        {
            MostrarCursorPasta();

            if (Input.GetMouseButtonDown(0) && acertouObjeto)
            {
                if (informacao.collider.CompareTag("Pasta"))
                {
                    cursorSelecionado = "";
                    MostrarCursorAberto();
                }
            }

            return;
        }

        if (cursorSelecionado == "Chave")
        {
            MostrarCursorChave();

            if (Input.GetMouseButtonDown(0) && acertouObjeto)
            {
                if (informacao.collider.CompareTag("Chave"))
                {
                    cursorSelecionado = "";
                    MostrarCursorAberto();
                }
            }

            return;
        }

        // =====================================================
        // SE O MOUSE ESTÁ SOBRE ALGUM OBJETO
        // =====================================================

        if (acertouObjeto)
        {
            // Clicou na Pasta
            if (Input.GetMouseButtonDown(0) && informacao.collider.CompareTag("Pasta"))
            {
                cursorSelecionado = "Pasta";
                MostrarCursorPasta();
                return;
            }

            // Clicou na Chave
            if (Input.GetMouseButtonDown(0) && informacao.collider.CompareTag("Chave"))
            {
                cursorSelecionado = "Chave";
                MostrarCursorChave();
                return;
            }

            // Botão esquerdo está sendo segurado
            if (Input.GetMouseButton(0))
            {
                MostrarCursorFechado();
                return;
            }

            // Está apontando para algo selecionável
            if (informacao.collider)
            {
                MostrarCursorAponta();
                return;
            }
        }

        // =====================================================
        // NENHUMA INTERAÇÃO
        // =====================================================

        MostrarCursorAberto();
    }

    // =========================================================
    // FUNÇÕES DOS CURSORES
    // =========================================================

    private void MostrarCursorAberto()
    {
        aberta.enabled = true;
        fechada.enabled = false;
        aponta.enabled = false;
        pasta.enabled = false;
        chave.enabled = false;
    }

    private void MostrarCursorAponta()
    {
        aberta.enabled = false;
        fechada.enabled = false;
        aponta.enabled = true;
        pasta.enabled = false;
        chave.enabled = false;
    }

    private void MostrarCursorFechado()
    {
        aberta.enabled = false;
        fechada.enabled = true;
        aponta.enabled = false;
        pasta.enabled = false;
        chave.enabled = false;
    }

    private void MostrarCursorPasta()
    {
        aberta.enabled = false;
        fechada.enabled = false;
        aponta.enabled = false;
        pasta.enabled = true;
        chave.enabled = false;
    }

    private void MostrarCursorChave()
    {
        aberta.enabled = false;
        fechada.enabled = false;
        aponta.enabled = false;
        pasta.enabled = false;
        chave.enabled = true;
    }
}
