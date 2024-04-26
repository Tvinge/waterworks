using System.Collections.Generic;
/// <summary>
/// moze courtines do ststemu stopniowego rozplywu wody
/// albo invoke reapeting
/// </summary>
/// 
/*
public interface IDataVersion
{
    public float zasilanieZPompowni { get; set; }
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
*/

public class DataVersion //: IDataVersion
{
    public float zasilanieZPompowni { get; set; }
    public float wspolczynnik { get; set; }
    public float[] nodesRozbiory { get; set; } = new float[8];
    public float[] nodesOutflow { get; set; } = new float[8];
    public float[] nodesInflow { get; set; } = new float[8];
    public float[] pipesRozbiory { get; set; } = new float[9];
    public bool[] kierunekPrzeplywu { get; set; } = new bool[9];
    public float[] pipesOutflows { get; set; } = new float[9];
    public float[] pipesInflows { get; set; } = new float[9];
    public float[][] doubleInflowsOnPipes { get; set; } // idk czemu dziala bez new...

    public Dictionary<int, List<int>> _nodeAndAdjacentPipes = new Dictionary<int, List<int>>();

    public Dictionary<int, List<int>> _pipesAdjacentNodes = new Dictionary<int, List<int>>();
    public DataVersion()
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


    public class DefaultVersion //: IDataVersion
    {

        public float zasilanieZPompowni { get; set; } = 188;
        public float wspolczynnik { get; set; }
        public float[] polozenieWezlow { get; set; } = { 145f, 147f, 146f, 151f, 154f, 159f, 168f, 192f };
        public float[] dlugoscOdcinka = { 150f, 400f, 350f, 320f, 290f, 300f, 315f, 290f, 250f };
        float[] wysokoscZabudowy = { 0f, 20f, 25f, 15f, 20f, 15f, 15f, 15f, 0f };
        public float[] nodesRozbiory { get; set; } = { 0f, 17f, 12f, 23f, 26f, 29f, 30f, 0f };
        public float[] pipesRozbiory { get; set; } = { 0f, 21f, 25f, 11f, 32f, 15f, 26f, 15f, 0f };
        public float[] nodesOutflow { get; set; } = new float[8];
        public float[] nodesInflow { get; set; } = new float[8];
        public bool[] kierunekPrzeplywu { get; set; } = new bool[9];
        public float[] pipesOutflows { get; set; } = new float[9];
        public float[] pipesInflows { get; set; } = new float[9];
        public float[][] doubleInflowsOnPipes;


        public DefaultVersion()
        {
            doubleInflowsOnPipes = new float[9][];


            for (int i = 0; i < doubleInflowsOnPipes.Length; i++)
            {
                doubleInflowsOnPipes[i] = new float[] { 0, 0 };


            }
        }

        public Dictionary<int, List<int>> _nodeAndAdjacentPipes = new Dictionary<int, List<int>>();

        public Dictionary<int, List<int>> _pipesAdjacentNodes = new Dictionary<int, List<int>>();

    }





}