using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum DataVersionType
{
    Max,
    Min,
    MaxFire,
    Default
}

public class DataVersions
{
    public DataVersion[] dataVersions;

    public DataVersions()
    {
        dataVersions = new DataVersion[3];
        dataVersions[(int)DataVersionType.Max] = new DataVersion();
        dataVersions[(int)DataVersionType.Min] = new DataVersion();
        dataVersions[(int)DataVersionType.MaxFire] = new DataVersion();
        //dataVersions[(int)DataVersionType.Default] = new DataVersion();
    }

    public DataVersion this   [DataVersionType type]
    {
        get
        {
            return dataVersions[(int)type];
        }
        set
        {
            dataVersions[(int)type] = value;
        }
    }
}


public class DataVersion
{
    public decimal supplyFromPumpStation { get; set; }
    public decimal supplyFromReservoir{ get; set; }
    public decimal coefficient { get; set; }
    public decimal[] nodesConsumptions { get; set; } = new decimal[8];
    public decimal[] nodesOutflows { get; set; } = new decimal[8];
    public decimal[] nodesInflows { get; set; } = new decimal[8];
    public decimal[] nodesHeight { get; set; } = new decimal[8];
    public decimal[] pipesConsumptions { get; set; } = new decimal[9];
    public bool[] flowDirection { get; set; } = new bool[9];
    public bool[] flowDirectionForUI { get; set; } = new bool[9];
    public decimal[] pipesOutflows { get; set; } = new decimal[9];
    public decimal[] pipesInflows { get; set; } = new decimal[9];
    public decimal[] pipesLength { get; set; } = new decimal[9];
    public decimal[] buildingsHeight { get; set; } = new decimal[9];

    public decimal[][] doubleInflowsOnPipes { get; set; } // idk czemu dziala bez new...

    public Dictionary<int, List<int>> _nodeAndAdjacentPipes { get; set; } = new Dictionary<int, List<int>>();

    public Dictionary<int, List<int>> _pipesAdjacentNodes { get; set; } = new Dictionary<int, List<int>>();

    public List<Pipe> Pipes { get; set; } = new List<Pipe>();
    public List<Node> Nodes { get; set; } = new List<Node>();

    public Vector3[] pipesPositions = Enumerable.Repeat(Vector3.zero, 9).ToArray();
    public Vector3[] nodesPositions = Enumerable.Repeat(Vector3.zero, 8).ToArray();

    //punkt odniesienia dla kierunku przeplywu, wartosc stala, niezmienialna
    //Gdy kierunek wodociagu odpowiada ruchowi wskazowek zegara - true
    public bool[] clockwiseDirection = Enumerable.Repeat(true, 9).ToArray();

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
            supplyFromPumpStation = 188,
            coefficient = 1.75m,
            nodesConsumptions = new decimal[] { 0m, 17m, 12m, 23m, 26m, 29m, 30m, 0m },
            pipesConsumptions = new decimal[] { 0m, 21m, 25m, 11m, 32m, 15m, 26m, 15m, 0m },
            nodesHeight = new decimal[] { 145m, 147m, 146m, 151m, 154m, 159m, 168m, 192m },
            pipesLength = new decimal[] { 150m, 400m, 350m, 320m, 290m, 300m, 315m, 290m, 250m },
            buildingsHeight = new decimal[] { 0m, 20m, 25m, 15m, 20m, 15m, 15m, 15m, 0m },
        };
    }
    public DataVersion DeepCopy()
    {
        DataVersion copy = new DataVersion();

        // Copy primitive and value type properties
        copy.supplyFromPumpStation = this.supplyFromPumpStation;
        copy.supplyFromReservoir = this.supplyFromReservoir;
        copy.coefficient = this.coefficient;

        // Copy arrays
        copy.nodesConsumptions = (decimal[])this.nodesConsumptions.Clone();
        copy.nodesOutflows = (decimal[])this.nodesOutflows.Clone();
        copy.nodesInflows = (decimal[])this.nodesInflows.Clone();
        copy.pipesConsumptions = (decimal[])this.pipesConsumptions.Clone();
        copy.flowDirection = (bool[])this.flowDirection.Clone();
        copy.pipesOutflows = (decimal[])this.pipesOutflows.Clone();
        copy.pipesInflows = (decimal[])this.pipesInflows.Clone();
        copy.nodesHeight = (decimal[])this.nodesHeight.Clone();
        copy.pipesLength = (decimal[])this.pipesLength.Clone();
        copy.buildingsHeight = (decimal[])this.buildingsHeight.Clone();
        copy.flowDirectionForUI = (bool[])this.flowDirectionForUI.Clone();
        copy.pipesPositions = (Vector3[])this.pipesPositions.Clone();
        copy.nodesPositions = (Vector3[])this.nodesPositions.Clone();
        copy.clockwiseDirection = (bool[])this.clockwiseDirection.Clone();

        // Copy jagged array
        copy.doubleInflowsOnPipes = this.doubleInflowsOnPipes.Select(arr => (decimal[])arr.Clone()).ToArray();

        // Copy lists
        copy.Nodes = this.Nodes.Select(node => node.Clone()).ToList();
        copy.Pipes = this.Pipes.Select(pipe => pipe.Clone()).ToList();

        // Copy dictionaries
        copy._nodeAndAdjacentPipes = this._nodeAndAdjacentPipes.ToDictionary(entry => entry.Key, entry => new List<int>(entry.Value));
        copy._pipesAdjacentNodes = this._pipesAdjacentNodes.ToDictionary(entry => entry.Key, entry => new List<int>(entry.Value));

        return copy;
    }
}


public class UIData
{
    public decimal[] nodesRozbiory { get; set; } = new decimal[8];
    public decimal[] pipesRozbiory { get; set; } = new decimal[9];
    public decimal[] pipesInflows { get; set; } = new decimal[9];
    public decimal[] pipesOutflows { get; set; } = new decimal[9];
    public bool[] flowDirection { get; set; } = new bool[9];

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