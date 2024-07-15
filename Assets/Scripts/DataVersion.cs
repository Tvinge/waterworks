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
    public decimal zasilanieZPompowni { get; set; }
    public decimal zasilanieZeZbiornika { get; set; }
    public decimal coefficient { get; set; }
    public decimal[] nodesRozbiory { get; set; } 
    public decimal[] nodesOutflows { get; set; } 
    public decimal[] nodesInflows { get; set; } 
    public decimal[] pipesRozbiory { get; set; } 
    public bool[] kierunekPrzeplywu { get; set; } 
    public decimal[] pipesOutflows { get; set; } 
    public decimal[] pipesInflows { get; set; } 
    public decimal[][] doubleInflowsOnPipes { get; set; } 

    public Dictionary<int, List<int>> _nodeAndAdjacentPipes { get; set; }

    public Dictionary<int, List<int>> _pipesAdjacentNodes { get; set; }


}


public class DataVersion : IDataVersion
{
    public decimal zasilanieZPompowni { get; set; }
    public decimal zasilanieZeZbiornika{ get; set; }
    public decimal coefficient { get; set; }
    public decimal[] nodesRozbiory { get; set; } = new decimal[8];
    public decimal[] nodesOutflows { get; set; } = new decimal[8];
    public decimal[] nodesInflows { get; set; } = new decimal[8];
    public decimal[] nodesLocation { get; set; } = new decimal[8];
    public decimal[] pipesRozbiory { get; set; } = new decimal[9];
    public bool[] kierunekPrzeplywu { get; set; } = new bool[9];
    public decimal[] pipesOutflows { get; set; } = new decimal[9];
    public decimal[] pipesInflows { get; set; } = new decimal[9];
    public decimal[] pipeLenght { get; set; } = new decimal[9];
    public decimal[] wysokoscZabudowy { get; set; } = new decimal[9];

    public decimal[][] doubleInflowsOnPipes { get; set; } // idk czemu dziala bez new...

    public Dictionary<int, List<int>> _nodeAndAdjacentPipes { get; set; } = new Dictionary<int, List<int>>();

    public Dictionary<int, List<int>> _pipesAdjacentNodes { get; set; } = new Dictionary<int, List<int>>();

    public Vector3[] pipesPositions = Enumerable.Repeat(Vector3.zero, 9).ToArray();
    public Vector3[] nodesPositions = Enumerable.Repeat(Vector3.zero, 8).ToArray();

    //punkt odniesienia dla kierunku przeplywu, wartosc stala, niezmienialna
    //Gdy kierunek wodociagu odpowiada ruchowi wskazowek zegara - true
    public bool[] kierunekRuchuWskazowekZegara = Enumerable.Repeat(true, 9).ToArray();

    public DataVersion()//constructor
    {
        for (int i = 0; i < nodesOutflows.Length; i++)
        {
            List<int> foundPipes = new List<int>();
            _nodeAndAdjacentPipes.Add(i, foundPipes);
        }
        for (int i = 0; i < pipesInflows.Length; i++)
        {
            List<int> foundNodes = new List<int>();
            _pipesAdjacentNodes.Add(i, foundNodes);
        }


        doubleInflowsOnPipes = new decimal[9][];

        for (int i = 0; i < doubleInflowsOnPipes.Length; i++)
        {
            doubleInflowsOnPipes[i] = new decimal[] { 0, 0 };
        }
    }
    public static DataVersion CreateDefault()
    {
        return new DataVersion
        {
            zasilanieZPompowni = 188,
            coefficient = 1.75m,
            nodesRozbiory = new decimal[] { 0m, 17m, 12m, 23m, 26m, 29m, 30m, 0m },
            pipesRozbiory = new decimal[] { 0m, 21m, 25m, 11m, 32m, 15m, 26m, 15m, 0m },
            nodesLocation = new decimal[] { 145m, 147m, 146m, 151m, 154m, 159m, 168m, 192m },
            pipeLenght = new decimal[] { 150m, 400m, 350m, 320m, 290m, 300m, 315m, 290m, 250m },
            wysokoscZabudowy = new decimal[] { 0m, 20m, 25m, 15m, 20m, 15m, 15m, 15m, 0m },
        };
    }
}

public class UIData
{
    public decimal[] nodesRozbiory { get; set; } = new decimal[8];
    public decimal[] pipesRozbiory { get; set; } = new decimal[9];
    public decimal[] pipesInflows { get; set; } = new decimal[9];
    public decimal[] pipesOutflows { get; set; } = new decimal[9];
    public bool[] kierunekPrzeplywu { get; set; } = new bool[9];

    public decimal[] nodesOutflow { get; set; } = new decimal[8];

    public decimal[][] doubleInflowsOnPipes;

    public UIData()
    {
        doubleInflowsOnPipes = new decimal[9][];


        for (int i = 0; i < doubleInflowsOnPipes.Length; i++)
        {
            doubleInflowsOnPipes[i] = new decimal[] { 0, 0 };


        }
    }
}