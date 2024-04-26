using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;
using System;

public class AppLogic : MonoBehaviour
{
    public Action<DataVersion> updateDataVersion;
    public Action<DataVersion> updateDataSet;
    public Action updateUIData;
    public Action startIteration;
    public Action initializeDictionaries;
    public Action resetSimulation;

    DataVersion dataVersion = new DataVersion();

    List<DataVersion> dataVersions = new List<DataVersion>();


    public float[] nodesRozbiory { get; set; } = { 0f, 17f, 12f, 23f, 26f, 29f, 30f, 0f };
    public float[] polozenieWezlow { get; set; } = {145f, 147f, 146f, 151f, 154f, 159f, 168f, 192f};

    public float[] pipesRozbiory { get; set; } = { 0f, 21f, 25f, 11f, 32f, 15f, 26f, 15f, 0f };
    float[] dlugoscOdcinka = { 150f, 400f, 350f, 320f, 290f, 300f, 315f, 290f, 250f };
    float[] wysokoscZabudowy = { 0f, 20f, 25f, 15f, 20f, 15f, 15f, 15f, 0f };
    float[] nodesInflow = new float[8];
    public float[] nodesOutflow { get; set; } = new float[8];
    public float[] pipesOutflows { get; private set; } = new float[9];
    public float[] pipesInflows { get; private set; } = new float[9];
    public float[][] doubleInflowsOnPipes { get; private set; } = new float[9][];
    float zasilanieZPompowni = 188;
    float maxQh;
    //Qhmax / Qhmin wynosi:
    float wspolczynnik = 1.75f;
    float zasilanieZeZbiornika;

    Vector3[] pipesPositions = Enumerable.Repeat(Vector3.zero, 9).ToArray();
    Vector3[] nodesPositions = Enumerable.Repeat(Vector3.zero, 8).ToArray();



    //Gdy przep³yw nastêpuje zgodnie ze wskazowkami zegara bool == 1/true
    //do nadpisania przez program, wartosc duynamiczna, ustalana przez przeplyw
    public bool[] kierunekPrzeplywu { get; private set; } = Enumerable.Repeat(false, 9).ToArray();
    
    //punkt odniesienia dla kierunku przeplywu, wartosc stala, niezmienialna
    //Gdy kierunek wodociagu odpowiada ruchowi wskazowek zegara - true
    bool[] kierunekRuchuWskazowekZegara = Enumerable.Repeat(true, 9).ToArray();

    //up right - true
    //bot left - false
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



    private void Awake()
    {
        DeclareWplywyArray();
        UstalWskazowkiZegara();
        InitializePositions();
        //LoadDefaultDataVersion();

        updateDataVersion += OnDataUpdated;

    }
    void Start()
    {
        ObliczQzbiornika();
        ZasilanieZPompowniZbiornika();
        Blabla();

        for (int i = 0; i < polozenieWezlow.Length; i++)
        {
            if (nodesOutflow[i] > 0)
            {
                startingInFlowsNodeIndexes.Add(i);
            }
        }


        UpdateDataVersion();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            //if (stepCount < nodesOutflow.Length)
            {
                Debug.Log("**********************************ITERATION***********************************");
                startIteration?.Invoke();
                UpdateDataVersion();
            }
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetApp();
        }
    }

    void Blabla()
    {
        _nodeAndAdjacentPipes.Clear();
        _pipesAdjacentNodes.Clear();
        for (int i = 0; i < polozenieWezlow.Length; i++)
        {
            SearchForAdjacentPipes(i);
        }
        for (int i = 0; i < dlugoscOdcinka.Length; i++)
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
        if(d.wspolczynnik != wspolczynnik && d.zasilanieZPompowni != zasilanieZPompowni)
        {
            wspolczynnik = d.wspolczynnik;
            zasilanieZPompowni = d.zasilanieZPompowni;
            nodesRozbiory = d.nodesRozbiory;
            pipesRozbiory = d.pipesRozbiory;
            
            //tutaj dodac reszte zapisywanek po dodaniu kolejnych wartoœci w clasie dataversion
            Start();
        }
    }
    /*
    void LoadDefaultDataVersion()
    {
        DefaultVersion d = new DefaultVersion();

        zasilanieZPompowni = d.zasilanieZPompowni;
        wspolczynnik = d.wspolczynnik;
        nodesRozbiory = d.nodesRozbiory;
        nodesOutflow = d.nodesOutflow;
        nodesInflow = d.nodesInflow;
        pipesRozbiory = d.pipesRozbiory;
        kierunekPrzeplywu = d.kierunekPrzeplywu;
        pipesOutflows = d.pipesOutflows;
        pipesInflows = d.pipesInflows;
        doubleInflowsOnPipes = d.doubleInflowsOnPipes;
        _nodeAndAdjacentPipes = d._nodeAndAdjacentPipes;
        _pipesAdjacentNodes = d._pipesAdjacentNodes;

    }*/


    void UpdateDataVersion()
    {
        dataVersion.zasilanieZPompowni = zasilanieZPompowni;
        dataVersion.wspolczynnik = wspolczynnik;
        dataVersion.nodesRozbiory = nodesRozbiory;
        dataVersion.nodesOutflow = nodesOutflow;
        dataVersion.nodesInflow = nodesInflow;
        dataVersion.pipesRozbiory = pipesRozbiory;
        dataVersion.kierunekPrzeplywu = kierunekPrzeplywu;
        dataVersion.pipesOutflows = pipesOutflows;
        dataVersion.pipesInflows = pipesInflows;
        dataVersion.doubleInflowsOnPipes = doubleInflowsOnPipes;

        if (dataVersion._nodeAndAdjacentPipes != _nodeAndAdjacentPipes)
        {
            DeclareNodesPositions();
        }


        dataVersion._nodeAndAdjacentPipes = _nodeAndAdjacentPipes;
        dataVersion._pipesAdjacentNodes = _pipesAdjacentNodes;

        dataVersions.Add(dataVersion);
        if (updateDataVersion != null)
        {
            updateDataVersion?.Invoke(dataVersion);
        }
    }
  

    #region
    void ObliczQzbiornika()
    {
        
        zasilanieZeZbiornika = ObliczQhmax() - zasilanieZPompowni;
    }

    float ObliczQhmax()
    {
        float value = 0;

        foreach (float i in pipesRozbiory)
        {
            value += i;
        }

        foreach (float i in nodesRozbiory)
        {
            value += i;
        }

        return value;
    }

    void ZasilanieZPompowniZbiornika()
    {
        int p = 0;
        int zPipe = dlugoscOdcinka.Length - 1;
        int zNode = polozenieWezlow.Length - 1;

        nodesOutflow[p] = zasilanieZPompowni;
        pipesInflows[p] = nodesOutflow[p];
        KierunekPrzeplywu(p, true);

        nodesOutflow[zNode] = zasilanieZeZbiornika;
        pipesInflows[zPipe] = nodesOutflow[zNode];
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


    #endregion


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
        for (int i = 0; i < pipesOutflows.Length; i++)
        {
            pipesInflows[i] = 0;
            pipesOutflows[i] = 0;

            for (int j = 0; j < doubleInflowsOnPipes[i].Length; j++)
            {
                doubleInflowsOnPipes[i][j] = 0;
            }
        }
        for (int i = 0; i < nodesOutflow.Length; i++)
        {
            nodesInflow[i] = 0;
            nodesOutflow[i] = 0;
        }

        resetSimulation.Invoke();
        Start();
    }
}