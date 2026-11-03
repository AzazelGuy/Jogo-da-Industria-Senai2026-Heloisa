using UnityEngine;

public class GerenciadorPontuacaoGlobal : MonoBehaviour
{
    public static GerenciadorPontuacaoGlobal Instancia;

    private const string ChavePontuacaoTotal = "PontuacaoTotal";
    private const string ChaveErrosTotal = "ErrosTotal";

    private void Awake()
    {
        if (Instancia == null)
        {
            Instancia = this;
            DontDestroyOnLoad(gameObject); // Mantem o objeto vivo entre a troca de cenas
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void IniciarNovaSessao()
    {
        PlayerPrefs.SetInt(ChavePontuacaoTotal, 0);
        PlayerPrefs.SetInt(ChaveErrosTotal, 0);
        PlayerPrefs.Save();
    }

    public void AdicionarProgressoDaFase(int pontosDaFase, int errosDaFase)
    {
        int pontuacaoAtual = PlayerPrefs.GetInt(ChavePontuacaoTotal, 0);
        int errosAtuais = PlayerPrefs.GetInt(ChaveErrosTotal, 0);

        PlayerPrefs.SetInt(ChavePontuacaoTotal, pontuacaoAtual + pontosDaFase);
        PlayerPrefs.SetInt(ChaveErrosTotal, errosAtuais + errosDaFase);
        PlayerPrefs.Save();
    }

    public int ObterPontuacaoTotal()
    {
        return PlayerPrefs.GetInt(ChavePontuacaoTotal, 0);
    }

    public int ObterErrosTotal()
    {
        return PlayerPrefs.GetInt(ChaveErrosTotal, 0);
    }
}