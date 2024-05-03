using Codice.Client.Common.FsNodeReaders.Watcher;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/// <summary>
/// moze courtines do ststemu stopniowego rozplywu wody
/// albo invoke reapeting
/// </summary>
/// 

public interface IDataVersion
{
    public float zasilanieZPompowni { get; set; }
    public float zasilanieZeZbiornika { get; set; }
    public float wspolczynnik { get; set; }
    public float[] nodesRozbiory { get; set; } 
    public float[] nodesOutflow { get; set; } 
    public float[] nodesInflow { get; set; } 
    public float[] pipesRozbiory { get; set; } 
    public bool[] kierunekPrzeplywu { get; set; } 
    public float[] pipesOutflows { get; set; } 
    public float[] pipesInflows { get; set; } 
    public float[][] doubleInflowsOnPipes { get; set; } 

    public Dictionary<int, List<int>> _nodeAndAdjacentPipes { get; set; }

    public Dictionary<int, List<int>> _pipesAdjacentNodes { get; set; }


}


public class DataVersion : IDataVersion
{
    public float zasilanieZPompowni { get; set; }
    public float zasilanieZeZbiornika{ get; set; }
    public float wspolczynnik { get; set; }
    public float[] nodesRozbiory { get; set; } = new float[8];
    public float[] nodesOutflow { get; set; } = new float[8];
    public float[] nodesInflow { get; set; } = new float[8];
    public float[] polozenieWezlow { get; set; } = new float[8];
    public float[] pipesRozbiory { get; set; } = new float[9];
    public bool[] kierunekPrzeplywu { get; set; } = new bool[9];
    public float[] pipesOutflows { get; set; } = new float[9];
    public float[] pipesInflows { get; set; } = new float[9];
    public float[] dlugoscOdcinka { get; set; } = new float[9];
    public float[] wysokoscZabudowy { get; set; } = new float[9];

    public float[][] doubleInflowsOnPipes { get; set; } // idk czemu dziala bez new...

    public Dictionary<int, List<int>> _nodeAndAdjacentPipes { get; set; } = new Dictionary<int, List<int>>();

    public Dictionary<int, List<int>> _pipesAdjacentNodes { get; set; } = new Dictionary<int, List<int>>();

    public Vector3[] pipesPositions = Enumerable.Repeat(Vector3.zero, 9).ToArray();
    public Vector3[] nodesPositions = Enumerable.Repeat(Vector3.zero, 8).ToArray();

    //punkt odniesienia dla kierunku przeplywu, wartosc stala, niezmienialna
    //Gdy kierunek wodociagu odpowiada ruchowi wskazowek zegara - true
    public bool[] kierunekRuchuWskazowekZegara = Enumerable.Repeat(true, 9).ToArray();

    public DataVersion()//constructor
    {
        for (int i = 0; i < nodesOutflow.Length; i++)
        {
            List<int> foundPipes = new List<int>();
            _nodeAndAdjacentPipes.Add(i, foundPipes);
        }
        for (int i = 0; i < pipesInflows.Length; i++)
        {
            List<int> foundNodes = new List<int>();
            _pipesAdjacentNodes.Add(i, foundNodes);
        }


        doubleInflowsOnPipes = new float[9][];

        for (int i = 0; i < doubleInflowsOnPipes.Length; i++)
        {
            doubleInflowsOnPipes[i] = new float[] { 0, 0 };
        }
    }
    public static DataVersion CreateDefault()
    {
        return new DataVersion
        {
            zasilanieZPompowni = 188,
            wspolczynnik = 1.75f,
            nodesRozbiory = new float[] { 0f, 17f, 12f, 23f, 26f, 29f, 30f, 0f },
            pipesRozbiory = new float[] { 0f, 21f, 25f, 11f, 32f, 15f, 26f, 15f, 0f },
            polozenieWezlow = new float[] { 145f, 147f, 146f, 151f, 154f, 159f, 168f, 192f },
            dlugoscOdcinka = new float[] { 150f, 400f, 350f, 320f, 290f, 300f, 315f, 290f, 250f },
            wysokoscZabudowy = new float[] { 0f, 20f, 25f, 15f, 20f, 15f, 15f, 15f, 0f },
        };
    }
}


public class UIData
{
    public float[] nodesRozbiory { get; set; } = new float[8];
    public float[] pipesRozbiory { get; set; } = new float[9];
    public float[] pipesInflows { get; set; } = new float[9];
    public float[] pipesOutflows { get; set; } = new float[9];
    public bool[] kierunekPrzeplywu { get; set; } = new bool[9];

    public float[] nodesOutflow { get; set; } = new float[8];

    public float[][] doubleInflowsOnPipes;

    public UIData()
    {
        doubleInflowsOnPipes = new float[9][];


        for (int i = 0; i < doubleInflowsOnPipes.Length; i++)
        {
            doubleInflowsOnPipes[i] = new float[] { 0, 0 };


        }
    }
}