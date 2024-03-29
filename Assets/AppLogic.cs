using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.Events;


/// <summary>
/// moze courtines do ststemu stopniowego rozplywu wody
/// albo invoke reapeting
/// </summary>

public class AppLogic : MonoBehaviour
{

    public UnityEvent<int> nadpiszWartosciWplywow;
    public UnityEvent nadpiszWartosciRozbiorow;
    public UnityEvent<int> nadpiszWartosciOdplywow;
    public UnityEvent<int> updateOdplywyZWezlow;
    public UnityEvent resetSimulation;

    //Wezel Input
    //Maksymalny godzinne rozbiory w wezlach - Qhmax [dm^3/s]
    public float[] rozbioryNaWezlach { get; private set; } = { 0f, 17f, 12f, 23f, 26f, 29f, 30f, 0f };

    //Wspolrzedne wysokokosci polezenia wezlow [stepCount npm]
    public int[] polozenieWezlow { get; private set; } = {145, 147, 146, 151, 154, 159, 168, 192};


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
    public float[] pipesOutFlows { get; private set; } = new float[9];
    public float[] pipesInFlows { get; private set; } = new float[9];
    public float[][] doubleInFlowsOnPipes { get; private set; } = new float[9][];

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
        nadpiszWartosciRozbiorow.Invoke();
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
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            //if (stepCount < nodesOutFlow.Length)
            {
                Debug.Log("**********************************ITERATION " + stepCount + "***********************************");
                SearchForNextNodeIndexAndCalculateIt();

            }
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetGame();
        }
    }


    //pozwala wziac do operacji najwysza wartosc z doubleInFlowsOnPipes na odcinkach a nastepnie ja usuwa
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
            nadpiszWartosciOdplywow.Invoke(adjacentPipes[i]);
            CalculateOutFlowOnNode(ReturnAdjacentNode(adjacentPipes[i], nodeIndex));
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
            Debug.Log($"adjacent pipe index: {adjacentPipesIndexes[i]}, outflow: {pipesOutFlows[adjacentPipesIndexes[i]]})");
            if (pipesOutFlows[adjacentPipesIndexes[i]] > 0 && nodesOutFlow[nodeIndex] > 0)
            {
                Debug.Log($"calculated node index: {nodeIndex}, pipeindex: {adjacentPipesIndexes[i]}");
            }
            else if (pipesOutFlows[adjacentPipesIndexes[i]] > 0)
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
            Debug.Log($"pipe {fullPipeIndexes[i]} outflow {pipesOutFlows[fullPipeIndexes[i]]}");
            nodesInFlow[nodeIndex] += pipesOutFlows[fullPipeIndexes[i]];
        }

        Debug.Log($" node {nodeIndex} inflow {nodesInFlow[nodeIndex]} and rozbior {rozbioryNaWezlach[nodeIndex]}");

        if (nodesInFlow[nodeIndex] > rozbioryNaWezlach[nodeIndex])
        {
            nodesOutFlow[nodeIndex] = nodesInFlow[nodeIndex] - rozbioryNaWezlach[nodeIndex];
        }
        else
        {
            Debug.Log("za malo wody w wezle");
        }
        updateOdplywyZWezlow.Invoke(nodeIndex);

    }

    void CalculateInFlowOnPipesNextToNode(int nodeIndex)
    {
        //tworzy liste z indexami pobliskich rur
        List<int> adjacentPipes = _nodeAndAdjacentPipes[nodeIndex];
        List<int> uncalculatedPipeIndexes = new List<int>();

        for (int i = 0; i < adjacentPipes.Count; i++)
        {
            Debug.Log($"CalculateInFlowOnPipesNextNode. pipe index: {adjacentPipes[i]}, node: {ReturnAdjacentNode(adjacentPipes[i], nodeIndex)}");
            if (pipesOutFlows[adjacentPipes[i]] == 0 || nodesOutFlow[ReturnAdjacentNode(adjacentPipes[i], nodeIndex)] == 0)
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

        //dziala tylko dla 2 nieobliczony wezlow
        Debug.Log("kurwa");
        //float[] tempOdplywyArray = new float[uncalculatedPipeIndexes.Count];

        for (int i = 0; i < uncalculatedPipeIndexes.Count; i++)
        {
            Debug.Log("kurwe");
            //outflow on adjacentNodes[1] in adj...[i] pipe
            float IOdplyw = nodesOutFlow[adjacentNodesToAdjacentPipes[i].adjacentNodes[1]];
            if(uncalculatedPipeIndexes.Count > 1)
            {
                for (int j = i + 1; j < uncalculatedPipeIndexes.Count; j++)
                {
                    Debug.Log($"more than 1 unculculatedpipe, count: {uncalculatedPipeIndexes.Count}, pipe index: {uncalculatedPipeIndexes[i]}, node index: {nodeIndex} case A");
                    float JOdplyw = nodesOutFlow[adjacentNodesToAdjacentPipes[j].adjacentNodes[1]];
                    int debugIIndexValue = adjacentNodesToAdjacentPipes[i].adjacentNodes[1];
                    int debugJIndexValue = adjacentNodesToAdjacentPipes[j].adjacentNodes[1];

                    if (IOdplyw == 0 && JOdplyw == 0)
                    {
                        Debug.Log(i + " + " + j);
                        Debug.Log($"case Aa - empty adjacent nodes: {debugIIndexValue} + {debugJIndexValue}");
                        doubleInFlowsOnPipes[uncalculatedPipeIndexes[i]][0] = nodesOutFlow[nodeIndex] / uncalculatedPipeIndexes.Count;
                        doubleInFlowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutFlow[nodeIndex] / uncalculatedPipeIndexes.Count;
                        Debug.Log(doubleInFlowsOnPipes[uncalculatedPipeIndexes[i]][0]);
                        Debug.Log(doubleInFlowsOnPipes[uncalculatedPipeIndexes[j]][0]);
                        Debug.Log($"nodes {nodeIndex} outflow" + nodesOutFlow[nodeIndex]);
                    }
                    else if (IOdplyw > 0 && JOdplyw == 0)
                    {
                        Debug.Log($"case Ab - full nodeindex; {debugIIndexValue} and empty nodeindex: {debugJIndexValue}");
                        bool znak = true;
                        PorownanieDwochRur(znak, i, j, uncalculatedPipeIndexes, nodeIndex);
                    }
                    else if (IOdplyw == 0 && JOdplyw > 0)
                    {
                        Debug.Log($"case Ac - empty nodeindex: {debugIIndexValue} and full nodeindex; {debugJIndexValue}");
                        bool znak = false;
                        PorownanieDwochRur(znak, i, j, uncalculatedPipeIndexes, nodeIndex);
                    }
                    else if (IOdplyw > 0 && JOdplyw > 0) ///work this shit out
                    {
                        Debug.Log($"case Ad - full nodeindex: {debugIIndexValue}, full nodeindex; {debugJIndexValue}");
                        Debug.Log($"case ad - pipe index i: {uncalculatedPipeIndexes[i]},pipe index j: {uncalculatedPipeIndexes[j]}");
                        Debug.Log($"case ad - inflow on pipeindex i: {pipesInFlows[uncalculatedPipeIndexes[i]]}, inflow on pipeindex j: {pipesInFlows[uncalculatedPipeIndexes[j]]}");
                        Debug.Log($"case ad - rozbior on pipeindex i: {pipesRozbiory[uncalculatedPipeIndexes[i]]}, rozbior on pipeindex j: {pipesRozbiory[uncalculatedPipeIndexes[j]]}");

                        if (pipesInFlows[uncalculatedPipeIndexes[i]] == pipesRozbiory[uncalculatedPipeIndexes[i]] && pipesInFlows[uncalculatedPipeIndexes[j]] == pipesRozbiory[uncalculatedPipeIndexes[j]])
                        {
                            Debug.Log($"case Ada");
                        }
                        else if (pipesInFlows[uncalculatedPipeIndexes[i]] < pipesRozbiory[uncalculatedPipeIndexes[i]] && pipesInFlows[uncalculatedPipeIndexes[j]] == pipesRozbiory[uncalculatedPipeIndexes[j]])
                        {
                            Debug.Log($"case Adb");
                            doubleInFlowsOnPipes[uncalculatedPipeIndexes[i]][1] = nodesOutFlow[nodeIndex];

                        }
                        else if (pipesInFlows[uncalculatedPipeIndexes[i]] == pipesRozbiory[uncalculatedPipeIndexes[i]] && pipesInFlows[uncalculatedPipeIndexes[j]] < pipesRozbiory[uncalculatedPipeIndexes[j]])
                        {
                            Debug.Log($"case Adc");
                            doubleInFlowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutFlow[nodeIndex];
                            AddUpInFlows(uncalculatedPipeIndexes[j]);

                            if (pipesInFlows[uncalculatedPipeIndexes[j]] < pipesRozbiory[uncalculatedPipeIndexes[j]])
                            {
                                doubleInFlowsOnPipes[uncalculatedPipeIndexes[j]][1] = pipesRozbiory[uncalculatedPipeIndexes[j]] - doubleInFlowsOnPipes[uncalculatedPipeIndexes[j]][1];
                            }
                        }
                        else if (pipesInFlows[uncalculatedPipeIndexes[i]] < pipesRozbiory[uncalculatedPipeIndexes[i]] && pipesInFlows[uncalculatedPipeIndexes[j]] < pipesRozbiory[uncalculatedPipeIndexes[j]])
                        {
                            Debug.Log($"case Add");
                            doubleInFlowsOnPipes[uncalculatedPipeIndexes[i]][1] = nodesOutFlow[nodeIndex];
                            doubleInFlowsOnPipes[uncalculatedPipeIndexes[j]][1] = nodesOutFlow[nodeIndex];
                        }
                        else
                        {
                            Debug.Log($"case Ade");
                        }
                    }
                    else
                    {
                        Debug.Log($"wariant dupa, nieznany case");
                    }
                    StuffToDoAfterCalculatingPipe(j, nodeIndex, uncalculatedPipeIndexes);
                    StuffToDoAfterCalculatingPipe(i, nodeIndex, uncalculatedPipeIndexes);
                }
            }
            else if (uncalculatedPipeIndexes.Count == 1)
            {
                Debug.Log($"less than 2 unculculated pipes, case B pipeindex: {uncalculatedPipeIndexes[i]} nodeindex: {nodeIndex}");
                doubleInFlowsOnPipes[uncalculatedPipeIndexes[i]][0] = nodesOutFlow[nodeIndex];
                StuffToDoAfterCalculatingPipe(i, nodeIndex, uncalculatedPipeIndexes);
            }
            else
            {
                Debug.Log("case C nwm ocb");
            }
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
        nadpiszWartosciWplywow.Invoke(uncalculatedPipeIndexes[pipeIndex]);
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

        if (pipesInFlows[uncalculatedPipeIndexes[i]] > pipesRozbiory[uncalculatedPipeIndexes[i]])
        {
            Debug.Log($"porownanie A");
            CalculateOutflowOnPipe(uncalculatedPipeIndexes[i]);
            nodesInFlow[nodeIndex] += pipesOutFlows[uncalculatedPipeIndexes[i]];
            nodesOutFlow[nodeIndex] += pipesOutFlows[uncalculatedPipeIndexes[i]];
            doubleInFlowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutFlow[nodeIndex];
        }
        else if (pipesInFlows[uncalculatedPipeIndexes[i]] == pipesRozbiory[uncalculatedPipeIndexes[i]])
        {
            Debug.Log($"porownanie B");
            CalculateOutflowOnPipe(uncalculatedPipeIndexes[i]);
            doubleInFlowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutFlow[nodeIndex];
        }
        else if (pipesInFlows[uncalculatedPipeIndexes[i]] < pipesRozbiory[uncalculatedPipeIndexes[i]])
        {
            
            Debug.Log($"porownanie C, pipeindex: {uncalculatedPipeIndexes[i]}, doublewplywy i 1: {doubleInFlowsOnPipes[uncalculatedPipeIndexes[i]][1]} ");
            Debug.Log($"porownanie C, pipeindex: {uncalculatedPipeIndexes[j]}, doublewplywy j 0: {doubleInFlowsOnPipes[uncalculatedPipeIndexes[j]][0]} ");
            CalculateOutflowOnPipe(uncalculatedPipeIndexes[i]);
            if (pipesOutFlows[uncalculatedPipeIndexes[i]] == 0)
            {
                doubleInFlowsOnPipes[uncalculatedPipeIndexes[i]][1] = (pipesRozbiory[uncalculatedPipeIndexes[i]] - pipesInFlows[uncalculatedPipeIndexes[i]]);
                doubleInFlowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutFlow[nodeIndex] - doubleInFlowsOnPipes[uncalculatedPipeIndexes[i]][1];
            }
            else
            {
                doubleInFlowsOnPipes[uncalculatedPipeIndexes[i]][1] = -1 * (pipesOutFlows[uncalculatedPipeIndexes[i]] - pipesInFlows[uncalculatedPipeIndexes[i]]);
                doubleInFlowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutFlow[nodeIndex] - doubleInFlowsOnPipes[uncalculatedPipeIndexes[i]][1];
            }
           
            //mozna dodac case gdy za malo wody odplywa z wezla i nie wypelni?

            Debug.Log($" nodeindex: {nodeIndex}, pipesoutflows: {pipesOutFlows[uncalculatedPipeIndexes[i]]}");
            Debug.Log($"porownanie C, pipeindex: {uncalculatedPipeIndexes[i]}, doublewplywy i 1: {doubleInFlowsOnPipes[uncalculatedPipeIndexes[i]][1]} ");
            Debug.Log($"porownanie C, pipeindex: {uncalculatedPipeIndexes[j]}, doublewplywy j 0: {doubleInFlowsOnPipes[uncalculatedPipeIndexes[j]][0]} ");
        }
        else
        {
            Debug.Log($"porownanie dupa");
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

        foreach (float i in rozbioryNaWezlach)
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
        pipesInFlows[p] = nodesOutFlow[p];
        KierunekPrzeplywu(p, true);
        nadpiszWartosciWplywow.Invoke(p);

        nodesOutFlow[zNode] = zasilanieZeZbiornika;
        pipesInFlows[zPipe] = nodesOutFlow[zNode];
        KierunekPrzeplywu(zPipe, false);
        nadpiszWartosciWplywow.Invoke(zPipe);
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
        pipesOutFlows[pipeIndex] = pipesInFlows[pipeIndex] - pipesRozbiory[pipeIndex];
        if (pipesOutFlows[pipeIndex] < 0)
        {
            pipesOutFlows[pipeIndex] = 0;
        }
        //Debug.Log(pipesOutFlows[pipeIndex]);
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
        pipesInFlows[pipeIndex] = doubleInFlowsOnPipes[pipeIndex][0] + doubleInFlowsOnPipes[pipeIndex][1];
        Debug.Log("wartosc z sumowania wplywow " + pipesInFlows[pipeIndex]);
        Debug.Log("wartosc z sumowania wplywow 1 " + doubleInFlowsOnPipes[pipeIndex][0]);
        Debug.Log("wartosc z sumowania wplywow 2 " + doubleInFlowsOnPipes[pipeIndex][1]);

        //nadpiszWartosciWplywow.Invoke(pipeIndex);
        //nadpiszWartosciOdplywow.Invoke(pipeIndex);
    }
    void DeclareWplywyArray()
    {
        for (int i = 0; i < 9; i++)
        {
            if (doubleInFlowsOnPipes[i] == null)
            {
                doubleInFlowsOnPipes[i] = new float[2];
            }
        }
    }

    void ResetGame()
    {
        Debug.Log("////////////////////////////////////ResetSimulation///////////////////////////////////////////////");
        stepCount = 0;
        for (int i = 0; i < pipesOutFlows.Length; i++)
        {
            pipesInFlows[i] = 0;
            pipesOutFlows[i] = 0;
            nadpiszWartosciOdplywow.Invoke(i);
            nadpiszWartosciWplywow.Invoke(i);

            for (int j = 0; j < doubleInFlowsOnPipes[i].Length; j++)
            {
                doubleInFlowsOnPipes[i][j] = 0;
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