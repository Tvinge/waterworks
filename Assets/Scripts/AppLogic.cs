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
    DataVersion defaultDataVersion = DataVersion.CreateDefault();
    List<DataVersion> dataVersions = new List<DataVersion>();

    List<int> nodesWithOutflowsOnStart = new List<int>();

    private void Awake()
    {
        DeclareWplywyArray();
        UstalWskazowkiZegara();
        InitializePositions();

        updateDataVersion += OnDataUpdated;

    }
    void Start()
    {
        CalculateQzbiornika(defaultDataVersion);
        ZasilanieZPompowniZbiornika();
        WplywNaOdcinkachZPompowniZbiornika();
        Blabla();

        for (int i = 0; i < defaultDataVersion.polozenieWezlow.Length; i++)
        {
            if (defaultDataVersion.nodesOutflow[i] > 0)
            {
                nodesWithOutflowsOnStart.Add(i);
            }
        }


        //UpdateDataVersion();
        updateDataVersion?.Invoke(defaultDataVersion);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            //if (stepCount < nodesOutflow.Length)
            {
                Debug.Log("**********************************ITERATION***********************************");
                startIteration?.Invoke();
                //UpdateDataVersion();
                updateDataVersion?.Invoke(defaultDataVersion);
            }
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetApp();
        }
    }

    void Blabla()
    {
        defaultDataVersion._nodeAndAdjacentPipes.Clear();
        defaultDataVersion._pipesAdjacentNodes.Clear();
        for (int i = 0; i < defaultDataVersion.polozenieWezlow.Length; i++)
        {
            SearchForAdjacentPipes(i);
        }
        for (int i = 0; i < defaultDataVersion.dlugoscOdcinka.Length; i++)
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
        if (d.wspolczynnik != defaultDataVersion.wspolczynnik && d.zasilanieZPompowni != defaultDataVersion.zasilanieZPompowni)
        {
            defaultDataVersion.wspolczynnik = d.wspolczynnik;
            defaultDataVersion.zasilanieZPompowni = d.zasilanieZPompowni;
            defaultDataVersion.nodesRozbiory = d.nodesRozbiory;
            defaultDataVersion.pipesRozbiory = d.pipesRozbiory;

            //tutaj dodac reszte zapisywanek po dodaniu kolejnych wartoœci w clasie dataversion
            Start();
        }
    }

    #region
    public static float CalculateQzbiornika(DataVersion data)
    {

        data.zasilanieZeZbiornika = CalculateQhmax(data) - data.zasilanieZPompowni;
        return data.zasilanieZeZbiornika;
    }

    public static float CalculateQhmax(DataVersion data)
    {

        float value = 0;

        foreach (float i in data.pipesRozbiory)
        {
            value += i;
        }

        foreach (float i in data.nodesRozbiory)
        {
            value += i;
        }

        return value;
    }

    float[] ZasilanieZPompowniZbiornika()
    {
        int p = 0;
        int zPipe = defaultDataVersion.dlugoscOdcinka.Length - 1;
        int zNode = defaultDataVersion.polozenieWezlow.Length - 1;

        defaultDataVersion.nodesOutflow[p] = defaultDataVersion.zasilanieZPompowni;
        KierunekPrzeplywu(p, true);

        defaultDataVersion.nodesOutflow[zNode] = defaultDataVersion.zasilanieZeZbiornika;
        KierunekPrzeplywu(zPipe, false);

        return defaultDataVersion.nodesOutflow;
    }

    float[] WplywNaOdcinkachZPompowniZbiornika()
    {
        int p = 0;
        int zPipe = defaultDataVersion.dlugoscOdcinka.Length - 1;
        int zNode = defaultDataVersion.polozenieWezlow.Length - 1;

        defaultDataVersion.pipesInflows[p] = defaultDataVersion.nodesOutflow[p];
        KierunekPrzeplywu(p, true);

        defaultDataVersion.pipesInflows[zPipe] = defaultDataVersion.nodesOutflow[zNode];
        KierunekPrzeplywu(zPipe, false);

        return defaultDataVersion.pipesInflows;
    }

    bool[] UstalWskazowkiZegara()
    {
        for (int i = 0; i < defaultDataVersion.kierunekRuchuWskazowekZegara.Length; i++)
        {
            if (i == 0)
            {
                defaultDataVersion.kierunekRuchuWskazowekZegara[i] = true;
            }
            else if (i == defaultDataVersion.kierunekRuchuWskazowekZegara.Length - 1)
            {
                defaultDataVersion.kierunekRuchuWskazowekZegara[i] = false;
            }
            else
            {
                if (i == 1 || i == 2 || i == 5)
                    defaultDataVersion.kierunekRuchuWskazowekZegara[i] = true;
                else
                    defaultDataVersion.kierunekRuchuWskazowekZegara[i] = false;
            }
        }
        return defaultDataVersion.kierunekRuchuWskazowekZegara;
    }



    bool KierunekPrzeplywu(int pipeIndex, bool kierunekPrzeplyw)
    {
        defaultDataVersion.kierunekPrzeplywu[pipeIndex] = kierunekPrzeplyw;
        return defaultDataVersion.kierunekPrzeplywu[pipeIndex];
    }

    Vector3[] DeclarePipePositions()
    {
        for (int i = 0; i < defaultDataVersion.dlugoscOdcinka.Length; i++)
        {
            defaultDataVersion.pipesPositions[i] = GetComponent<Transform>().GetChild(0).GetChild(i).GetComponent<RectTransform>().anchoredPosition;
            //Debug.Log(pipesPositions[i]);
        }
        return defaultDataVersion.pipesPositions;
    }

    Vector3[] DeclareNodesPositions()
    {
        for (int i = 0; i < defaultDataVersion.polozenieWezlow.Length; i++)
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
        for (int pipeIndex = 0; pipeIndex < defaultDataVersion.dlugoscOdcinka.Length; pipeIndex++)
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
        for (int nodeIndex = 0; nodeIndex < defaultDataVersion.polozenieWezlow.Length; nodeIndex++)
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


    #endregion


    void DeclareWplywyArray()
    {
        for (int i = 0; i < 9; i++)
        {
            if (defaultDataVersion.doubleInflowsOnPipes[i] == null)
            {
                defaultDataVersion.doubleInflowsOnPipes[i] = new float[2];
            }
        }
    }

    void ResetApp()
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
        for (int i = 0; i < defaultDataVersion.nodesOutflow.Length; i++)
        {
            defaultDataVersion.nodesInflow[i] = 0;
            defaultDataVersion.nodesOutflow[i] = 0;
        }

        resetSimulation.Invoke();
        Start();
    }
}