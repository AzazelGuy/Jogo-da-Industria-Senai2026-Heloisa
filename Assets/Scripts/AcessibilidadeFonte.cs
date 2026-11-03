using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class AcessibilidadeFonte : MonoBehaviour
{
    public static AcessibilidadeFonte Instance { get; private set; }

    [SerializeField] private TMP_FontAsset fonteDislexia;

    public bool Ativa { get; private set; }
    public event Action<bool> AoMudar;

    // guarda a fonte original de cada texto que a gente alterou
    private readonly Dictionary<TMP_Text, TMP_FontAsset> originais = new();

    const string Chave = "fonte_dislexia";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Ativa = PlayerPrefs.GetInt(Chave, 0) == 1;
        SceneManager.sceneLoaded += AoCarregarCena;
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= AoCarregarCena;
    }

    void AoCarregarCena(Scene cena, LoadSceneMode modo)
    {
        LimparMortos();
        if (Ativa) AplicarEmTudo();
    }

    public void Definir(bool ativa)
    {
        if (Ativa == ativa) return;
        Ativa = ativa;
        PlayerPrefs.SetInt(Chave, ativa ? 1 : 0);
        PlayerPrefs.Save();

        if (Ativa) AplicarEmTudo();
        else RestaurarTudo();

        AoMudar?.Invoke(Ativa);
    }

    // Use isso em texto criado em runtime (ou deixe o TextoAcessivel chamar)
    public void Aplicar(TMP_Text t)
    {
        if (t == null || fonteDislexia == null) return;

        if (Ativa)
        {
            if (t.font == fonteDislexia) return;
            if (!originais.ContainsKey(t)) originais[t] = t.font;
            t.font = fonteDislexia;
        }
        else if (originais.TryGetValue(t, out var original))
        {
            if (original != null) t.font = original;
            originais.Remove(t);
        }
    }

    void AplicarEmTudo()
    {
        var textos = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in textos) Aplicar(t);
    }

    void RestaurarTudo()
    {
        LimparMortos();
        foreach (var t in new List<TMP_Text>(originais.Keys)) Aplicar(t);
    }

    // textos de cenas descarregadas viram "null fantasma" no dicionário
    void LimparMortos()
    {
        var mortos = new List<TMP_Text>();
        foreach (var t in originais.Keys) if (t == null) mortos.Add(t);
        foreach (var t in mortos) originais.Remove(t);
    }
}