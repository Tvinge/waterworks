using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.Events;
using System;

/// <summary>
/// moze courtines do ststemu stopniowego rozplywu wody
/// albo invoke reapeting
/// </summary>
/// 
public struct DataVersion
{
    public int index;
    public float[] nodesRozbiory { get; set; }
    public float[] nodesOutflow { get; set; }
    public float[] pipesRozbiory { get; set; }
    public bool[] kierunekPrzeplywu { get; set; }
    public float[] pipesOutflows { get; set; }
    public float[] pipesInflows { get; set; }
    public float[][] doubleInflowsOnPipes { get; set; }

}

public class AppLogic : MonoBehaviour
{
    public Action<DataVersion> updateData;
    public Action startIteration;

    DataVersion dataVersion = new DataVersion();

    List<DataVersion> dataVersions = new List<DataVersion>();


    //Wezel Input
    //Maksymalny godzinne rozbiory w wezlach - Qhmax [dm^3/s
    public float[] nodesRozbiory { get; private set; } = { 0f, 17f, 12f, 23f, 26f, 29f, 30f, 0f };

    //Wspolrzedne wysokokosci polezenia wezlow [stepCount npm]
    public float[] polozenieWezlow { get; private set; } = {145f, 147f, 146f, 151f, 154f, 159f, 168f, 192f};


    //Wezel Output
    float[] nodesInFlow = new float[8];

    public float[] nodesOutFlow { get; private set; } = new float[8];

    //Odcinek Input
    public float[] pipesRozbiory { get; private set; } = { 0f, 21f, 25f, 11f, 32f, 15f, 26f, 15f, 0f };
    float[] dlugoscOdcinka = { 150f, 400f, 350f, 320f, 290f, 300f, 315f, 290f, 250f };
    float[] wysokoscZabudowy = { 0f, 20f, 25f, 15f, 20f, 15f, 15f, 15f, 0f };

    //przechowuje pozycje rur
    Vector3[] pipesPositions = Enumerable.Repeat(Vector3.zero, 9).ToArray();

    Vector3[] nodesPositions = Enumerable.Repeat(Vector3.zero, 8).ToArray();

    float zasilanieZPompowni = 188;
    float zasilanieZeZbiornika;

    //Qhmax - maxymalne rozbiory na odcinkach i wezlach
    float maxQh;

    //Qhmax / Qhmin wynosi:
    float wspolczynnik = 1.75f;

    //Gdy przep³yw nastêpuje zgodnie ze wskazowkami zegara bool == 1/true
    //do nadpisania przez program, wartosc duynamiczna, ustalana przez przeplyw
    public bool[] kierunekPrzeplywu { get; private set; } = Enumerable.Repeat(false, 9).ToArray();
    
    //punkt odniesienia dla kierunku przeplywu, wartosc stala, niezmienialna
    //Gdy kierunek wodociagu odpowiada ruchowi wskazowek zegara - true
    bool[] kierunekRuchuWskazowekZegara = Enumerable.Repeat(true, 9).ToArray();

    //up right - true
    //bot left - false

    int k = 0;
    int l = 0;
    int stepCount = 0;
    int stepCountBackward = 7;

    //Odcinek Output
    public float[] pipesOutflows { get; private set; } = new float[9];
    public float[] pipesInflows { get; private set; } = new float[9];
    public float[][] doubleInflowsOnPipes { get; private set; } = new float[9][];

    public class IndexedValue
    {
        public int Index { get; set; }
        public float Value { get; set; }
    }

    private static List<IndexedValue> sortedIndexesAndValues = new List<IndexedValue>();
    private List<int> indexesToRemove = new List<int>();

    //hold indexes of adjacanet pipes with key - node 
    private Dictionary<int, List<int>> _nodeAndAdjacentPipes = new Dictionary<int, List<int>>();

    private Dictionary<int, List<int>> _pipesAdjacentNodes = new Dictionary<int, List<int>>();

    List<int> startingInFlowsNodeIndexes = new List<int>();
    List<int> skipedNodeIndexes = new List<int>();



    void Start()
    {
        DeclareWplywyArray();
        ObliczQzbiornika();
        ZasilanieZPompowniZbiornika();
        //nadpiszWartosciRozbiorow.Invoke(nodesRozbiory, pipesRozbiory);
        UstalWskazowkiZegara();
        DeclarePipePositions();
        DeclareNodesPositions();
        for (int i = 0; i < polozenieWezlow.Length; i++)
        {
            //Debug.Log("node index " + i + " adjacent pipes: ");
            SearchForAdjacentPipes(i);
        }
        for (int i = 0; i < dlugoscOdcinka.Length; i++)
        {
            //Debug.Log("pipe index " + i + " adjacent nodes: ");
            SearchForAdjacentNodes(i);
        }
        for (int i = 0; i < polozenieWezlow.Length; i++)
        {
            if (nodesOutFlow[i] > 0)
            {
                startingInFlowsNodeIndexes.Add(i);
            }
        }
        //UpdateDataVersion();
        startIteration.Invoke();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            //if (stepCount < nodesOutFlow.Length)
            {
                Debug.Log("**********************************ITERATION " + stepCount + "***********************************");
                SearchForNextNodeIndexAndCalculateIt();
                UpdateDataVersion();
            }
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetApp();
        }
    }

    void UpdateDataVersion()
    {
        var d = dataVersion;
        d.nodesRozbiory = nodesRozbiory;
        d.nodesOutflow = nodesOutFlow;
        d.pipesRozbiory = pipesRozbiory;
        d.kierunekPrzeplywu = kierunekPrzeplywu;
        d.pipesOutflows = pipesOutflows;
        d.pipesInflows = pipesInflows;
        d.doubleInflowsOnPipes = doubleInflowsOnPipes;

        dataVersions.Add(d);
        updateData?.Invoke(d);
    }

    //pozwala wziac do operacji najwysza wartosc z doubleInflowsOnPipes na odcinkach a nastepnie ja usuwa
    void SearchForNextNodeIndexAndCalculateIt()
    {
        if (nodesOutFlow[stepCount] > 0)
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
        CalculateInFlowOnPipesNextToNode(nodeIndex);
        for (int i = 0; i < adjacentPipes.Count; i++)
        {
            CalculateOutflowOnPipe(adjacentPipes[i]);
            CalculateOutFlowOnNode(ReturnAdjacentNode(adjacentPipes[i], nodeIndex));
        }
    }
    void CalculateInFlowOnPipesNextToNode(int nodeIndex)
    {
        //tworzy liste z indexami pobliskich rur
        List<int> adjacentPipes = _nodeAndAdjacentPipes[nodeIndex];
        List<int> uncalculatedPipeIndexes = new List<int>();

        for (int i = 0; i < adjacentPipes.Count; i++)
        {
            Debug.Log($"CalculateInFlowOnPipesNextNode. pipe index: {adjacentPipes[i]}, node: {ReturnAdjacentNode(adjacentPipes[i], nodeIndex)}");
            if (pipesOutflows[adjacentPipes[i]] == 0 || nodesOutFlow[ReturnAdjacentNode(adjacentPipes[i], nodeIndex)] == 0)
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
            float IOdplyw = nodesOutFlow[adjacentNodesToAdjacentPipes[i].adjacentNodes[1]];
            if (uncalculatedPipeIndexes.Count > 1)
            {
                for (int j = i + 1; j < uncalculatedPipeIndexes.Count; j++)
                {
                    float JOdplyw = nodesOutFlow[adjacentNodesToAdjacentPipes[j].adjacentNodes[1]];
                    int debugIIndexValue = adjacentNodesToAdjacentPipes[i].adjacentNodes[1];
                    int debugJIndexValue = adjacentNodesToAdjacentPipes[j].adjacentNodes[1];

                    if (IOdplyw == 0 && JOdplyw == 0)
                    {

                        doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = nodesOutFlow[nodeIndex] / uncalculatedPipeIndexes.Count;
                        doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutFlow[nodeIndex] / uncalculatedPipeIndexes.Count;

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
                Debug.Log($"less than 2 unculculated pipes, case B pipeindex: {uncalculatedPipeIndexes[i]} nodeindex: {nodeIndex}");
                doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][0] = nodesOutFlow[nodeIndex];
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
            doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1] = nodesOutFlow[nodeIndex];

        }
        else if (pipesInflows[uncalculatedPipeIndexes[i]] == pipesRozbiory[uncalculatedPipeIndexes[i]] && pipesInflows[uncalculatedPipeIndexes[j]] < pipesRozbiory[uncalculatedPipeIndexes[j]])
        {
            Debug.Log($"case Adc");
            doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutFlow[nodeIndex];
            AddUpInFlows(uncalculatedPipeIndexes[j]);

            if (pipesInflows[uncalculatedPipeIndexes[j]] < pipesRozbiory[uncalculatedPipeIndexes[j]])
            {
                doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][1] = pipesRozbiory[uncalculatedPipeIndexes[j]] - doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][1];
            }
        }
        else if (pipesInflows[uncalculatedPipeIndexes[i]] < pipesRozbiory[uncalculatedPipeIndexes[i]] && pipesInflows[uncalculatedPipeIndexes[j]] < pipesRozbiory[uncalculatedPipeIndexes[j]])
        {
            Debug.Log($"case Add");
            doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1] = nodesOutFlow[nodeIndex];
            doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][1] = nodesOutFlow[nodeIndex];
        }
        else
        {
            Debug.Log($"case Ade");
        }
    }

    //zrobic to z try-catch?
    void CalculateOutFlowOnNode(int nodeIndex)
    {
        //tworzy liste z indexami pobliskich rur
        List<int> adjacentPipesIndexes = _nodeAndAdjacentPipes[nodeIndex];
        List<int> emptyPipeIndexes = new List<int>();
        List<int> fullPipeIndexes = new List<int>();

        for (int i = 0; i < adjacentPipesIndexes.Count; i++)
        {
            Debug.Log($"adjacent pipe index: {adjacentPipesIndexes[i]}, outflow: {pipesOutflows[adjacentPipesIndexes[i]]})");
            if (pipesOutflows[adjacentPipesIndexes[i]] > 0 && nodesOutFlow[nodeIndex] > 0)
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
            nodesInFlow[nodeIndex] += pipesOutflows[fullPipeIndexes[i]];
        }

        Debug.Log($" node {nodeIndex} inflow {nodesInFlow[nodeIndex]} and rozbior {nodesRozbiory[nodeIndex]}");

        if (nodesInFlow[nodeIndex] > nodesRozbiory[nodeIndex])
        {
            nodesOutFlow[nodeIndex] = nodesInFlow[nodeIndex] - nodesRozbiory[nodeIndex];
        }
        else
        {
            Debug.Log($"za malo wody w wezle {nodeIndex}");
        }
    }

    
    void StuffToDoAfterCalculatingPipe(int pipeIndex, int nodeIndex, List<int> uncalculatedPipeIndexes)
    {
        /*
        if (nodesOutFlow[nodeIndex] > nodesOutFlow[ReturnAdjacentNode(uncalculatedPipeIndexes[pipeIndex], nodeIndex)])
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
            nodesInFlow[nodeIndex] += pipesOutflows[uncalculatedPipeIndexes[i]];
            nodesOutFlow[nodeIndex] += pipesOutflows[uncalculatedPipeIndexes[i]];
            doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutFlow[nodeIndex];
        }
        else if (pipesInflows[uncalculatedPipeIndexes[i]] == pipesRozbiory[uncalculatedPipeIndexes[i]])
        {
            CalculateOutflowOnPipe(uncalculatedPipeIndexes[i]);
            doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutFlow[nodeIndex];
        }
        else if (pipesInflows[uncalculatedPipeIndexes[i]] < pipesRozbiory[uncalculatedPipeIndexes[i]])
        {
            CalculateOutflowOnPipe(uncalculatedPipeIndexes[i]);
            if (pipesOutflows[uncalculatedPipeIndexes[i]] == 0)
            {
                doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1] = (pipesRozbiory[uncalculatedPipeIndexes[i]] - pipesInflows[uncalculatedPipeIndexes[i]]);
                doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutFlow[nodeIndex] - doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1];
            }
            else
            {
                doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1] = -1 * (pipesOutflows[uncalculatedPipeIndexes[i]] - pipesInflows[uncalculatedPipeIndexes[i]]);
                doubleInflowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutFlow[nodeIndex] - doubleInflowsOnPipes[uncalculatedPipeIndexes[i]][1];
            }
       
            //mozna dodac case gdy za malo wody odplywa z wezla i nie wypelni?
        }
        else
        {
            Debug.Log($"porownaniedwochrur else puste");
        }
    }

    #region
    void ObliczQzbiornika()
    {
        ObliczQhmax();
        zasilanieZeZbiornika = maxQh - zasilanieZPompowni;
    }

    void ObliczQhmax()
    {
        foreach (float i in pipesRozbiory)
        {
            maxQh += i;
        }

        foreach (float i in nodesRozbiory)
        {
            maxQh += i;
        }

    }

    void ZasilanieZPompowniZbiornika()
    {
        int p = 0;
        int zPipe = dlugoscOdcinka.Length - 1;
        int zNode = polozenieWezlow.Length - 1;

        nodesOutFlow[p] = zasilanieZPompowni;
        pipesInflows[p] = nodesOutFlow[p];
        KierunekPrzeplywu(p, true);

        nodesOutFlow[zNode] = zasilanieZeZbiornika;
        pipesInflows[zPipe] = nodesOutFlow[zNode];
        KierunekPrzeplywu(zPipe, false);
    }

    void UstalWskazowkiZegara()
    {
        for (int i = 0; i < kierunekRuchuWskazowekZegara.Length; i++)
        {
            if (i == 0)
            {
                kierunekRuchuWskazowekZegara[i] = true;
            }
            else if (i == kierunekRuchuWskazowekZegara.Length - 1)
            {
                kierunekRuchuWskazowekZegara[i] = false;
            }
            else
            {
                if (i == 1 || i == 2 || i == 5)
                    kierunekRuchuWskazowekZegara[i] = true;
                else
                    kierunekRuchuWskazowekZegara[i] = false;
            }
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

    void KierunekPrzeplywu(int pipeIndex, bool kierunekPrzeplyw)
    {
        kierunekPrzeplywu[pipeIndex] = kierunekPrzeplyw;
    }

    //ten override chyba useless
    void KierunekPrzeplywu(int pipeIndex, bool kierunekPrzeplyw, bool kierunekZegar)
    {
        kierunekPrzeplywu[pipeIndex] = kierunekPrzeplyw;
        kierunekRuchuWskazowekZegara[pipeIndex] = kierunekZegar;
    }

    void DeclarePipePositions()
    {
        for (int i = 0; i < dlugoscOdcinka.Length; i++)
        {
            pipesPositions[i] = GetComponent<Transform>().GetChild(0).GetChild(i).GetComponent<RectTransform>().anchoredPosition;
            //Debug.Log(pipesPositions[i]);
        }
    }

    void DeclareNodesPositions()
    {
        for (int i = 0; i < polozenieWezlow.Length; i++)
        {
            nodesPositions[i] = GetComponent<Transform>().GetChild(1).GetChild(i).GetComponent<RectTransform>().anchoredPosition;
            //Debug.Log(pipesPositions[i]);
        }
    }

    void SearchForAdjacentPipes(int nodeIndex)
    {
        Vector3 nodePosition = GetComponent<Transform>().GetChild(1).GetChild(nodeIndex).GetComponent<RectTransform>().anchoredPosition;
        List<int> foundPipes = new List<int>();
        for (int pipeIndex = 0; pipeIndex < dlugoscOdcinka.Length; pipeIndex++)
        {
            float distance = Vector3.Distance(nodePosition, pipesPositions[pipeIndex]);
            if (distance <= 60)
            {
                //Debug.Log("index " + pipeIndex);
                foundPipes.Add(pipeIndex);
            }
        }
        _nodeAndAdjacentPipes.Add(nodeIndex, foundPipes);
        //Debug.Log($"__ node index: {nodeIndex}");
    }

    void SearchForAdjacentNodes(int pipeIndex)
    {
        Vector3 pipePosition = GetComponent<Transform>().GetChild(0).GetChild(pipeIndex).GetComponent<RectTransform>().anchoredPosition;
        List<int> foundNodes = new List<int>();
        for (int nodeIndex = 0; nodeIndex < polozenieWezlow.Length; nodeIndex++)
        {
            float distance = Vector3.Distance(pipePosition, nodesPositions[nodeIndex]);
            //Debug.Log(distance);
            if (distance <= 60)
            {
                //Debug.Log("node index " + nodeIndex);
                foundNodes.Add(nodeIndex);
            }
        }
        _pipesAdjacentNodes.Add(pipeIndex, foundNodes);
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
    #endregion

    void AddUpInFlows(int pipeIndex)
    {
        pipesInflows[pipeIndex] = doubleInflowsOnPipes[pipeIndex][0] + doubleInflowsOnPipes[pipeIndex][1];
        Debug.Log("wartosc z sumowania wplywow " + pipesInflows[pipeIndex]);
        Debug.Log("wartosc z sumowania wplywow 1 " + doubleInflowsOnPipes[pipeIndex][0]);
        Debug.Log("wartosc z sumowania wplywow 2 " + doubleInflowsOnPipes[pipeIndex][1]);
    }
    void DeclareWplywyArray()
    {
        for (int i = 0; i < 9; i++)
        {
            if (doubleInflowsOnPipes[i] == null)
            {
                doubleInflowsOnPipes[i] = new float[2];
            }
        }
    }

    void ResetApp()
    {
        Debug.Log("////////////////////////////////////ResetSimulation///////////////////////////////////////////////");
        stepCount = 0;
        for (int i = 0; i < pipesOutflows.Length; i++)
        {
            pipesInflows[i] = 0;
            pipesOutflows[i] = 0;

            for (int j = 0; j < doubleInflowsOnPipes[i].Length; j++)
            {
                doubleInflowsOnPipes[i][j] = 0;
            }
        }
        for (int i = 0; i < nodesOutFlow.Length; i++)
        {
            nodesInFlow[i] = 0;
            nodesOutFlow[i] = 0;
        }
        _nodeAndAdjacentPipes.Clear();
        _pipesAdjacentNodes.Clear();

        Start();
    }
}