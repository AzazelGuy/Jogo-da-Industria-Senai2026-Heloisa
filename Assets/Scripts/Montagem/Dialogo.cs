using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Dialogo : MonoBehaviour
{
    [SerializeField] private DialoguePage[] paginas;
    [SerializeField] private string cenaDestino;
    [SerializeField] private bool usarLoadingScreen = true;

    private bool ouvindo;

    private IEnumerator Start()
    {
        // Espera 1 frame: o DialogueControler esconde o próprio grupo no Start dele,
        // e se a gente começar antes, ele apaga o diálogo logo depois de abrir.
        yield return null;

        if (DialogueControler.instance == null || paginas == null || paginas.Length == 0)
        {
            Debug.LogWarning("Sem DialogueControler ou sem páginas, indo direto pra cena.");
            IrParaCena();
            yield break;
        }

        DialogueControler.OnDialogueEnd += AoTerminarDialogo;
        ouvindo = true;
        DialogueControler.instance.StartDialogue(paginas);
    }

    private void AoTerminarDialogo()
    {
        Desinscrever();
        IrParaCena();
        Destroy(DialogueControler.instance.gameObject);
    }

    private void IrParaCena()
    {
        if (usarLoadingScreen && LoadingScreen.Instance != null)
            LoadingScreen.Carregar(cenaDestino);
        else
            SceneManager.LoadScene(cenaDestino);
    }

    private void Desinscrever()
    {
        if (!ouvindo) return;
        DialogueControler.OnDialogueEnd -= AoTerminarDialogo;
        ouvindo = false;
    }

    private void OnDestroy() => Desinscrever();
}