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
    Ram,
    PlacaRede,
    Cabo
}

public enum Encaixe
{
    SlotRam, CaboEnergia, CaboEnergiaCpu, EntradaSata, EntradaSataNvme, Nenhum, CPUSockete, SlotGPU, Cooler, Parafuso
}
public enum Cabos
{
    Sata, 
    ConectorEnergia, 
    ConectorEnergiaCPU, 
    ConectorPower //Sim o botão power do PC
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

    public List<GameObject> prefab;
}
