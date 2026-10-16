using UnityEngine;

public class AtalhosTeclado : MonoBehaviour
{
    [Header("Referências")]
    public SplashScreen splashScreen;
    public ControleMenu controleMenu;

    void Update()
    {
        // Durante a intro: qualquer tecla/clique pula, e NADA mais é processado
        if (splashScreen != null && splashScreen.EstaEmAndamento())
        {
            if (Input.anyKeyDown) splashScreen.Pular();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (controleMenu == null) return;

            if (controleMenu.EstaNoMenuModos())
                controleMenu.VoltarParaMenuInicial();
            else
                controleMenu.SairDoJogo();
        }
    }
}