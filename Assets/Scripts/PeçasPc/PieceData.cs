using UnityEngine;

[System.Serializable]
public class PieceData : ScriptableObject
{
    [Header("Identificação da Peça")]
    public string idPeca;               // Ex: "GPU_1050TI_DEF"
    public string nomePeca;             // Ex: "GTX 1050 Ti"

    [Header("Referências Visuais")]
    public Sprite iconePeca;            // Nome ajustado para alinhar com o SlotUI.cs
    public GameObject imagemFichaCanva; // A UI/Image da Ficha inteira posicionada na Coluna 3
    public GameObject modelo3DGabinete; // O objeto 3D correspondente dentro do gabinete
}