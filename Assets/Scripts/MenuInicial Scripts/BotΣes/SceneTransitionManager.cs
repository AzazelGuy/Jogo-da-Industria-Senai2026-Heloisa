using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    private void Awake()
    {
        // Se você não precisa que ele persista entre as cenas, remova o DontDestroyOnLoad.
        // Assim, cada cena usa o seu próprio gerenciador local e nunca corrompe o botão do menu!
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Função para chamar no On Click () do botão
    public void CarregarCena(string nomeDaCena)
    {
        SceneManager.LoadScene(nomeDaCena);
    }
}