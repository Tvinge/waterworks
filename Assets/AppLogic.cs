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

    //Wezel Input
    //Maksymalny godzinne rozbiory w wezlach - Qhmax [dm^3/s]
    public float[] rozbioryNaWezlach { get; private set; } = { 0f, 17f, 12f, 23f, 26f, 29f, 30f, 0f };

    //Wspolrzedne wysokokosci polezenia wezlow [m npm]
    public int[] polozenieWezlow { get; private set; } = {145, 147, 146, 151, 154, 159, 168, 192};


    //Wezel Output
    float[] nodesInFlow = new float[8];

    public float[] nodesOutFlow { get; private set; } = new float[8];

    //Odcinek Input
    public float[] rozbioryNaOdcinkach { get; private set; } = { 0f, 21f, 25f, 11f, 32f, 15f, 26f, 15f, 0f };
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
    int m = 0;

    //Odcinek Output
    public float[] pipesOutFlows { get; private set; } = new float[9];
    public float[] wplywyNaOdcinkach { get; private set; } = new float[9];
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

        List<float> startingInFlows = new List<float>();
        for (int i = 0; i < dlugoscOdcinka.Length; i++)
        {
            if (nodesInFlow[i] > 0)
            {
                startingInFlows.Add(nodesInFlow[i]);
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (m < 9)
            {
                Debug.Log(m + "ITERACJA***********************************");
                SearchForNextNodeIndexAndCalculateIt();
                m += 1;
                
            }

        }
    }

    //pozwala wziac do operacji najwysza wartosc z doubleInFlowsOnPipes na odcinkach a nastepnie ja usuwa
    void SearchForNextNodeIndexAndCalculateIt()
    {
        // ZnajdŸ indeksy dla ka¿dej wartoœci w tablicy
        var indexesAndValues = nodesOutFlow.Select((value, index) => new IndexedValue { Index =  index, Value = value });

        // Posortuj indeksy i wartoœci wed³ug wartoœci w malej¹cej kolejnoœci
        sortedIndexesAndValues = indexesAndValues.OrderByDescending(item => item.Value).ToList();

        foreach (var indexToRemove in indexesToRemove)
        {
            sortedIndexesAndValues.RemoveAll(item => item.Index == indexToRemove);
        }
        /*
        int zeroIndexesCount = 0;
        foreach (var element in sortedIndexesAndValues)
        {
            if (element.Value != 0)
            {
                Debug.Log($"Indeks: {element.Index}, Wartoœæ: {element.Value}");
            }
            else
            {
                zeroIndexesCount++;
            }
        }
        Debug.Log($"Empty indexes: {zeroIndexesCount} \n removed indexes in this iteration: {indexesToRemove.Count}");
        */
        if (sortedIndexesAndValues.Any())
        {
            var maxElement = sortedIndexesAndValues.First();
            int maxNodeIndex = maxElement.Index;
            float maxValue = maxElement.Value;

            indexesToRemove.Add(maxNodeIndex);

            Debug.Log($"Wplyw na odcinku: {maxValue}");
            //miejsce na operacje do wykonania
            WaterFlow(maxNodeIndex);
        }
    }
    void WaterFlow(int nodeIndex)
    {
        List<int> adjacentPipes = _nodeAndAdjacentPipes[nodeIndex];
        if (nodesOutFlow[nodeIndex] == 0)
        {
            CalculateOutFlowOnNode(nodeIndex);
        }
        CalculateInFlowOnPipesNextToNode(nodeIndex);
        for (int i = 0; i < adjacentPipes.Count; i++)
        {
            CalculateOutflowOnPipe(adjacentPipes[i]);
            nadpiszWartosciOdplywow.Invoke(adjacentPipes[i]);

            int[] adjacentNodes = new int[2];
            adjacentNodes[0] = nodeIndex;
            adjacentNodes[1] = ReturnAdjacentNode(adjacentPipes[i], nodeIndex);

            Debug.Log(nodesInFlow[adjacentNodes[1]]);
        }
    }

    //zrobic to z try-catch?
    void CalculateOutFlowOnNode(int nodeIndex)
    {
        //tworzy liste z indexami pobliskich rur
        List<int> adjacentPipes = _nodeAndAdjacentPipes[nodeIndex];
        List<int> emptyPipeIndexes = new List<int>();
        List<int> fullPipeIndexes = new List<int>();

        for (int i = 0; i < adjacentPipes.Count; i++)
        {
            if (pipesOutFlows[adjacentPipes[i]] > 0)
            {
                fullPipeIndexes.Add(adjacentPipes[i]);
                //Debug.Log("full pipe index" + adjacentPipes[i]);
            }
            else
            {
                emptyPipeIndexes.Add(adjacentPipes[i]);
                //Debug.Log("empty pipe index" + adjacentPipes[i]);
            }
        }

        for (int i = 0; i < fullPipeIndexes.Count; i++)
        {
            nodesInFlow[nodeIndex] += pipesOutFlows[adjacentPipes[i]];
        }

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
            if (pipesOutFlows[adjacentPipes[i]] <= 0)
            {
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
            float IOdplyw = nodesOutFlow[adjacentNodesToAdjacentPipes[i].adjacentNodes[1]];
            if(uncalculatedPipeIndexes.Count > 1)
            {
                Debug.Log($"more than 1 unculculatedpipe, case A");
                for (int j = i + 1; j < uncalculatedPipeIndexes.Count; j++)
                {
                    float JOdplyw = nodesOutFlow[adjacentNodesToAdjacentPipes[j].adjacentNodes[1]];
                    int debugIIndexValue = adjacentNodesToAdjacentPipes[i].adjacentNodes[1];
                    int debugJIndexValue = adjacentNodesToAdjacentPipes[j].adjacentNodes[1];

                    if (IOdplyw == 0 && JOdplyw == 0)
                    {
                        Debug.Log(i + " + " + j);
                        Debug.Log($"wariant Aa - puste sasiednie wezly: {debugIIndexValue} + {debugJIndexValue}");
                        doubleInFlowsOnPipes[uncalculatedPipeIndexes[i]][0] = nodesOutFlow[nodeIndex] / uncalculatedPipeIndexes.Count;
                        doubleInFlowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutFlow[nodeIndex] / uncalculatedPipeIndexes.Count;
                        Debug.Log(doubleInFlowsOnPipes[uncalculatedPipeIndexes[i]][0]);
                        Debug.Log(doubleInFlowsOnPipes[uncalculatedPipeIndexes[j]][0]);
                        Debug.Log($"nodes {nodeIndex} outflow" + nodesOutFlow[nodeIndex]);
                    }
                    else if (IOdplyw > 0 && JOdplyw > 0) 
                    {
                        Debug.Log($"dupa - i node: {debugIIndexValue}, j node; {debugJIndexValue}");
                    }
                    else if (IOdplyw > 0 && JOdplyw == 0)
                    {
                        Debug.Log($"wariant Ab - pusty wezel: {debugIIndexValue} i pelny wezel: {debugJIndexValue}");
                        bool znak = true;
                        PorownanieDwochRur(znak, i, j, uncalculatedPipeIndexes, nodeIndex);
                    }
                    else if (IOdplyw == 0 && JOdplyw > 0)
                    {
                        Debug.Log($"wariant Ac - pusty wezel: {debugIIndexValue} i pelny wezel: {debugJIndexValue}");
                        bool znak = false;
                        PorownanieDwochRur(znak, i, j, uncalculatedPipeIndexes, nodeIndex);
                    }
                    else
                    {
                        Debug.Log($"wariant dupa, nieznany case");
                    }
                    AddUpInFlows(uncalculatedPipeIndexes[i]);
                    KierunekPrzeplywu(uncalculatedPipeIndexes[i], true);
                    nadpiszWartosciWplywow.Invoke(uncalculatedPipeIndexes[i]);

                    AddUpInFlows(uncalculatedPipeIndexes[j]);
                    KierunekPrzeplywu(uncalculatedPipeIndexes[j], true);
                    nadpiszWartosciWplywow.Invoke(uncalculatedPipeIndexes[j]);
                    Debug.Log("dupka");
                }
            }
            else if (uncalculatedPipeIndexes.Count == 1)
            {
                Debug.Log($"less than 2 unculculated pipes, case B pipeindex: {uncalculatedPipeIndexes[i]} nodeindex: {nodeIndex}");
                doubleInFlowsOnPipes[uncalculatedPipeIndexes[i]][0] = nodesOutFlow[nodeIndex];
                AddUpInFlows(uncalculatedPipeIndexes[i]);
                KierunekPrzeplywu(uncalculatedPipeIndexes[i], true);
                nadpiszWartosciWplywow.Invoke(uncalculatedPipeIndexes[i]);
            }
            
            else
            {
                Debug.Log("case C nwm ocb");
            }
        }
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

        if (wplywyNaOdcinkach[uncalculatedPipeIndexes[i]] > rozbioryNaOdcinkach[uncalculatedPipeIndexes[i]])
        {
            CalculateOutflowOnPipe(uncalculatedPipeIndexes[i]);
            nodesInFlow[nodeIndex] += pipesOutFlows[uncalculatedPipeIndexes[i]];
            nodesOutFlow[nodeIndex] += pipesOutFlows[uncalculatedPipeIndexes[i]];
            doubleInFlowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutFlow[nodeIndex];
        }
        else if (wplywyNaOdcinkach[uncalculatedPipeIndexes[i]] == rozbioryNaOdcinkach[uncalculatedPipeIndexes[i]])
        {
            CalculateOutflowOnPipe(uncalculatedPipeIndexes[i]);
            doubleInFlowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutFlow[nodeIndex];
        }
        else if (wplywyNaOdcinkach[uncalculatedPipeIndexes[i]] < rozbioryNaOdcinkach[uncalculatedPipeIndexes[i]])
        {
            CalculateOutflowOnPipe(uncalculatedPipeIndexes[i]);
            doubleInFlowsOnPipes[uncalculatedPipeIndexes[i]][1] = -1 * (pipesOutFlows[uncalculatedPipeIndexes[i]]);
            //mozna dodac case gdy za malo wody odplywa z wezla i nie wypelni?
            doubleInFlowsOnPipes[uncalculatedPipeIndexes[j]][0] = nodesOutFlow[nodeIndex] + pipesOutFlows[uncalculatedPipeIndexes[i]];
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
        foreach (float i in rozbioryNaOdcinkach)
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
        wplywyNaOdcinkach[p] = nodesOutFlow[p];
        KierunekPrzeplywu(p, true);
        nadpiszWartosciWplywow.Invoke(p);

        nodesOutFlow[zNode] = zasilanieZeZbiornika;
        wplywyNaOdcinkach[zPipe] = nodesOutFlow[zNode];
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
        pipesOutFlows[pipeIndex] = wplywyNaOdcinkach[pipeIndex] - rozbioryNaOdcinkach[pipeIndex];
        Debug.Log(pipesOutFlows[pipeIndex]);
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
        wplywyNaOdcinkach[pipeIndex] = doubleInFlowsOnPipes[pipeIndex][0] + doubleInFlowsOnPipes[pipeIndex][1];
        Debug.Log("wartosc z sumowania wplywow " + wplywyNaOdcinkach[pipeIndex]);
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
}