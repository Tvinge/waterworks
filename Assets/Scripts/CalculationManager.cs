using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CalculationManager : MonoBehaviour
{
    [SerializeField] AppLogic appLogic;

    float[] nodesRozbiory;
    float[] nodesOutflow;
    float[] nodesInflow;
    float[] pipesRozbiory;
    bool[] kierunekPrzeplywu;
    float[] pipesOutflows;
    float[] pipesInflows;
    float[][] doubleInflowsOnPipes;
    Dictionary<int, List<int>> _nodeAndAdjacentPipes = new Dictionary<int, List<int>>();
    Dictionary<int, List<int>> _pipesAdjacentNodes = new Dictionary<int, List<int>>();

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

        Debug.Log("invoked datraevent");
        nodesRozbiory = d.nodesRozbiory;//
        nodesOutflow = d.nodesOutflow;
        nodesInflow = d.nodesInflow;
        pipesRozbiory = d.pipesRozbiory;//
        kierunekPrzeplywu = d.kierunekPrzeplywu;
        pipesOutflows = d.pipesOutflows;
        pipesInflows = d.pipesInflows;
        doubleInflowsOnPipes = d.doubleInflowsOnPipes;
        
        if (_nodeAndAdjacentPipes != d._nodeAndAdjacentPipes)
        {
            _nodeAndAdjacentPipes = d._nodeAndAdjacentPipes;
        }
        if (_pipesAdjacentNodes != d._pipesAdjacentNodes)
        {
            _pipesAdjacentNodes = d._pipesAdjacentNodes;
        }

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
        if (stepCount == stepCountBackward)
            return;

        if (nodesOutflow[stepCount] > 0)
        {
            //KierunekPrzeplywu(stepCount, true);
            WaterFlow(stepCount);
            Debug.Log($"stepCount {stepCount}");
            stepCount += 1;
        }
        else
        {
            //KierunekPrzeplywu(stepCountBackward, false);
            WaterFlow(stepCountBackward);
            Debug.Log($"stepCountBackward {stepCountBackward}");
            stepCountBackward -= 1;
        }
    }
    void WaterFlow(int nodeIndex)
    {
        List<int> adjacentPipes = _nodeAndAdjacentPipes[nodeIndex];
        CalculateInflowOnPipesNextToNode(nodeIndex);
        for (int i = 0; i < adjacentPipes.Count; i++)
        {
            CalculateOutflowOnPipe(adjacentPipes[i]);
            CalculateOutflowOnNode(ReturnAdjacentNode(adjacentPipes[i], nodeIndex));
        }
    }
    void CalculateInflowOnPipesNextToNode(int nodeIndex)
    {
        //tworzy liste z indexami pobliskich rur
        List<int> adjacentPipes = _nodeAndAdjacentPipes[nodeIndex];
        List<int> uncalculatedPipeIndexes = new List<int>();

        for (int i = 0; i < adjacentPipes.Count; i++)
        {
            Debug.Log($"CalculateInFlowOnPipesNextNode. pipe index: {adjacentPipes[i]}, node: {ReturnAdjacentNode(adjacentPipes[i], nodeIndex)}");
            if (pipesOutflows[adjacentPipes[i]] == 0 || nodesOutflow[ReturnAdjacentNode(adjacentPipes[i], nodeIndex)] == 0)
            {//zastanowic sie nad waurnkiem //iteration 4
                Debug.Log("dodana do nieobliczonych");
                uncalculatedPipeIndexes.Add(adjacentPipes[i]);
            }
        }
        List<(int pipeIndex, int[] adjacentNodes)> adjacentNodesToAdjacentPipes = new List<(int, int[])>();

        for (int i = 0; i < uncalculatedPipeIndexes.Count; i++)
        {
            List<int> adjacentNodesToPipe = _pipesAdjacentNodes[uncalculatedPipeIndexes[i]];
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
            float IOdplyw = nodesOutflow[adjacentNodesToAdjacentPipes[i].adjacentNodes[1]];
            float IDoplyw = nodesInflow[adjacentNodesToAdjacentPipes[i].adjacentNodes[1]];
            float IRozbiory = nodesRozbiory[adjacentNodesToAdjacentPipes[i].adjacentNodes[1]];
            if (uncalculatedPipeIndexes.Count > 1)
            {
                for (int j = i + 1; j < uncalculatedPipeIndexes.Count; j++)
                {
                    float JOdplyw = nodesOutflow[adjacentNodesToAdjacentPipes[j].adjacentNodes[1]];
                    float JDoplyw = nodesInflow[adjacentNodesToAdjacentPipes[j].adjacentNodes[1]];
                    float JRozbiory = nodesRozbiory[adjacentNodesToAdjacentPipes[j].adjacentNodes[1]];

                    int debugIIndexValue = adjacentNodesToAdjacentPipes[i].adjacentNodes[1];
                    int debugJIndexValue = adjacentNodesToAdjacentPipes[j].adjacentNodes[1];

                    if (IOdplyw == 0 && JOdplyw == 0)
                    {
                        if(IDoplyw > 0 && JDoplyw > 0)
                        {
                            doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = IRozbiory - IDoplyw + pipesRozbiory[uncalculatedPipeIndexes[i]];
                            doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = JRozbiory - JDoplyw + pipesRozbiory[uncalculatedPipeIndexes[j]];
                        }
                        else if (IDoplyw > 0 && JDoplyw == 0)
                        {
                            doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = IRozbiory - IDoplyw + pipesRozbiory[uncalculatedPipeIndexes[i]];
                            doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutflow[nodeIndex] - (IRozbiory - IDoplyw + pipesRozbiory[uncalculatedPipeIndexes[i]]);
                        }
                        else if (IDoplyw == 0 && JDoplyw > 0)
                        {
                            doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = nodesOutflow[nodeIndex] - (JRozbiory - JDoplyw + pipesRozbiory[uncalculatedPipeIndexes[j]]);
                            doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = JRozbiory - JDoplyw + pipesRozbiory[uncalculatedPipeIndexes[j]];
                        }
                        else if (IDoplyw == 0 && JDoplyw == 0)
                        {
                            doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = nodesOutflow[nodeIndex] / uncalculatedPipeIndexes.Count;
                            doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutflow[nodeIndex] / uncalculatedPipeIndexes.Count;
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
                if (nodesInflow[nodeIndex] == nodesRozbiory[nodeIndex])
                    return;

                Debug.Log($"less than 2 unculculated pipes, case B pipeindex: {uncalculatedPipeIndexes[i]} nodeindex: {nodeIndex}");
                doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = nodesOutflow[nodeIndex];
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
        if (pipesInflows[uncalculatedPipeIndexes[i]] == pipesRozbiory[uncalculatedPipeIndexes[i]] && pipesInflows[uncalculatedPipeIndexes[j]] == pipesRozbiory[uncalculatedPipeIndexes[j]])
        {
            Debug.Log($"case Ada");
        }
        else if (pipesInflows[uncalculatedPipeIndexes[i]] < pipesRozbiory[uncalculatedPipeIndexes[i]] && pipesInflows[uncalculatedPipeIndexes[j]] == pipesRozbiory[uncalculatedPipeIndexes[j]])
        {
            Debug.Log($"case Adb");
            doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1] = nodesOutflow[nodeIndex];

        }
        else if (pipesInflows[uncalculatedPipeIndexes[i]] == pipesRozbiory[uncalculatedPipeIndexes[i]] && pipesInflows[uncalculatedPipeIndexes[j]] < pipesRozbiory[uncalculatedPipeIndexes[j]])
        {
            Debug.Log($"case Adc");
            doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutflow[nodeIndex];
            AddUpInFlows(uncalculatedPipeIndexes[j]);

            if (pipesInflows[uncalculatedPipeIndexes[j]] < pipesRozbiory[uncalculatedPipeIndexes[j]])
            {
                doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][1] = pipesRozbiory[uncalculatedPipeIndexes[j]] - doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][1];
            }
        }
        else if (pipesInflows[uncalculatedPipeIndexes[i]] < pipesRozbiory[uncalculatedPipeIndexes[i]] && pipesInflows[uncalculatedPipeIndexes[j]] < pipesRozbiory[uncalculatedPipeIndexes[j]])
        {
            Debug.Log($"case Add");
            doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1] = nodesOutflow[nodeIndex];
            doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][1] = nodesOutflow[nodeIndex];
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
        List<int> adjacentPipesIndexes = _nodeAndAdjacentPipes[nodeIndex];
        List<int> emptyPipeIndexes = new List<int>();
        List<int> fullPipeIndexes = new List<int>();

        for (int i = 0; i < adjacentPipesIndexes.Count; i++)
        {
            Debug.Log($"adjacent pipe index: {adjacentPipesIndexes[i]}, outflow: {pipesOutflows[adjacentPipesIndexes[i]]})");
            if (pipesOutflows[adjacentPipesIndexes[i]] > 0 && nodesOutflow[nodeIndex] > 0)
            {
                Debug.Log($"calculated node index: {nodeIndex}, pipeindex: {adjacentPipesIndexes[i]}");
            }
            else if (pipesOutflows[adjacentPipesIndexes[i]] > 0)
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
            Debug.Log($"pipe {fullPipeIndexes[i]} outflow {pipesOutflows[fullPipeIndexes[i]]}");
            nodesInflow[nodeIndex] += pipesOutflows[fullPipeIndexes[i]];
        }

        Debug.Log($" node {nodeIndex} inflow {nodesInflow[nodeIndex]} and rozbior {nodesRozbiory[nodeIndex]}");

        if (nodesInflow[nodeIndex] > nodesRozbiory[nodeIndex])
        {
            nodesOutflow[nodeIndex] = nodesInflow[nodeIndex] - nodesRozbiory[nodeIndex];
        }
        else
        {
            Debug.Log($"za malo wody w wezle {nodeIndex}");
        }
    }


    void StuffToDoAfterCalculatingPipe(int pipeIndex, int nodeIndex, List<int> uncalculatedPipeIndexes)
    {
        /*
        if (nodesOutflow[nodeIndex] > nodesOutflow[ReturnAdjacentNode(uncalculatedPipeIndexes[pipeIndex], nodeIndex)])
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
        AddUpInFlows(uncalculatedPipeIndexes[pipeIndex]);
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

        if (pipesInflows[uncalculatedPipeIndexes[i]] > pipesRozbiory[uncalculatedPipeIndexes[i]])
        {
            CalculateOutflowOnPipe(uncalculatedPipeIndexes[i]);
            nodesInflow[nodeIndex] += pipesOutflows[uncalculatedPipeIndexes[i]];
            nodesOutflow[nodeIndex] += pipesOutflows[uncalculatedPipeIndexes[i]];
            doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutflow[nodeIndex];
        }
        else if (pipesInflows[uncalculatedPipeIndexes[i]] == pipesRozbiory[uncalculatedPipeIndexes[i]])
        {
            CalculateOutflowOnPipe(uncalculatedPipeIndexes[i]);
            doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutflow[nodeIndex];
        }
        else if (pipesInflows[uncalculatedPipeIndexes[i]] < pipesRozbiory[uncalculatedPipeIndexes[i]])
        {
            CalculateOutflowOnPipe(uncalculatedPipeIndexes[i]);
            if (pipesOutflows[uncalculatedPipeIndexes[i]] == 0)
            {
                doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1] = (pipesRozbiory[uncalculatedPipeIndexes[i]] - pipesInflows[uncalculatedPipeIndexes[i]]);
                doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutflow[nodeIndex] - doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1];
            }
            else
            {
                doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1] = -1 * (pipesOutflows[uncalculatedPipeIndexes[i]] - pipesInflows[uncalculatedPipeIndexes[i]]);
                doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutflow[nodeIndex] - doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1];
            }

            //mozna dodac case gdy za malo wody odplywa z wezla i nie wypelni?
        }
        else
        {
            Debug.Log($"porownaniedwochrur else puste");
        }
    }
    void CalculateOutflowOnPipe(int pipeIndex)
    {
        pipesOutflows[pipeIndex] = pipesInflows[pipeIndex] - pipesRozbiory[pipeIndex];
        if (pipesOutflows[pipeIndex] < 0)
        {
            pipesOutflows[pipeIndex] = 0;
        }
        //Debug.Log(pipesOutflows[pipeIndex]);
    }

    //inputs pipeindex for search of adjacent nodes, and nodeindex to be avoided
    int ReturnAdjacentNode(int pipeIndex, int nodeIndex)
    {
        List<int> adjacentNodes = _pipesAdjacentNodes[pipeIndex];
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
    public void AddUpInFlows(int pipeIndex)
    {
        pipesInflows[pipeIndex] = doubleInflowsOnPipes[pipeIndex][0] + doubleInflowsOnPipes[pipeIndex][1];
        Debug.Log("wartosc z sumowania wplywow " + pipesInflows[pipeIndex]);
        Debug.Log("wartosc z sumowania wplywow 1 " + doubleInflowsOnPipes[pipeIndex][0]);
        Debug.Log("wartosc z sumowania wplywow 2 " + doubleInflowsOnPipes[pipeIndex][1]);
    }
    void KierunekPrzeplywu(int pipeIndex, bool kierunekPrzeplyw)
    {
        kierunekPrzeplywu[pipeIndex] = kierunekPrzeplyw;
    }
}
