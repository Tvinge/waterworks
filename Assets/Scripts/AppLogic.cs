using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;
using System;
using UnityEngine.PlayerLoop;

public class AppLogic : MonoBehaviour
{
    public Action<DataVersion> updateDataVersion;
    //public Action<DataVersion> updateDataSet;

    //public Action updateUIData;
    public Action calculateWaterDistribution;
    public Action iterateWaterDistribution;
    public Action initializeDictionaries;
    public Action resetSimulation;

    DataVersion dataVersion = new DataVersion();
    DataVersion defaultDataVersion = DataVersion.CreateDefault();
    List<DataVersion> dataVersions = new List<DataVersion>();

    List<int> nodesWithOutflowsOnStart = new List<int>();

    private void Awake()
    {
        DeclareInflowArray();
        UstalWskazowkiZegara(defaultDataVersion);
        InitializePositions();

        updateDataVersion += OnDataUpdated;
    }
    void Start()
    {
        CalculateQzbiornika(defaultDataVersion);
        ZasilanieZPompowniZbiornika(defaultDataVersion);
        InflowOnPipesFromStartingFullNodes(defaultDataVersion);
        FindNearbyUIElements(defaultDataVersion);
        TransferUIElementsToData(defaultDataVersion); //TODO: create data, than UI elements
        GetNodesWithOutflowOnStart(defaultDataVersion);
        updateDataVersion?.Invoke(defaultDataVersion);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetApp();
        }
    }

    void GetNodesWithOutflowOnStart(DataVersion dataVersion)
    {
        for (int i = 0; i < dataVersion.nodesHeight.Length; i++)
        {
            if (dataVersion.nodesOutflows[i] > 0)
            {
                nodesWithOutflowsOnStart.Add(i);
            }
        }
    }
    public void CalculateWaterDistributionButton()
    {
        calculateWaterDistribution?.Invoke();
        updateDataVersion?.Invoke(defaultDataVersion);
    }
    void FindNearbyUIElements(DataVersion data)
    {
        data._nodeAndAdjacentPipes.Clear();
        data._pipesAdjacentNodes.Clear();
        for (int i = 0; i < data.nodesHeight.Length; i++)
        {
            SearchForAdjacentPipes(i);
        }
        for (int i = 0; i < data.pipesLength.Length; i++)
        {
            SearchForAdjacentNodes(i);
        }
    }

    void InitializePositions()
    {
        DeclarePipePositions();
        DeclareNodesPositions();

        initializeDictionaries?.Invoke();
    }

    void OnDataUpdated(DataVersion d)
    {
        if (d.coefficient != defaultDataVersion.coefficient || d.zasilanieZPompowni != defaultDataVersion.zasilanieZPompowni)
        {

            defaultDataVersion.coefficient = d.coefficient;
            defaultDataVersion.zasilanieZPompowni = d.zasilanieZPompowni;
            defaultDataVersion.nodesRozbiory = d.nodesRozbiory;
            defaultDataVersion.pipesRozbiory = d.pipesRozbiory;
            defaultDataVersion.pipesLength = d.pipesLength;

            //tutaj dodac reszte zapisywanek po dodaniu kolejnych wartoœci w clasie dataversion
            Start();
        }
    }

    #region
    public static decimal CalculateQzbiornika(DataVersion data)
    {
        data.zasilanieZeZbiornika = CalculateQhmax(data) - data.zasilanieZPompowni;
        return data.zasilanieZeZbiornika;
    }

    public static decimal CalculateQhmax(DataVersion data)
    {
        decimal value = 0;

        foreach (decimal i in data.pipesRozbiory)
        {
            value += i;
        }

        foreach (decimal i in data.nodesRozbiory)
        {
            value += i;
        }
        return value;
    }

    decimal[] ZasilanieZPompowniZbiornika(DataVersion data)
    {
        int p = 0;
        int zPipe = data.pipesLength.Length - 1;
        int zNode = data.nodesHeight.Length - 1;

        data.nodesOutflows[p] = data.zasilanieZPompowni;
        FlowDirection(p, true);

        data.nodesOutflows[zNode] = data.zasilanieZeZbiornika;
        FlowDirection(zPipe, false);

        return data.nodesOutflows;
    }

    decimal[] InflowOnPipesFromStartingFullNodes(DataVersion data)
    {
        int p = 0;
        int zPipe = data.pipesLength.Length - 1;
        int zNode = data.nodesHeight.Length - 1;

        defaultDataVersion.pipesInflows[p] = data.nodesOutflows[p];
        FlowDirection(p, true);

        defaultDataVersion.pipesInflows[zPipe] = data.nodesOutflows[zNode];
        FlowDirection(zPipe, false);

        return data.pipesInflows;
    }

    bool[] UstalWskazowkiZegara(DataVersion data)
    {
        for (int i = 0; i < data.kierunekRuchuWskazowekZegara.Length; i++)
        {
            if (i == 0)
            {
                data.kierunekRuchuWskazowekZegara[i] = true;
            }
            else if (i == data.kierunekRuchuWskazowekZegara.Length - 1)
            {
                data.kierunekRuchuWskazowekZegara[i] = false;
            }
            else
            {
                if (i == 1 || i == 2 || i == 5)
                    data.kierunekRuchuWskazowekZegara[i] = true;
                else
                    data.kierunekRuchuWskazowekZegara[i] = false;
            }
        }
        return data.kierunekRuchuWskazowekZegara;
    }

    bool FlowDirection(int pipeIndex, bool flowDirection)
    {
        defaultDataVersion.flowDirection[pipeIndex] = flowDirection;
        return defaultDataVersion.flowDirection[pipeIndex];
    }

    Vector3[] DeclarePipePositions()
    {
        for (int i = 0; i < defaultDataVersion.pipesLength.Length; i++)
        {
            defaultDataVersion.pipesPositions[i] = GetComponent<Transform>().GetChild(0).GetChild(i).GetComponent<RectTransform>().anchoredPosition;
            //Debug.Log(pipesPositions[i]);
        }
        return defaultDataVersion.pipesPositions;
    }

    Vector3[] DeclareNodesPositions()
    {
        for (int i = 0; i < defaultDataVersion.nodesHeight.Length; i++)
        {
            defaultDataVersion.nodesPositions[i] = GetComponent<Transform>().GetChild(1).GetChild(i).GetComponent<RectTransform>().anchoredPosition;
            //Debug.Log(pipesPositions[i]);
        }
        return defaultDataVersion.nodesPositions;
    }

    Dictionary<int, List<int>> SearchForAdjacentPipes(int nodeIndex)
    {
        Vector3 nodePosition = GetComponent<Transform>().GetChild(1).GetChild(nodeIndex).GetComponent<RectTransform>().anchoredPosition;
        List<int> foundPipes = new List<int>();
        for (int pipeIndex = 0; pipeIndex < defaultDataVersion.pipesLength.Length; pipeIndex++)
        {
            float distance = Vector3.Distance(nodePosition, defaultDataVersion.pipesPositions[pipeIndex]);
            if (distance <= 60)
            {
                //Debug.Log("index " + pipeIndex);
                foundPipes.Add(pipeIndex);
            }
        }
        defaultDataVersion._nodeAndAdjacentPipes.Add(nodeIndex, foundPipes);
        //Debug.Log($"__ node index: {nodeIndex}");
        return defaultDataVersion._nodeAndAdjacentPipes;
    }

    Dictionary<int, List<int>> SearchForAdjacentNodes(int pipeIndex)
    {
        Vector3 pipePosition = GetComponent<Transform>().GetChild(0).GetChild(pipeIndex).GetComponent<RectTransform>().anchoredPosition;
        List<int> foundNodes = new List<int>();
        for (int nodeIndex = 0; nodeIndex < defaultDataVersion.nodesHeight.Length; nodeIndex++)
        {
            float distance = Vector3.Distance(pipePosition, defaultDataVersion.nodesPositions[nodeIndex]);
            //Debug.Log(distance);
            if (distance <= 60)
            {
                //Debug.Log("node index " + nodeIndex);
                foundNodes.Add(nodeIndex);
            }
        }
        defaultDataVersion._pipesAdjacentNodes.Add(pipeIndex, foundNodes);
        return defaultDataVersion._pipesAdjacentNodes; 
    }

    void TransferUIElementsToData(DataVersion dataVersion)
    {
        int pipeCount = dataVersion.pipesOutflows.Length;
        int nodeCount = dataVersion.nodesOutflows.Length;

        List<Pipe> pipes = dataVersion.Pipes;
        List<Node> nodes = dataVersion.Nodes;

        for (int i = 0; i < pipeCount; i++)
        {
            Pipe pipe = new Pipe(i);
            pipe.length = dataVersion.pipesLength[i];
            pipe.rozbiory = dataVersion.pipesRozbiory[i];
            pipes.Add(pipe);

        }

        for (int i = 0; i < nodeCount; i++)
        {
            Node node = new Node();
            node.index = i;
            node.rozbiory = dataVersion.nodesRozbiory[i];
            node.height = dataVersion.nodesHeight[i];
            node.location = transform.GetChild(1).GetChild(i).GetComponent<RectTransform>();
            nodes.Add(node);
        }

        for (int i = 0; i < pipeCount; i++)
        {
            List<int> list = dataVersion._pipesAdjacentNodes[i];

            int node1 = list[0];
            int node2 = list[1];

            pipes[i].inflowNode = nodes[node1];
            pipes[i].outflowNode = nodes[node2];
        }

        for (int i = 0; i < nodeCount; i++)
        {
            List<int> list = dataVersion._nodeAndAdjacentPipes[i];
            List<Pipe> connectedPipes = new List<Pipe>();

            foreach (var pipe in list)
            {
                connectedPipes.Add(pipes[pipe]);
            }
            nodes[i].ConnectedPipes = connectedPipes;
        }
    }

    #endregion

    List<bool> DetermineFlowDirection(Node node)
    {
        List<bool> bools = new List<bool>();

        foreach (var pipe in node.ConnectedPipes)
        {      }
                return bools;
    }

    void DeclareInflowArray()
    {
        for (int i = 0; i < 9; i++)
        {
            if (defaultDataVersion.doubleInflowsOnPipes[i] == null)
            {
                defaultDataVersion.doubleInflowsOnPipes[i] = new decimal[2];
            }
        }
    }

    public void ResetApp()
    {
        Debug.Log("////////////////////////////////////ResetSimulation///////////////////////////////////////////////");
        for (int i = 0; i < defaultDataVersion.pipesOutflows.Length; i++)
        {
            defaultDataVersion.pipesInflows[i] = 0;
            defaultDataVersion.pipesOutflows[i] = 0;

            for (int j = 0; j < defaultDataVersion.doubleInflowsOnPipes[i].Length; j++)
            {
                defaultDataVersion.doubleInflowsOnPipes[i][j] = 0;
            }
        }
        for (int i = 0; i < defaultDataVersion.nodesOutflows.Length; i++)
        {
            defaultDataVersion.nodesInflows[i] = 0;
            defaultDataVersion.nodesOutflows[i] = 0;
        }

        resetSimulation.Invoke();
        Start();
    }
}