using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class UiController : MonoBehaviour
{
    public static UiController Instance;

    [SerializeField] private CanvasGroup _winUi;
    [SerializeField] private TextMeshProUGUI nameText;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }else
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

    private void Completo()
    {
        _winUi.gameObject.SetActive(true);
    }

    public void ChangeActive(bool b)
    {
        _winUi.gameObject.SetActive(b);
    }

    public void ReturntoMainMenu()
    {
        SceneManager.LoadScene("MenuInicial");
        Destroy(gameObject);
        Destroy(StepChecker.Instance.gameObject);
    }
    public CanvasGroup winUI => _winUi;
    public TextMeshProUGUI textNameUI => nameText;
}
