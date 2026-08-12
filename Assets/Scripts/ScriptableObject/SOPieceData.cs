using System.Collections.Generic;
using UnityEngine;

public enum Tipo
{
    CPU, //Processador
    GPU, //Placa de Video
    PlacaMae,
    Cooler,
    Armazenamento, //Conectores Sata ou NVME
    Gabinete,
    Fonte,
    Ram, //DDR 3-5
    PlacaRede
}

public enum Encaixe
{
    DDR3, DDR4, DDR5,
    Sata, NVME, CPUSocket, NenhumEspecificado, ConectorEnergia, ConectorEnergiaCPU
}

public enum Cabos
{
    Sata, ConectorEnergia, ConectorEnergiaCPU
}

[CreateAssetMenu(fileName = "Peça", menuName = "Peças/ Nova Peça")]
public class SOPieceData : ScriptableObject
{
    public string ID;
    public string Nome;
    [TextArea(3, 6)]
    public string Descricao;

    //Utiliza-se LIST pois pode incluir variações e mais de 1
    public Tipo tipoDePeca;

    public List<Cabos> cabosUsados;
    public List<Encaixe> EncaixesUtilizados;

    public List<Mesh> modelos;
    public List<Material> materiais;

    public GameObject prefab;
}
