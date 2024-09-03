using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class CalculationManager : MonoBehaviour
{
    AppLogic appLogic;
    DataLoader dataLoader;
    bool[] isCalculated = new bool[8];
    static bool calculations = false;
    int stepCount = 0;
    int stepCountBackward = 7;

    DataVersions dataVersions = new DataVersions();
    DataVersion dataVersion = new DataVersion();

    private void Awake()
    {
        appLogic = FindObjectOfType<AppLogic>();
        dataLoader = FindObjectOfType<DataLoader>();
        //appLogic.updateDataVersion += OnDataUpdated;
        appLogic.updateDataVersions += OnDatasUpdated;

        appLogic.calculateWaterDistribution += CalculateWholeIteration;
        appLogic.resetSimulation += ResetValues;
    }

    private void ResetValues()
    {
        calculations = false;
        stepCount = 0;
        stepCountBackward = 7;
        for (int i = 0; i < isCalculated.Length; i++)
        {
            isCalculated[i] = false;
        }
    }

    void OnDatasUpdated(DataVersions datas)
    {
        dataVersions = datas;
    }
    //void OnDataUpdated(DataVersion d)
    //{
    //    dataVersion = d;
    //}


    #region FlowCalculations
    public void CalculateWholeIteration(DataVersions datas)
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (datas.dataVersions[i] != null)
                {
                    SearchForNextNodeIndexAndCalculateIt(datas.dataVersions[i]);
                }
            }
            ResetValues();
        }
    }
    void SearchForNextNodeIndexAndCalculateIt(DataVersion d)
    {
        if (isCalculated[stepCount] == true && isCalculated[stepCountBackward] == true)
            return;

        if (d.nodesOutflows[stepCount] > 0)
        {
            //KierunekPrzeplywu(stepCount, true);
            CalculateWaterFlow(d, stepCount);
            Debug.Log($"stepCount {stepCount}");
            isCalculated[stepCount] = true;
            stepCount += 1;
        }
        else
        {
            //KierunekPrzeplywu(stepCountBackward, false);
            CalculateWaterFlow(d, stepCountBackward);
            Debug.Log($"stepCountBackward {stepCountBackward}");
            isCalculated[stepCountBackward] = true;
            calculations = true;
            stepCountBackward -= 1;
        }
    }

    void CalculateWaterFlow(DataVersion d,int nodeIndex)
    {
        List<int> adjacentPipes = d._nodeAndAdjacentPipes[nodeIndex];
        CalculateInflowOnPipesNextToNode(d, nodeIndex);
        for (int i = 0; i < adjacentPipes.Count; i++)
        {
            CalculateOutflowOnPipe(d, adjacentPipes[i]);
            CalculateOutflowOnNode(d, ReturnAdjacentNode(d, adjacentPipes[i], nodeIndex));
        }
    }

    void CalculateInflowOnPipesNextToNode(DataVersion d, int nodeIndex)
    {
        List<int> uncalculatedPipeIndexes = CheckIfPipesAreCalculated(d, nodeIndex);
        List<(int pipeIndex, int[] adjacentNodes)> adjacentNodesToAdjacentPipes = new List<(int, int[])>();

        for (int i = 0; i < uncalculatedPipeIndexes.Count; i++)
        {
            List<int> adjacentNodesToPipe = d._pipesAdjacentNodes[uncalculatedPipeIndexes[i]];
            int[] adjacentNodes = new int[2];
            adjacentNodes[0] = nodeIndex;
            adjacentNodes[1] = ReturnAdjacentNode(d, uncalculatedPipeIndexes[i], nodeIndex);
            adjacentNodesToAdjacentPipes.Add((uncalculatedPipeIndexes[i], adjacentNodes));
        }
        SetOutFlowOnNodeBasedOnOutflowsOnAdjacantNodes(d, nodeIndex, uncalculatedPipeIndexes, adjacentNodesToAdjacentPipes);
    }

    static List<int> CheckIfPipesAreCalculated(DataVersion dataVersion, int nodeIndex)
    {
        List<int> adjacentPipes = dataVersion._nodeAndAdjacentPipes[nodeIndex];
        List<int> uncalculatedPipeIndexes = new List<int>();

        for (int i = 0; i < adjacentPipes.Count; i++)
        {
            Debug.Log($"CalculateInFlowOnPipesNextNode. pipe index: {adjacentPipes[i]}, node: {ReturnAdjacentNode(dataVersion, adjacentPipes[i], nodeIndex)}");
            if (dataVersion.pipesInflows[adjacentPipes[i]] == 0 || dataVersion.nodesOutflows[ReturnAdjacentNode(dataVersion, adjacentPipes[i], nodeIndex)] == 0)
            {
                Debug.Log("dodana do nieobliczonych");
                uncalculatedPipeIndexes.Add(adjacentPipes[i]);
            }
        }
        return uncalculatedPipeIndexes;
    }
    void SetOutFlowOnNodeBasedOnOutflowsOnAdjacantNodes(DataVersion dataVersion, int nodeIndex, List<int> uncalculatedPipeIndexes, List<(int pipeIndex, int[] adjacentNodes)> adjacentNodesToAdjacentPipes)
    {
        for (int i = 0; i < uncalculatedPipeIndexes.Count; i++)
        {
            //outflow on adjacentNodes[1] in adj...[i] pipe
            decimal IOutflow = dataVersion.nodesOutflows[adjacentNodesToAdjacentPipes[i].adjacentNodes[1]];
            if (uncalculatedPipeIndexes.Count > 1)
            {
                SetOutflowOnNodeWithTwoUncalculatedPipes(dataVersion, i, nodeIndex, IOutflow,uncalculatedPipeIndexes, adjacentNodesToAdjacentPipes);
            }
            else if (uncalculatedPipeIndexes.Count == 1)
            {
                SetOutflowOnNodeWithOneUncalculatedPipe(dataVersion, i, nodeIndex, uncalculatedPipeIndexes);
            }
        }
    }

    public static decimal[][] SetOutflowOnNodeWithTwoUncalculatedPipes(DataVersion dataVersion, int i, int nodeIndex, decimal IOutflow, List<int> uncalculatedPipeIndexes, List<(int pipeIndex, int[] adjacentNodes)> adjacentNodesToAdjacentPipes) 
    {
        for (int j = i + 1; j < uncalculatedPipeIndexes.Count; j++)
        {
            decimal JOutflow = dataVersion.nodesOutflows[adjacentNodesToAdjacentPipes[j].adjacentNodes[1]];

            int debugIIndexValue = adjacentNodesToAdjacentPipes[i].adjacentNodes[1];
            int debugJIndexValue = adjacentNodesToAdjacentPipes[j].adjacentNodes[1];

            if (IOutflow == 0 && JOutflow == 0)
            {
                CalculateNodeWhileAdjacentNodesHaveNoOutflow(dataVersion, nodeIndex, i, j, uncalculatedPipeIndexes, adjacentNodesToAdjacentPipes);
            }
            else if (IOutflow > 0 && JOutflow == 0)
            {
                bool znak = true;
                ComparisionOfTwoPipes(dataVersion, znak, i, j, nodeIndex, uncalculatedPipeIndexes);
            }
            else if (IOutflow == 0 && JOutflow > 0)
            {
                bool znak = false;
                ComparisionOfTwoPipes(dataVersion, znak, i, j, nodeIndex, uncalculatedPipeIndexes);
            }
            else if (IOutflow > 0 && JOutflow > 0) ///work this  out
            {
                CalculateOutflowOnNodeWhileAdjacentNodesAreFullOfWater(dataVersion, nodeIndex, uncalculatedPipeIndexes, i, j);
            }
            StuffToDoAfterCalculatingPipe(dataVersion, j, nodeIndex, uncalculatedPipeIndexes);
            StuffToDoAfterCalculatingPipe(dataVersion, i, nodeIndex, uncalculatedPipeIndexes);
        }
        return dataVersion.doubleInflowsOnPipes;
    }

    public static decimal[][] SetOutflowOnNodeWithOneUncalculatedPipe(DataVersion dataVersion,int i, int nodeIndex, List<int> uncalculatedPipeIndexes)
    {
        if (dataVersion.nodesInflows[nodeIndex] == dataVersion.nodesConsumptions[nodeIndex])
            return dataVersion.doubleInflowsOnPipes;

        Debug.Log($"less than 2 unculculated pipes, case B pipeindex: {uncalculatedPipeIndexes[i]} nodeindex: {nodeIndex}");

        if (dataVersion.nodesOutflows[nodeIndex] > dataVersion.pipesConsumptions[uncalculatedPipeIndexes[i]])
        {
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = dataVersion.nodesOutflows[nodeIndex];
        }
        else
        {
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = dataVersion.nodesOutflows[nodeIndex];
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1] = dataVersion.pipesConsumptions[uncalculatedPipeIndexes[i]] - dataVersion.nodesOutflows[nodeIndex];
            dataVersion.nodesInflows[ReturnAdjacentNode(dataVersion, uncalculatedPipeIndexes[i], nodeIndex)] -= dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1];
        }
        StuffToDoAfterCalculatingPipe(dataVersion, i, nodeIndex, uncalculatedPipeIndexes);
        return dataVersion.doubleInflowsOnPipes;
    }

    static decimal[][] CalculateNodeWhileAdjacentNodesHaveNoOutflow(DataVersion dataVersion, int nodeIndex, int i, int j, List<int> uncalculatedPipeIndexes, List<(int pipeIndex, int[] adjacentNodes)> adjacentNodesToAdjacentPipes)
    {
        decimal IInflows = dataVersion.nodesInflows[adjacentNodesToAdjacentPipes[i].adjacentNodes[1]];
        decimal IConsumptions = dataVersion.nodesConsumptions[adjacentNodesToAdjacentPipes[i].adjacentNodes[1]];
        decimal JInflows = dataVersion.nodesInflows[adjacentNodesToAdjacentPipes[j].adjacentNodes[1]];
        decimal JConsumptions = dataVersion.nodesConsumptions[adjacentNodesToAdjacentPipes[j].adjacentNodes[1]];

        if (IInflows > 0 && JInflows > 0)
        {
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = IConsumptions - IInflows + dataVersion.pipesConsumptions[uncalculatedPipeIndexes[i]];
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = JConsumptions - JInflows + dataVersion.pipesConsumptions[uncalculatedPipeIndexes[j]];
        }
        else if (IInflows > 0 && JInflows <= 0)
        {
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = IConsumptions - IInflows + dataVersion.pipesConsumptions[uncalculatedPipeIndexes[i]];
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = dataVersion.nodesOutflows[nodeIndex] - (IConsumptions - IInflows + dataVersion.pipesConsumptions[uncalculatedPipeIndexes[i]]);
        }
        else if (IInflows <= 0 && JInflows > 0)
        {
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = dataVersion.nodesOutflows[nodeIndex] - (JConsumptions - JInflows + dataVersion.pipesConsumptions[uncalculatedPipeIndexes[j]]);
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = JConsumptions - JInflows + dataVersion.pipesConsumptions[uncalculatedPipeIndexes[j]];
        }
        else if (IInflows <= 0 && JInflows <= 0)
        {
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = dataVersion.nodesOutflows[nodeIndex] / uncalculatedPipeIndexes.Count;
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = dataVersion.nodesOutflows[nodeIndex] / uncalculatedPipeIndexes.Count;
        }
        return dataVersion.doubleInflowsOnPipes;
    }

    static decimal[][] CalculateOutflowOnNodeWhileAdjacentNodesAreFullOfWater(DataVersion dataVersion, int nodeIndex, List<int> uncalculatedPipeIndexes, int i, int j)
    {
        if (dataVersion.pipesInflows[uncalculatedPipeIndexes[i]] == dataVersion.pipesConsumptions[uncalculatedPipeIndexes[i]] && dataVersion.pipesInflows[uncalculatedPipeIndexes[j]] == dataVersion.pipesConsumptions[uncalculatedPipeIndexes[j]])
        {
            Debug.Log($"case Ada");
        }
        else if (dataVersion.pipesInflows[uncalculatedPipeIndexes[i]] < dataVersion.pipesConsumptions[uncalculatedPipeIndexes[i]] && dataVersion.pipesInflows[uncalculatedPipeIndexes[j]] == dataVersion.pipesConsumptions[uncalculatedPipeIndexes[j]])
        {
            Debug.Log($"case Adb");
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1] = dataVersion.nodesOutflows[nodeIndex];
        }
        else if (dataVersion.pipesInflows[uncalculatedPipeIndexes[i]] == dataVersion.pipesConsumptions[uncalculatedPipeIndexes[i]] && dataVersion.pipesInflows[uncalculatedPipeIndexes[j]] < dataVersion.pipesConsumptions[uncalculatedPipeIndexes[j]])
        {
            Debug.Log($"case Adc");
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = dataVersion.nodesOutflows[nodeIndex];
            AddupInflows(dataVersion, uncalculatedPipeIndexes[j]);

            if (dataVersion.pipesInflows[uncalculatedPipeIndexes[j]] < dataVersion.pipesConsumptions[uncalculatedPipeIndexes[j]])
            {
                dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][1] = dataVersion.pipesConsumptions[uncalculatedPipeIndexes[j]] - dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][1];
            }
        }
        else if (dataVersion.pipesInflows[uncalculatedPipeIndexes[i]] < dataVersion.pipesConsumptions[uncalculatedPipeIndexes[i]] && dataVersion.pipesInflows[uncalculatedPipeIndexes[j]] < dataVersion.pipesConsumptions[uncalculatedPipeIndexes[j]])
        {
            Debug.Log($"case Add");
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1] = dataVersion.nodesOutflows[nodeIndex];
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][1] = dataVersion.nodesOutflows[nodeIndex];
        }
        else
        {
            Debug.Log($"case Ade");
        }
        return dataVersion.doubleInflowsOnPipes;
    }
    static decimal[] CalculateOutflowOnNode(DataVersion d, int nodeIndex)
    {
        List<int> adjacentPipesIndexes = d._nodeAndAdjacentPipes[nodeIndex];
        List<int> emptyPipeIndexes = new List<int>();
        List<int> fullPipeIndexes = new List<int>();

        for (int i = 0; i < adjacentPipesIndexes.Count; i++)
        {
            if (d.pipesOutflows[adjacentPipesIndexes[i]] > 0 && d.nodesOutflows[nodeIndex] > 0)
            {
                Debug.Log($"calculated node index: {nodeIndex}, pipeindex: {adjacentPipesIndexes[i]}");
            }
            else if (d.pipesOutflows[adjacentPipesIndexes[i]] > 0)
            {
                fullPipeIndexes.Add(adjacentPipesIndexes[i]);
            }
            else
            {
                emptyPipeIndexes.Add(adjacentPipesIndexes[i]);
            }
        }
        if (d.nodesInflows[nodeIndex] > 0)
            d.nodesInflows[nodeIndex] = 0;

        for (int i = 0; i < fullPipeIndexes.Count; i++)
        {
            d.nodesInflows[nodeIndex] += d.pipesOutflows[fullPipeIndexes[i]];
        }

        if (d.nodesInflows[nodeIndex] > d.nodesConsumptions[nodeIndex])
            d.nodesOutflows[nodeIndex] = d.nodesInflows[nodeIndex] - d.nodesConsumptions[nodeIndex];

        return d.nodesOutflows;
    }

    static void StuffToDoAfterCalculatingPipe(DataVersion dataVersion, int pipeIndex, int nodeIndex, List<int> uncalculatedPipeIndexes)
    {

        bool znak = DirectionUI(uncalculatedPipeIndexes[pipeIndex], dataVersion);
        FlowDirectionForUI(dataVersion, uncalculatedPipeIndexes[pipeIndex], znak);
        AddupInflows(dataVersion, uncalculatedPipeIndexes[pipeIndex]);
    }

    static bool DirectionUI(int pipeIndex, DataVersion data)
    {
        bool znak = false;

        if (pipeIndex == 0)
        {
            znak = true;
        }
        if (pipeIndex == 1)
        {
            znak = true;
        }
        if (pipeIndex == 2)
        {
            znak = true;
        }
        if (pipeIndex == 4)
        {
            znak = true;
        }
        if (pipeIndex == 5)
        {
            znak = true;
        }
        if (pipeIndex == 7)
        {
            znak = true;
        }

        /*
        decimal one = data.doubleInflowsOnPipes[pipeIndex][0];
        decimal two = data.doubleInflowsOnPipes[pipeIndex][1];
        if (one > two)
        {
            znak = true;
        }*/

        return znak;
    }


    public static decimal[][] ComparisionOfTwoPipes(DataVersion d, bool znak, int _i, int _j, int nodeIndex, List<int> uncalculatedPipeIndexes)
    {
        int i;
        int j;
        if (znak == true)
        {
            i = _i;
            j = _j;
        }
        else
        {
            j = _i;
            i = _j;
        }

        int pipeIndexI = uncalculatedPipeIndexes[i];
        int pipeIndexJ = uncalculatedPipeIndexes[j];

        if (d.pipesInflows[pipeIndexI] > d.pipesConsumptions[pipeIndexI])
        {
            d.nodesInflows[nodeIndex] += CalculateOutflowOnPipe(d, pipeIndexI);
            d.nodesOutflows[nodeIndex] += CalculateOutflowOnPipe(d, pipeIndexI);
            d.doubleInflowsOnPipes[pipeIndexJ][0] = d.nodesOutflows[nodeIndex];
        }
        else if (d.pipesInflows[pipeIndexI] == d.pipesConsumptions[pipeIndexI])
        {
            CalculateOutflowOnPipe(d, pipeIndexI);
            d.doubleInflowsOnPipes[pipeIndexJ][0] = d.nodesOutflows[nodeIndex];
        }
        else if (d.pipesInflows[pipeIndexI] < d.pipesConsumptions[pipeIndexI])
        {
            if (CalculateOutflowOnPipe(d, pipeIndexI) == 0)
            {
                d.doubleInflowsOnPipes[pipeIndexI][1] = (d.pipesConsumptions[pipeIndexI] - d.pipesInflows[pipeIndexI]);
                d.doubleInflowsOnPipes[pipeIndexJ][0] = d.nodesOutflows[nodeIndex] - d.doubleInflowsOnPipes[pipeIndexI][1];
            }
            else
            {
                d.doubleInflowsOnPipes[pipeIndexI][0] = -1 * (d.pipesOutflows[pipeIndexI] - d.pipesInflows[pipeIndexI]);
                d.doubleInflowsOnPipes[pipeIndexJ][0] = d.nodesOutflows[nodeIndex] - d.doubleInflowsOnPipes[pipeIndexI][1];
            }
            //mozna dodac case gdy za malo wody odplywa z wezla i nie wypelni?
        }
        else
        {
            Debug.Log($"porownaniedwochrur else puste");
        }
        return d.doubleInflowsOnPipes;
    }

    public static decimal CalculateOutflowOnPipe(DataVersion data, int pipeIndex)//, int nodeIndex)
    {
        if (data.pipesInflows[pipeIndex] > data.pipesConsumptions[pipeIndex])
            return data.pipesOutflows[pipeIndex] = data.pipesInflows[pipeIndex] - data.pipesConsumptions[pipeIndex];
        else
            return data.pipesOutflows[pipeIndex] = 0;
    }

    static int ReturnAdjacentNode(DataVersion dataVersion, int pipeIndex, int otherNodeIndex)
    {
        List<int> adjacentNodes = dataVersion._pipesAdjacentNodes[pipeIndex];
        int nodeIndex = -1;

        for (int j = 0; j < adjacentNodes.Count; j++)
        {
            if (otherNodeIndex != adjacentNodes[j])
                nodeIndex = adjacentNodes[j];
        }

        if (nodeIndex == -1)
            Debug.LogWarning("_pipesAdjacentNodes is empty");

        return nodeIndex;
    }
    public static decimal AddupInflows(DataVersion data, int pipeIndex)
    {
        data.pipesInflows[pipeIndex] = data.doubleInflowsOnPipes[pipeIndex][0] + data.doubleInflowsOnPipes[pipeIndex][1];
        return data.pipesInflows[pipeIndex];
    }
    static bool FlowDirectionForUI(DataVersion data, int pipeIndex, bool flowDirection)
    {
        data.flowDirectionForUI[pipeIndex] = flowDirection;
        return data.flowDirectionForUI[pipeIndex];
    }
    static bool FlowDirection(DataVersion data, int pipeIndex, bool flowDirection)
    {
        data.flowDirection[pipeIndex] = flowDirection;
        return data.flowDirection[pipeIndex];
    }

    #endregion




}
