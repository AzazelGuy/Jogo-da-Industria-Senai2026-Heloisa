using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Controlador central da UI do jogo: exibe a tela de vitória ao concluir a montagem,
/// mostra o nome da peça sob o cursor e trata o retorno ao menu principal.
/// </summary>
public class UiController : MonoBehaviour
{
    #region Singleton

    public static UiController Instance;

    #endregion

    #region Campos Serializados

    [SerializeField] private CanvasGroup _winUi;
    [SerializeField] private TextMeshProUGUI nameText;

    #endregion

    #region Propriedades Públicas

    public CanvasGroup winUI => _winUi;
    public TextMeshProUGUI textNameUI => nameText;

    #endregion

    #region Ciclo de Vida (Unity)

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // Add event listener
        StepChecker.OnTudoFeito += Completo;
    }

    private void OnDisable()
    {
        // Remove event listener
        StepChecker.OnTudoFeito -= Completo;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    #endregion

    #region Eventos do StepChecker

    /// <summary>
    /// Chamado quando o StepChecker sinaliza que a montagem foi 100% concluída.
    /// Exibe a tela de vitória.
    /// </summary>
    private void Completo()
    {
        _winUi.gameObject.SetActive(true);
    }

    #endregion

    #region API Pública

    /// <summary>
    /// Ativa ou desativa a tela de vitória manualmente.
    /// </summary>
    public void ChangeActive(bool b)
    {
        _winUi.gameObject.SetActive(b);
    }

    /// <summary>
    /// Carrega a cena do menu principal e limpa as instâncias persistentes
    /// (UiController e StepChecker).
    /// </summary>
    public void ReturntoMainMenu()
    {
        SceneManager.LoadScene("MenuInicial");
        Destroy(gameObject);
        Destroy(StepChecker.Instance.gameObject);
    }

    #endregion
}
