using UnityEngine;

public class AtalhosTeclado : MonoBehaviour
{
    [Header("Referências")]
    public SplashScreen splashScreen;
    public ControleMenu controleMenu;

    void Update()
    {
        // 🔹 TECLA ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Ignora o comando caso a apresentação ainda esteja rodando
            if (splashScreen != null && splashScreen.EstaEmAndamento())
            {
                return;
            }

            if (controleMenu == null) return;

            // Se estiver no menu de escolha de modos, volta para o menu inicial
            if (controleMenu.EstaNoMenuModos())
            {
                controleMenu.VoltarParaMenuInicial();
            }
            // Se estiver no menu inicial, fecha o jogo
            else
            {
                controleMenu.SairDoJogo();
            }
        }
    }
}