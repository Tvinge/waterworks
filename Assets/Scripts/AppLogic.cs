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
    public Action<DataVersions> updateDataVersions;
    //public Action<DataVersion> updateDataSet;

    //public Action updateUIData;
    public Action<DataVersion> calculateWaterDistribution;
    public Action iterateWaterDistribution;
    public Action initializeDictionaries;
    public Action resetSimulation;

    DataVersion defaultDataVersion = DataVersion.CreateDefault();
    DataVersion dataVersion = new DataVersion();
    DataVersions dataVersions = new DataVersions();

    DataVersion maxDataVersion;
    DataVersion minDataVersion;
    DataVersion maxFireDataVersion;

    //List<DataVersion> dataVersions = new List<DataVersion>();

    List<int> nodesWithOutflowsOnStart = new List<int>();

    private bool isFirstMinCalculation = true;
    private void Awake()
    {
        DeclareInflowArray();
        SetClockDirection(defaultDataVersion);
        InitializePositions();

        updateDataVersion += OnDataUpdated;
    }
    void Start()
    {
        defaultDataVersion.Pipes.Clear();
        defaultDataVersion.Nodes.Clear();

        CalculateSupplyFromReservoir(defaultDataVersion);
        SetOutflowsFromPumpStationAndReservoir(defaultDataVersion);
        InflowOnPipesFromStartingFullNodes(defaultDataVersion);
        FindNearbyUIElements(defaultDataVersion);
        TransferUIElementsToData(defaultDataVersion); //TODO: create data, than UI elements
        GetNodesWithOutflowOnStart(defaultDataVersion);



        maxDataVersion = defaultDataVersion;
        minDataVersion = defaultDataVersion;

        //maxDataVersion = defaultDataVersion.DeepCopy();
        //minDataVersion = defaultDataVersion.DeepCopy();
        //maxFireDataVersion = defaultDataVersion.DeepCopy();

        //dataVersions.maxDataVersion = maxDataVersion;
        //dataVersions.minDataVersion = minDataVersion;
        //dataVersions.maxFireDataVersion = maxFireDataVersion;
        //dataVersions.defaultDataVersion = defaultDataVersion;

        updateDataVersion?.Invoke(defaultDataVersion);

    }

    void CalculateQForMin()
    {
        foreach (var node in minDataVersion.Nodes)
        {
            node.consumption = maxDataVersion.nodesConsumptions[node.index] / minDataVersion.coefficient;
        }
        foreach (var pipe in minDataVersion.Pipes)
        {
            pipe.consumption = maxDataVersion.pipesConsumptions[pipe.index] / minDataVersion.coefficient;
        }
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
    public void CalculateMaxWaterDistributionButton()
    {
        calculateWaterDistribution?.Invoke(maxDataVersion);
        updateDataVersion?.Invoke(maxDataVersion);
    }
    public void CalculateMinWDButton()
    {

        int nodeCount = 0;
        int pipeCount = 0;
        //if (isFirstMinCalculation == true)
        {
            foreach (var node in minDataVersion.Nodes)
            {
                node.consumption = maxDataVersion.nodesConsumptions[node.index] / minDataVersion.coefficient;
                minDataVersion.nodesConsumptions[node.index] = node.consumption;
                nodeCount++;
            }
            foreach (var pipe in minDataVersion.Pipes)
            {
                pipe.consumption = maxDataVersion.pipesConsumptions[pipe.index] / minDataVersion.coefficient;
                minDataVersion.pipesConsumptions[pipe.index] = pipe.consumption;
                minDataVersion.flowDirection[pipe.index] = true;
                pipeCount++;
            }
            minDataVersion.nodesOutflows[7] = 0;
            isFirstMinCalculation = false;
        }
        calculateWaterDistribution?.Invoke(minDataVersion);
        updateDataVersion?.Invoke(minDataVersion);
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
        if (d.coefficient != defaultDataVersion.coefficient || d.supplyFromPumpStation != defaultDataVersion.supplyFromPumpStation)
        {

            defaultDataVersion.coefficient = d.coefficient;
            defaultDataVersion.supplyFromPumpStation = d.supplyFromPumpStation;
            defaultDataVersion.nodesConsumptions = d.nodesConsumptions;
            defaultDataVersion.pipesConsumptions = d.pipesConsumptions;
            defaultDataVersion.pipesLength = d.pipesLength;

            //tutaj dodac reszte zapisywanek po dodaniu kolejnych wartoœci w clasie dataversion
            Start();
        }
    }

    #region
    public static decimal CalculateSupplyFromReservoir(DataVersion data)
    {
        decimal consumptionOnRings = CalculateQhmax(data); 
        if (consumptionOnRings > data.supplyFromPumpStation)
            data.supplyFromReservoir = consumptionOnRings - data.supplyFromPumpStation ;
        else
            data.supplyFromReservoir = 0;

        return data.supplyFromReservoir;
    }

    public static decimal CalculateQhmax(DataVersion data)
    {
        decimal value = 0;

        foreach (decimal i in data.pipesConsumptions)
        {
            value += i;
        }

        foreach (decimal i in data.nodesConsumptions)
        {
            value += i;
        }
        return value;
    }

    decimal[] SetOutflowsFromPumpStationAndReservoir(DataVersion data)
    {
        int p = 0;
        int zPipe = data.pipesLength.Length - 1;
        int zNode = data.nodesHeight.Length - 1;

        data.nodesOutflows[p] = data.supplyFromPumpStation;
        FlowDirection(p, true);

        if (data.supplyFromReservoir > 0)
        {
            data.nodesOutflows[zNode] = data.supplyFromReservoir;
            FlowDirection(zPipe, false);
        }


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

    bool[] SetClockDirection(DataVersion data)
    {
        for (int i = 0; i < data.clockwiseDirection.Length; i++)
        {
            if (i == 0)
            {
                data.clockwiseDirection[i] = true;
            }
            else if (i == data.clockwiseDirection.Length - 1)
            {
                data.clockwiseDirection[i] = false;
            }
            else
            {
                if (i == 1 || i == 2 || i == 5)
                    data.clockwiseDirection[i] = true;
                else
                    data.clockwiseDirection[i] = false;
            }
        }
        return data.clockwiseDirection;
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
            pipe.consumption = dataVersion.pipesConsumptions[i];
            pipes.Add(pipe);

        }

        for (int i = 0; i < nodeCount; i++)
        {
            Node node = new Node();
            node.index = i;
            node.consumption = dataVersion.nodesConsumptions[i];
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
        defaultDataVersion = DataVersion.CreateDefault();
        InitializePositions();
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