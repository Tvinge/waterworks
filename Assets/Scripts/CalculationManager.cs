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

    DataVersion dataVersion = new DataVersion();

    private void Awake()
    {
        appLogic = FindObjectOfType<AppLogic>();
        dataLoader = FindObjectOfType<DataLoader>();
        appLogic.updateDataVersion += OnDataUpdated;


        appLogic.calculateWaterDistribution += CalculateWholeIteration;
        appLogic.resetSimulation += ResetValues;

        //StartingDiameterMethod();
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

    void OnDataUpdated(DataVersion d)
    {
        dataVersion = d;
    }


    #region FlowCalculations
    public void CalculateWholeIteration()
    {
        Debug.Log("invoked event");
        for(int i = 0; i < 8; i++)
        {
            if (dataVersion != null)
            {
                SearchForNextNodeIndexAndCalculateIt();
            }
        }
    }
    void SearchForNextNodeIndexAndCalculateIt()
    {
        if (isCalculated[stepCount] == true && isCalculated[stepCountBackward] == true)
            return;

        if (dataVersion.nodesOutflows[stepCount] > 0)
        {
            //KierunekPrzeplywu(stepCount, true);
            CalculateWaterFlow(stepCount);
            Debug.Log($"stepCount {stepCount}");
            isCalculated[stepCount] = true;
            stepCount += 1;
        }
        else
        {
            //KierunekPrzeplywu(stepCountBackward, false);
            CalculateWaterFlow(stepCountBackward);
            Debug.Log($"stepCountBackward {stepCountBackward}");
            isCalculated[stepCountBackward] = true;
            calculations = true;
            stepCountBackward -= 1;
        }
    }

    void CalculateWaterFlow(int nodeIndex)
    {
        List<int> adjacentPipes = dataVersion._nodeAndAdjacentPipes[nodeIndex];
        CalculateInflowOnPipesNextToNode(nodeIndex);
        for (int i = 0; i < adjacentPipes.Count; i++)
        {
            CalculateOutflowOnPipe(dataVersion, adjacentPipes[i]);
            CalculateOutflowOnNode(dataVersion, ReturnAdjacentNode(dataVersion, adjacentPipes[i], nodeIndex));
        }
    }

    void CalculateInflowOnPipesNextToNode(int nodeIndex)
    {
        List<int> uncalculatedPipeIndexes = CheckIfPipesAreCalculated(dataVersion, nodeIndex);
        List<(int pipeIndex, int[] adjacentNodes)> adjacentNodesToAdjacentPipes = new List<(int, int[])>();

        for (int i = 0; i < uncalculatedPipeIndexes.Count; i++)
        {
            List<int> adjacentNodesToPipe = dataVersion._pipesAdjacentNodes[uncalculatedPipeIndexes[i]];
            int[] adjacentNodes = new int[2];
            adjacentNodes[0] = nodeIndex;
            adjacentNodes[1] = ReturnAdjacentNode(dataVersion, uncalculatedPipeIndexes[i], nodeIndex);
            adjacentNodesToAdjacentPipes.Add((uncalculatedPipeIndexes[i], adjacentNodes));
        }
        SetOutFlowOnNodeBasedOnOutflowsOnAdjacantNodes(nodeIndex, uncalculatedPipeIndexes, adjacentNodesToAdjacentPipes);
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
    void SetOutFlowOnNodeBasedOnOutflowsOnAdjacantNodes(int nodeIndex, List<int> uncalculatedPipeIndexes, List<(int pipeIndex, int[] adjacentNodes)> adjacentNodesToAdjacentPipes)
    {
        for (int i = 0; i < uncalculatedPipeIndexes.Count; i++)
        {
            //outflow on adjacentNodes[1] in adj...[i] pipe
            decimal IOdplyw = dataVersion.nodesOutflows[adjacentNodesToAdjacentPipes[i].adjacentNodes[1]];
            if (uncalculatedPipeIndexes.Count > 1)
            {
                SetOutflowOnNodeWithTwoUncalculatedPipes(dataVersion, i, nodeIndex, IOdplyw,uncalculatedPipeIndexes, adjacentNodesToAdjacentPipes);
            }
            else if (uncalculatedPipeIndexes.Count == 1)
            {
                SetOutflowOnNodeWithOneUncalculatedPipe(dataVersion, i, nodeIndex, uncalculatedPipeIndexes);
            }
        }
    }

    public static decimal[][] SetOutflowOnNodeWithTwoUncalculatedPipes(DataVersion dataVersion, int i, int nodeIndex, decimal IOdplyw, List<int> uncalculatedPipeIndexes, List<(int pipeIndex, int[] adjacentNodes)> adjacentNodesToAdjacentPipes) 
    {
        for (int j = i + 1; j < uncalculatedPipeIndexes.Count; j++)
        {
            decimal JOdplyw = dataVersion.nodesOutflows[adjacentNodesToAdjacentPipes[j].adjacentNodes[1]];

            int debugIIndexValue = adjacentNodesToAdjacentPipes[i].adjacentNodes[1];
            int debugJIndexValue = adjacentNodesToAdjacentPipes[j].adjacentNodes[1];

            if (IOdplyw == 0 && JOdplyw == 0)
            {
                CalculateNodeWhileAdjacentNodesHaveNoOutflow(dataVersion, nodeIndex, i, j, uncalculatedPipeIndexes, adjacentNodesToAdjacentPipes);
            }
            else if (IOdplyw > 0 && JOdplyw == 0)
            {
                bool znak = true;
                PorownanieDwochRur(dataVersion, znak, i, j, nodeIndex, uncalculatedPipeIndexes);
            }
            else if (IOdplyw == 0 && JOdplyw > 0)
            {
                bool znak = false;
                PorownanieDwochRur(dataVersion, znak, i, j, nodeIndex, uncalculatedPipeIndexes);
            }
            else if (IOdplyw > 0 && JOdplyw > 0) ///work this  out
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
        if (dataVersion.nodesInflows[nodeIndex] == dataVersion.nodesRozbiory[nodeIndex])
            return dataVersion.doubleInflowsOnPipes;

        Debug.Log($"less than 2 unculculated pipes, case B pipeindex: {uncalculatedPipeIndexes[i]} nodeindex: {nodeIndex}");

        if (dataVersion.nodesOutflows[nodeIndex] > dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]])
        {
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = dataVersion.nodesOutflows[nodeIndex];
        }
        else
        {
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = dataVersion.nodesOutflows[nodeIndex];
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1] = dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]] - dataVersion.nodesOutflows[nodeIndex];
            dataVersion.nodesInflows[ReturnAdjacentNode(dataVersion, uncalculatedPipeIndexes[i], nodeIndex)] -= dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1];
        }
        StuffToDoAfterCalculatingPipe(dataVersion, i, nodeIndex, uncalculatedPipeIndexes);
        return dataVersion.doubleInflowsOnPipes;
    }

    static decimal[][] CalculateNodeWhileAdjacentNodesHaveNoOutflow(DataVersion dataVersion, int nodeIndex, int i, int j, List<int> uncalculatedPipeIndexes, List<(int pipeIndex, int[] adjacentNodes)> adjacentNodesToAdjacentPipes)
    {
        decimal IDoplyw = dataVersion.nodesInflows[adjacentNodesToAdjacentPipes[i].adjacentNodes[1]];
        decimal IRozbiory = dataVersion.nodesRozbiory[adjacentNodesToAdjacentPipes[i].adjacentNodes[1]];
        decimal JDoplyw = dataVersion.nodesInflows[adjacentNodesToAdjacentPipes[j].adjacentNodes[1]];
        decimal JRozbiory = dataVersion.nodesRozbiory[adjacentNodesToAdjacentPipes[j].adjacentNodes[1]];

        if (IDoplyw > 0 && JDoplyw > 0)
        {
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = IRozbiory - IDoplyw + dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]];
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = JRozbiory - JDoplyw + dataVersion.pipesRozbiory[uncalculatedPipeIndexes[j]];
        }
        else if (IDoplyw > 0 && JDoplyw <= 0)
        {
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = IRozbiory - IDoplyw + dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]];
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = dataVersion.nodesOutflows[nodeIndex] - (IRozbiory - IDoplyw + dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]]);
        }
        else if (IDoplyw <= 0 && JDoplyw > 0)
        {
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = dataVersion.nodesOutflows[nodeIndex] - (JRozbiory - JDoplyw + dataVersion.pipesRozbiory[uncalculatedPipeIndexes[j]]);
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = JRozbiory - JDoplyw + dataVersion.pipesRozbiory[uncalculatedPipeIndexes[j]];
        }
        else if (IDoplyw <= 0 && JDoplyw <= 0)
        {
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = dataVersion.nodesOutflows[nodeIndex] / uncalculatedPipeIndexes.Count;
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = dataVersion.nodesOutflows[nodeIndex] / uncalculatedPipeIndexes.Count;
        }
        return dataVersion.doubleInflowsOnPipes;
    }

    static decimal[][] CalculateOutflowOnNodeWhileAdjacentNodesAreFullOfWater(DataVersion dataVersion, int nodeIndex, List<int> uncalculatedPipeIndexes, int i, int j)
    {
        if (dataVersion.pipesInflows[uncalculatedPipeIndexes[i]] == dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]] && dataVersion.pipesInflows[uncalculatedPipeIndexes[j]] == dataVersion.pipesRozbiory[uncalculatedPipeIndexes[j]])
        {
            Debug.Log($"case Ada");
        }
        else if (dataVersion.pipesInflows[uncalculatedPipeIndexes[i]] < dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]] && dataVersion.pipesInflows[uncalculatedPipeIndexes[j]] == dataVersion.pipesRozbiory[uncalculatedPipeIndexes[j]])
        {
            Debug.Log($"case Adb");
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1] = dataVersion.nodesOutflows[nodeIndex];
        }
        else if (dataVersion.pipesInflows[uncalculatedPipeIndexes[i]] == dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]] && dataVersion.pipesInflows[uncalculatedPipeIndexes[j]] < dataVersion.pipesRozbiory[uncalculatedPipeIndexes[j]])
        {
            Debug.Log($"case Adc");
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = dataVersion.nodesOutflows[nodeIndex];
            AddupInflows(dataVersion, uncalculatedPipeIndexes[j]);

            if (dataVersion.pipesInflows[uncalculatedPipeIndexes[j]] < dataVersion.pipesRozbiory[uncalculatedPipeIndexes[j]])
            {
                dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][1] = dataVersion.pipesRozbiory[uncalculatedPipeIndexes[j]] - dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][1];
            }
        }
        else if (dataVersion.pipesInflows[uncalculatedPipeIndexes[i]] < dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]] && dataVersion.pipesInflows[uncalculatedPipeIndexes[j]] < dataVersion.pipesRozbiory[uncalculatedPipeIndexes[j]])
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

    //zrobic to z try-catch?
    static decimal[] CalculateOutflowOnNode(DataVersion d, int nodeIndex)
    {
        //tworzy liste z indexami pobliskich rur
        List<int> adjacentPipesIndexes = d._nodeAndAdjacentPipes[nodeIndex];
        List<int> emptyPipeIndexes = new List<int>();
        List<int> fullPipeIndexes = new List<int>();

        for (int i = 0; i < adjacentPipesIndexes.Count; i++)
        {
            Debug.Log($"adjacent pipe index: {adjacentPipesIndexes[i]}, outflow: {d.pipesOutflows[adjacentPipesIndexes[i]]})");
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
            Debug.Log($"pipe {fullPipeIndexes[i]} outflow {d.pipesOutflows[fullPipeIndexes[i]]}");
            d.nodesInflows[nodeIndex] += d.pipesOutflows[fullPipeIndexes[i]];
        }

        Debug.Log($" node {nodeIndex} inflow {d.nodesInflows[nodeIndex]} and rozbior {d.nodesRozbiory[nodeIndex]}");

        if (d.nodesInflows[nodeIndex] > d.nodesRozbiory[nodeIndex])
        {
            d.nodesOutflows[nodeIndex] = d.nodesInflows[nodeIndex] - d.nodesRozbiory[nodeIndex];
        }
        else
        {
            Debug.Log($"za malo wody w wezle {nodeIndex}");
        }
        return d.nodesOutflows;
    }

    static void StuffToDoAfterCalculatingPipe(DataVersion dataVersion, int pipeIndex, int nodeIndex, List<int> uncalculatedPipeIndexes)
    {
        bool znak;
        if (CalculationManager.calculations == false)
        {
            znak = true;
        }
        else
        {
            znak = false;
        }
        KierunekPrzeplywu(dataVersion, uncalculatedPipeIndexes[pipeIndex], znak);

        AddupInflows(dataVersion, uncalculatedPipeIndexes[pipeIndex]);
    }

    public static decimal[][] PorownanieDwochRur(DataVersion dataVersion, bool znak, int _i, int _j, int nodeIndex, List<int> uncalculatedPipeIndexes)
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

        if (dataVersion.pipesInflows[uncalculatedPipeIndexes[i]] > dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]])
        {
            dataVersion.nodesInflows[nodeIndex] += CalculateOutflowOnPipe(dataVersion, uncalculatedPipeIndexes[i]);
            dataVersion.nodesOutflows[nodeIndex] += CalculateOutflowOnPipe(dataVersion, uncalculatedPipeIndexes[i]);
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = dataVersion.nodesOutflows[nodeIndex];
        }
        else if (dataVersion.pipesInflows[uncalculatedPipeIndexes[i]] == dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]])
        {
            CalculateOutflowOnPipe(dataVersion, uncalculatedPipeIndexes[i]);
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = dataVersion.nodesOutflows[nodeIndex];
        }
        else if (dataVersion.pipesInflows[uncalculatedPipeIndexes[i]] < dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]])
        {
            if (CalculateOutflowOnPipe(dataVersion, uncalculatedPipeIndexes[i]) == 0)
            {
                dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1] = (dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]] - dataVersion.pipesInflows[uncalculatedPipeIndexes[i]]);
                dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = dataVersion.nodesOutflows[nodeIndex] - dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1];
            }
            else
            {
                dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = -1 * (dataVersion.pipesOutflows[uncalculatedPipeIndexes[i]] - dataVersion.pipesInflows[uncalculatedPipeIndexes[i]]);
                dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = dataVersion.nodesOutflows[nodeIndex] - dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1];
            }
            //mozna dodac case gdy za malo wody odplywa z wezla i nie wypelni?
        }
        else
        {
            Debug.Log($"porownaniedwochrur else puste");
        }
        return dataVersion.doubleInflowsOnPipes;
    }

    public static decimal CalculateOutflowOnPipe(DataVersion data, int pipeIndex)//, int nodeIndex)
    {
        if (data.pipesInflows[pipeIndex] > data.pipesRozbiory[pipeIndex])
            return data.pipesOutflows[pipeIndex] = data.pipesInflows[pipeIndex] - data.pipesRozbiory[pipeIndex];
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
        return nodeIndex;
    }
    public static decimal AddupInflows(DataVersion data, int pipeIndex)
    {
        data.pipesInflows[pipeIndex] = data.doubleInflowsOnPipes[pipeIndex][0] + data.doubleInflowsOnPipes[pipeIndex][1];
        return data.pipesInflows[pipeIndex];
    }
    static bool KierunekPrzeplywu(DataVersion dataVersion, int pipeIndex, bool kierunekPrzeplyw)
    {
        dataVersion.kierunekPrzeplywu[pipeIndex] = kierunekPrzeplyw;
        return dataVersion.kierunekPrzeplywu[pipeIndex];
    }

    #endregion



   
}
