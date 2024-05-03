using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CalculationManager : MonoBehaviour
{
    [SerializeField] AppLogic appLogic;

    /*
    float[] dataVersion.nodesRozbiory;
    float[] dataVersion.nodesOutflow;
    float[] dataVersion.nodesInflow;
    float[] dataVersion.pipesRozbiory;
    bool[] dataVersion.kierunekPrzeplywu;
    float[] dataVersion.pipesOutflows;
    float[] dataVersion.pipesInflows;
    float[][] dataVersion.doubleInflowsOnPipes;
    Dictionary<int, List<int>> dataVersion._nodeAndAdjacentPipes = new Dictionary<int, List<int>>();
    Dictionary<int, List<int>> _pipesAdjacentNodes = new Dictionary<int, List<int>>();
    */
    bool[] isCalculated = new bool[8];
    int stepCount = 0;
    int stepCountBackward = 7;

    DataVersion dataVersion = new DataVersion();

    private void Awake()
    {
        
        appLogic.updateDataVersion += OnDataUpdated;
        appLogic.startIteration += CalculateWholeIteration;
        appLogic.resetSimulation += ResetValues;
        //appLogic.initializeDictionaries += InitializePositions;
    }

    private void ResetValues()
    {
        stepCount = 0;
        stepCountBackward = 7;
    }

    
    void OnDataUpdated(DataVersion d)
    {

        dataVersion = d;
        /*
        Debug.Log("invoked datraevent");
        dataVersion.nodesRozbiory = d.dataVersion.nodesRozbiory;//
        dataVersion.nodesOutflow = d.dataVersion.nodesOutflow;
        dataVersion.nodesInflow = d.dataVersion.nodesInflow;
        dataVersion.pipesRozbiory = d.dataVersion.pipesRozbiory;//
        dataVersion.kierunekPrzeplywu = d.dataVersion.kierunekPrzeplywu;
        dataVersion.pipesOutflows = d.dataVersion.pipesOutflows;
        dataVersion.pipesInflows = d.dataVersion.pipesInflows;
        dataVersion.doubleInflowsOnPipes = d.dataVersion.doubleInflowsOnPipes;
        
        if (dataVersion._nodeAndAdjacentPipes != d.dataVersion._nodeAndAdjacentPipes)
        {
            dataVersion._nodeAndAdjacentPipes = d.dataVersion._nodeAndAdjacentPipes;
        }
        if (_pipesAdjacentNodes != d._pipesAdjacentNodes)
        {
            _pipesAdjacentNodes = d._pipesAdjacentNodes;
        }
        */
    }


    void CalculateWholeIteration()
    {
        Debug.Log("invoked event");
        //for(int i = 0; i < 6; i++)
        if (dataVersion != null)
        {
            SearchForNextNodeIndexAndCalculateIt();
        }

    }
    void SearchForNextNodeIndexAndCalculateIt()
    {
        if (isCalculated[stepCount] == true && isCalculated[stepCountBackward] == true && stepCount == stepCountBackward)
            return;

        if (dataVersion.nodesOutflow[stepCount] > 0)
        {
            //KierunekPrzeplywu(stepCount, true);
            WaterFlow(stepCount);
            Debug.Log($"stepCount {stepCount}");
            isCalculated[stepCount] = true;
            stepCount += 1;

        }
        else
        {
            //KierunekPrzeplywu(stepCountBackward, false);
            WaterFlow(stepCountBackward);
            Debug.Log($"stepCountBackward {stepCountBackward}");
            isCalculated[stepCountBackward] = true;
            stepCountBackward -= 1;

        }
    }

    void WaterFlow(int nodeIndex)
    {
        List<int> adjacentPipes = dataVersion._nodeAndAdjacentPipes[nodeIndex];
        CalculateInflowOnPipesNextToNode(nodeIndex);
        for (int i = 0; i < adjacentPipes.Count; i++)
        {
            CalculateOutflowOnPipe(dataVersion, adjacentPipes[i]);
            CalculateOutflowOnNode(ReturnAdjacentNode(adjacentPipes[i], nodeIndex));
        }
    }
    void CalculateInflowOnPipesNextToNode(int nodeIndex)
    {
        //tworzy liste z indexami pobliskich rur
        List<int> adjacentPipes = dataVersion._nodeAndAdjacentPipes[nodeIndex];
        List<int> uncalculatedPipeIndexes = new List<int>();

        for (int i = 0; i < adjacentPipes.Count; i++)
        {
            Debug.Log($"CalculateInFlowOnPipesNextNode. pipe index: {adjacentPipes[i]}, node: {ReturnAdjacentNode(adjacentPipes[i], nodeIndex)}");
            if (dataVersion.pipesOutflows[adjacentPipes[i]] == 0 || dataVersion.nodesOutflow[ReturnAdjacentNode(adjacentPipes[i], nodeIndex)] == 0)
            {//zastanowic sie nad waurnkiem //iteration 4
                Debug.Log("dodana do nieobliczonych");
                uncalculatedPipeIndexes.Add(adjacentPipes[i]);
            }
        }
        List<(int pipeIndex, int[] adjacentNodes)> adjacentNodesToAdjacentPipes = new List<(int, int[])>();

        for (int i = 0; i < uncalculatedPipeIndexes.Count; i++)
        {
            List<int> adjacentNodesToPipe = dataVersion._pipesAdjacentNodes[uncalculatedPipeIndexes[i]];
            int[] adjacentNodes = new int[2];
            adjacentNodes[0] = nodeIndex;
            adjacentNodes[1] = ReturnAdjacentNode(uncalculatedPipeIndexes[i], nodeIndex);
            adjacentNodesToAdjacentPipes.Add((uncalculatedPipeIndexes[i], adjacentNodes));
        }
        SetOutFlowOnNodeBasedOnOutFlowsOnAdjacantNodes(nodeIndex, uncalculatedPipeIndexes, adjacentNodesToAdjacentPipes);
    }

    void SetOutFlowOnNodeBasedOnOutFlowsOnAdjacantNodes(int nodeIndex, List<int> uncalculatedPipeIndexes, List<(int pipeIndex, int[] adjacentNodes)> adjacentNodesToAdjacentPipes)
    {
        for (int i = 0; i < uncalculatedPipeIndexes.Count; i++)
        {
            //outflow on adjacentNodes[1] in adj...[i] pipe
            float IOdplyw = dataVersion.nodesOutflow[adjacentNodesToAdjacentPipes[i].adjacentNodes[1]];
            float IDoplyw = dataVersion.nodesInflow[adjacentNodesToAdjacentPipes[i].adjacentNodes[1]];
            float IRozbiory = dataVersion.nodesRozbiory[adjacentNodesToAdjacentPipes[i].adjacentNodes[1]];
            if (uncalculatedPipeIndexes.Count > 1)
            {
                for (int j = i + 1; j < uncalculatedPipeIndexes.Count; j++)
                {
                    float JOdplyw = dataVersion.nodesOutflow[adjacentNodesToAdjacentPipes[j].adjacentNodes[1]];
                    float JDoplyw = dataVersion.nodesInflow[adjacentNodesToAdjacentPipes[j].adjacentNodes[1]];
                    float JRozbiory = dataVersion.nodesRozbiory[adjacentNodesToAdjacentPipes[j].adjacentNodes[1]];

                    int debugIIndexValue = adjacentNodesToAdjacentPipes[i].adjacentNodes[1];
                    int debugJIndexValue = adjacentNodesToAdjacentPipes[j].adjacentNodes[1];

                    if (IOdplyw == 0 && JOdplyw == 0)
                    {
                        if(IDoplyw > 0 && JDoplyw > 0)
                        {
                            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = IRozbiory - IDoplyw + dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]];
                            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = JRozbiory - JDoplyw + dataVersion.pipesRozbiory[uncalculatedPipeIndexes[j]];
                        }
                        else if (IDoplyw > 0 && JDoplyw == 0)
                        {
                            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = IRozbiory - IDoplyw + dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]];
                            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = dataVersion.nodesOutflow[nodeIndex] - (IRozbiory - IDoplyw + dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]]);
                        }
                        else if (IDoplyw == 0 && JDoplyw > 0)
                        {
                            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = dataVersion.nodesOutflow[nodeIndex] - (JRozbiory - JDoplyw + dataVersion.pipesRozbiory[uncalculatedPipeIndexes[j]]);
                            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = JRozbiory - JDoplyw + dataVersion.pipesRozbiory[uncalculatedPipeIndexes[j]];
                        }
                        else if (IDoplyw == 0 && JDoplyw == 0)
                        {
                            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = dataVersion.nodesOutflow[nodeIndex] / uncalculatedPipeIndexes.Count;
                            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = dataVersion.nodesOutflow[nodeIndex] / uncalculatedPipeIndexes.Count;
                        }
                    }
                    else if (IOdplyw > 0 && JOdplyw == 0)
                    {

                        bool znak = true;
                        PorownanieDwochRur(znak, i, j, uncalculatedPipeIndexes, nodeIndex);
                    }
                    else if (IOdplyw == 0 && JOdplyw > 0)
                    {
                        bool znak = false;
                        PorownanieDwochRur(znak, i, j, uncalculatedPipeIndexes, nodeIndex);
                    }
                    else if (IOdplyw > 0 && JOdplyw > 0) ///work this shit out
                    {
                        CalculateOutflowOnNodeWhileAdjacentNodesAreFullOfWater(nodeIndex, uncalculatedPipeIndexes, i, j);
                    }
                    else
                    {
                        Debug.Log($"wariant dpa, nieznany case");
                    }
                    StuffToDoAfterCalculatingPipe(j, nodeIndex, uncalculatedPipeIndexes);
                    StuffToDoAfterCalculatingPipe(i, nodeIndex, uncalculatedPipeIndexes);
                }
            }
            else if (uncalculatedPipeIndexes.Count == 1)
            {
                if (dataVersion.nodesInflow[nodeIndex] == dataVersion.nodesRozbiory[nodeIndex])
                    return;

                Debug.Log($"less than 2 unculculated pipes, case B pipeindex: {uncalculatedPipeIndexes[i]} nodeindex: {nodeIndex}");
                dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = dataVersion.nodesOutflow[nodeIndex];
                StuffToDoAfterCalculatingPipe(i, nodeIndex, uncalculatedPipeIndexes);
            }
            else
            {
                Debug.Log("case C nwm ocb");
            }
        }
    }
    void CalculateOutflowOnNodeWhileAdjacentNodesAreFullOfWater(int nodeIndex, List<int> uncalculatedPipeIndexes, int i, int j)
    {
        if (dataVersion.pipesInflows[uncalculatedPipeIndexes[i]] == dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]] && dataVersion.pipesInflows[uncalculatedPipeIndexes[j]] == dataVersion.pipesRozbiory[uncalculatedPipeIndexes[j]])
        {
            Debug.Log($"case Ada");
        }
        else if (dataVersion.pipesInflows[uncalculatedPipeIndexes[i]] < dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]] && dataVersion.pipesInflows[uncalculatedPipeIndexes[j]] == dataVersion.pipesRozbiory[uncalculatedPipeIndexes[j]])
        {
            Debug.Log($"case Adb");
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1] = dataVersion.nodesOutflow[nodeIndex];

        }
        else if (dataVersion.pipesInflows[uncalculatedPipeIndexes[i]] == dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]] && dataVersion.pipesInflows[uncalculatedPipeIndexes[j]] < dataVersion.pipesRozbiory[uncalculatedPipeIndexes[j]])
        {
            Debug.Log($"case Adc");
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = dataVersion.nodesOutflow[nodeIndex];
            AddupInflows(dataVersion, uncalculatedPipeIndexes[j]);

            if (dataVersion.pipesInflows[uncalculatedPipeIndexes[j]] < dataVersion.pipesRozbiory[uncalculatedPipeIndexes[j]])
            {
                dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][1] = dataVersion.pipesRozbiory[uncalculatedPipeIndexes[j]] - dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][1];
            }
        }
        else if (dataVersion.pipesInflows[uncalculatedPipeIndexes[i]] < dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]] && dataVersion.pipesInflows[uncalculatedPipeIndexes[j]] < dataVersion.pipesRozbiory[uncalculatedPipeIndexes[j]])
        {
            Debug.Log($"case Add");
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1] = dataVersion.nodesOutflow[nodeIndex];
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][1] = dataVersion.nodesOutflow[nodeIndex];
        }
        else
        {
            Debug.Log($"case Ade");
        }
    }

    //zrobic to z try-catch?
    void CalculateOutflowOnNode(int nodeIndex)
    {
        //tworzy liste z indexami pobliskich rur
        List<int> adjacentPipesIndexes = dataVersion._nodeAndAdjacentPipes[nodeIndex];
        List<int> emptyPipeIndexes = new List<int>();
        List<int> fullPipeIndexes = new List<int>();

        for (int i = 0; i < adjacentPipesIndexes.Count; i++)
        {
            Debug.Log($"adjacent pipe index: {adjacentPipesIndexes[i]}, outflow: {dataVersion.pipesOutflows[adjacentPipesIndexes[i]]})");
            if (dataVersion.pipesOutflows[adjacentPipesIndexes[i]] > 0 && dataVersion.nodesOutflow[nodeIndex] > 0)
            {
                Debug.Log($"calculated node index: {nodeIndex}, pipeindex: {adjacentPipesIndexes[i]}");
            }
            else if (dataVersion.pipesOutflows[adjacentPipesIndexes[i]] > 0)
            {
                fullPipeIndexes.Add(adjacentPipesIndexes[i]);
            }
            else
            {
                emptyPipeIndexes.Add(adjacentPipesIndexes[i]);
            }
        }

        for (int i = 0; i < fullPipeIndexes.Count; i++)
        {
            Debug.Log($"pipe {fullPipeIndexes[i]} outflow {dataVersion.pipesOutflows[fullPipeIndexes[i]]}");
            dataVersion.nodesInflow[nodeIndex] += dataVersion.pipesOutflows[fullPipeIndexes[i]];
        }

        Debug.Log($" node {nodeIndex} inflow {dataVersion.nodesInflow[nodeIndex]} and rozbior {dataVersion.nodesRozbiory[nodeIndex]}");

        if (dataVersion.nodesInflow[nodeIndex] > dataVersion.nodesRozbiory[nodeIndex])
        {
            dataVersion.nodesOutflow[nodeIndex] = dataVersion.nodesInflow[nodeIndex] - dataVersion.nodesRozbiory[nodeIndex];
        }
        else
        {
            Debug.Log($"za malo wody w wezle {nodeIndex}");
        }
    }


    void StuffToDoAfterCalculatingPipe(int pipeIndex, int nodeIndex, List<int> uncalculatedPipeIndexes)
    {
        /*
        if (dataVersion.nodesOutflow[nodeIndex] > dataVersion.nodesOutflow[ReturnAdjacentNode(uncalculatedPipeIndexes[pipeIndex], nodeIndex)])
        {
            KierunekPrzeplywu(uncalculatedPipeIndexes[pipeIndex], true);
            Debug.Log("true");
        }
        else
        {
            KierunekPrzeplywu(uncalculatedPipeIndexes[pipeIndex], false);
            Debug.Log("true12");
        }
        */
        KierunekPrzeplywu(uncalculatedPipeIndexes[pipeIndex], true);
        AddupInflows(dataVersion, uncalculatedPipeIndexes[pipeIndex]);
    }

    void PorownanieDwochRur(bool znak, int _i, int _j, List<int> uncalculatedPipeIndexes, int nodeIndex)
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
            dataVersion.nodesInflow[nodeIndex] += CalculateOutflowOnPipe(dataVersion, uncalculatedPipeIndexes[i]);
            dataVersion.nodesOutflow[nodeIndex] += CalculateOutflowOnPipe(dataVersion, uncalculatedPipeIndexes[i]);
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = dataVersion.nodesOutflow[nodeIndex];
        }
        else if (dataVersion.pipesInflows[uncalculatedPipeIndexes[i]] == dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]])
        {
            CalculateOutflowOnPipe(dataVersion, uncalculatedPipeIndexes[i]);
            dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = dataVersion.nodesOutflow[nodeIndex];
        }
        else if (dataVersion.pipesInflows[uncalculatedPipeIndexes[i]] < dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]])
        {
            if (CalculateOutflowOnPipe(dataVersion, uncalculatedPipeIndexes[i]) == 0)
            {
                dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1] = (dataVersion.pipesRozbiory[uncalculatedPipeIndexes[i]] - dataVersion.pipesInflows[uncalculatedPipeIndexes[i]]);
                dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = dataVersion.nodesOutflow[nodeIndex] - dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1];
            }
            else
            {
                dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1] = -1 * (dataVersion.pipesOutflows[uncalculatedPipeIndexes[i]] - dataVersion.pipesInflows[uncalculatedPipeIndexes[i]]);
                dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = dataVersion.nodesOutflow[nodeIndex] - dataVersion.doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1];
            }

            //mozna dodac case gdy za malo wody odplywa z wezla i nie wypelni?
        }
        else
        {
            Debug.Log($"porownaniedwochrur else puste");
        }
    }
    public static float CalculateOutflowOnPipe(DataVersion data, int pipeIndex)
    {
        data.pipesOutflows[pipeIndex] = data.pipesInflows[pipeIndex] - data.pipesRozbiory[pipeIndex];
        if (data.pipesOutflows[pipeIndex] < 0)
        {
            data.pipesOutflows[pipeIndex] = 0;
        }
        //Debug.Log(dataVersion.pipesOutflows[pipeIndex]);
        return data.pipesOutflows[pipeIndex];
    }

    //inputs pipeindex for search of adjacent nodes, and nodeindex to be avoided
    int ReturnAdjacentNode(int pipeIndex, int nodeIndex)
    {
        List<int> adjacentNodes = dataVersion._pipesAdjacentNodes[pipeIndex];
        //drugi wezel poza nodeIndex
        int secondNodeIndex = -1;

        for (int j = 0; j < adjacentNodes.Count; j++)
        {

            if (nodeIndex != adjacentNodes[j])
            {
                secondNodeIndex = adjacentNodes[j];
                return secondNodeIndex;
            }
        }
        Debug.Log($" funkcja ReturnAdjacentNode nie znalazla szukanego wezla. index = {secondNodeIndex}");
        return secondNodeIndex;
    }
    public static float[] AddupInflows(DataVersion data, int pipeIndex)
    {
        data.pipesInflows[pipeIndex] = data.doubleInflowsOnPipes[pipeIndex][0] + data.doubleInflowsOnPipes[pipeIndex][1];
        Debug.Log("wartosc z sumowania wplywow " + data.pipesInflows[pipeIndex]);
        Debug.Log("wartosc z sumowania wplywow 1 " + data.doubleInflowsOnPipes[pipeIndex][0]);
        Debug.Log("wartosc z sumowania wplywow 2 " + data.doubleInflowsOnPipes[pipeIndex][1]);
        return data.pipesInflows;
    }
    void KierunekPrzeplywu(int pipeIndex, bool kierunekPrzeplyw)
    {
        dataVersion.kierunekPrzeplywu[pipeIndex] = kierunekPrzeplyw;
    }
}
