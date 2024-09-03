using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;
using System;
using System.Reflection;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public Action updateDataset;

    AppLogic appLogic;
    DataLoader dataLoader;
    IterationManager iterationManager;

    [SerializeField] GameObject waterwork;
    [SerializeField] TMP_Dropdown dropdown;
    [SerializeField] TMP_Text updatedInfo;
    [SerializeField] Button button;
    [SerializeField] GameObject CalculationTableObject;


    [SerializeField] TMP_Text[] consumptionsOnPipesText;
    [SerializeField] TMP_Text[] consumptionsOnNodesText;
    [SerializeField] TMP_Text supplyFromPumpStationText;
    [SerializeField] TMP_Text supplyFromReservoirText;
    [SerializeField] TMP_Text[] inflowsOnPipesText;
    [SerializeField] TMP_Text[] inflowsOnNodesText;
    [SerializeField] TMP_Text[] outflowsOnPipesText;
    [SerializeField] TMP_Text[] outflowsOnNodesText;

    [SerializeField] Transform childZero;
    [SerializeField] Transform childOne;
    [SerializeField] Transform childTwo;
    [SerializeField] Transform childNode;

    private DataVersion previousDataVersion;

    List<DataVersion> choosableDataVersions = new List<DataVersion>();
    List<string> dropdownMenuOptions = new List<string>();

    private void Awake()
    {
        appLogic = FindObjectOfType<AppLogic>();
        dataLoader = FindObjectOfType<DataLoader>();
        iterationManager = FindObjectOfType<IterationManager>();

        appLogic.updateDataVersion += OnDataUpdated;
        appLogic.resetSimulation += ResetUI;
        iterationManager.updateIterationResultsData += UpdateResultsOfIteration;
        //appLogic.updateUIData += OnDataUpdated;
        previousDataVersion = default(DataVersion);


    }

    private void Start()
    {
        for (int i = 0; i < 16; i++)
        {
            dropdownMenuOptions.Add("DataSet" + i);
        }
        dropdown.ClearOptions();
        dropdown.AddOptions(dropdownMenuOptions);
;
    }
    void UpdateResultsOfIteration(List<RingData> ringDatas, int dataType, bool isFirstIterationForThisData)
    {
        ResetUI();
        Debug.Log("update iteration results UImanager");

        decimal[] nodesOutflows = new decimal[8];
        decimal[] nodesConsumption = new decimal[8];
        decimal[] pipesInflows = new decimal[9];
        decimal[] pipesOutflows = new decimal[9];
        decimal[] pipesConsumption = new decimal[9];
        decimal[][] doubleInflowsOnPipes = new decimal[9][];
        bool[] flowDirection = new bool[9];


        foreach (var ringData in ringDatas)
        {
            foreach (var pipe in ringData.Pipes)
            {
                pipesInflows[pipe.index] = pipe.inflow;
                pipesOutflows[pipe.index] = pipe.outflow;
                pipesConsumption[pipe.index] = pipe.consumption;
                flowDirection[pipe.index] = pipe.flowDirection;
            }
            foreach (var node in ringData.Nodes)
            {
                nodesOutflows[node.index] = node.outflow;
                nodesConsumption[node.index] = node.consumption;
           
            }
        }

        for (int i = 0; i < doubleInflowsOnPipes.Length; i++)
        {
         
            doubleInflowsOnPipes[i] = new decimal[2];
        }


        SetConsumptionValues(nodesConsumption, pipesConsumption);
        UpdateOutflowsOnNodes(nodesOutflows);
        UpdatePipesOutflow(pipesOutflows, flowDirection);
        UpdateInflowValues(pipesOutflows, pipesInflows, doubleInflowsOnPipes, flowDirection);

        //previousDataVersion = d;
    }
    void ResetUI()
    {
        for (int i = 0; i < outflowsOnPipesText.Length; i++)
        {
            if (outflowsOnPipesText[i] != null)
            {
                outflowsOnPipesText[i].color = Color.white;
            }
            if (inflowsOnPipesText[i] != null)
            {
                inflowsOnPipesText[i].color = Color.white;
            }
        }
    }

    public void HandleDropDownInputData(int DatasetIndex)
    {
        appLogic.updateDataVersion?.Invoke(dataLoader.ConvertDatasetToDataVersion(DatasetIndex));
    }

    void OnDataUpdated(DataVersion d)
    {
        if (!choosableDataVersions.Contains(d))
        {
            choosableDataVersions.Add(d);
            dropdownMenuOptions.Add("DataSet" + dropdownMenuOptions.Count + choosableDataVersions.Count);

            dropdown.ClearOptions();
            dropdown.AddOptions(dropdownMenuOptions);
        }
        ResetUI();
        Debug.Log("dataevent in UImanager");
        SetConsumptionValues(d.nodesConsumptions, d.pipesConsumptions);
        UpdateOutflowsOnNodes(d.nodesOutflows);
        UpdatePipesOutflow(d.pipesOutflows, d.flowDirection);
        UpdateInflowValues(d.pipesOutflows, d.pipesInflows, d.doubleInflowsOnPipes, d.flowDirection);

        previousDataVersion = d;
    }

    void AssignChildTransform(int index)
    {
        childZero = GetComponent<Transform>().GetChild(1).GetChild(0).GetChild(index).GetChild(0).GetChild(0).GetChild(0);
        childOne = GetComponent<Transform>().GetChild(1).GetChild(0)?.GetChild(index)?.GetChild(0)?.GetChild(1)?.GetChild(0);
        childTwo = GetComponent<Transform>().GetChild(1).GetChild(0).GetChild(index).GetChild(0).GetChild(2).GetChild(0);
    }

    void AssignChildTransform(int index, string node)
    {
        childNode = GetComponent<Transform>().GetChild(1).GetChild(1).GetChild(index).GetChild(2).GetChild(0);
    }

    //przypisywanie wartosci wplywow na odcinkach
    public void UpdateInflowValues(decimal[] pipesOutflows, decimal[] pipesInflows, decimal[][] doubleInFlowsOnPipes, bool[] flowDirection)
    {
        inflowsOnPipesText = new TMP_Text[pipesOutflows.Length];

        for (int pipeIndex = 0; pipeIndex < pipesOutflows.Length; pipeIndex++)
        {
            AssignChildTransform(pipeIndex);
            if (pipesOutflows[pipeIndex] != 0)
            {
                if (flowDirection[pipeIndex] == true)
                {
                    inflowsOnPipesText[pipeIndex] = childZero.GetComponent<TMP_Text>();
                    inflowsOnPipesText[pipeIndex].text = pipesInflows[pipeIndex].ToString("f2");
                }
                else if (flowDirection[pipeIndex] == false)
                {
                    inflowsOnPipesText[pipeIndex] = childTwo.GetComponent<TMP_Text>();
                    inflowsOnPipesText[pipeIndex].text = pipesInflows[pipeIndex].ToString("f2");
                }
            }
            else if (pipesOutflows[pipeIndex] == 0)
            {
                if (flowDirection[pipeIndex] == true)
                {
                    inflowsOnPipesText[pipeIndex] = childZero.GetComponent<TMP_Text>();
                    inflowsOnPipesText[pipeIndex].text = doubleInFlowsOnPipes[pipeIndex][0].ToString("f2");

                    inflowsOnPipesText[pipeIndex] = childTwo.GetComponent<TMP_Text>();
                    inflowsOnPipesText[pipeIndex].text = doubleInFlowsOnPipes[pipeIndex][1].ToString("f2");
                }
                else if (flowDirection[pipeIndex] == false)
                {
                    inflowsOnPipesText[pipeIndex] = childZero.GetComponent<TMP_Text>();
                    inflowsOnPipesText[pipeIndex].text = doubleInFlowsOnPipes[pipeIndex][1].ToString("f2");

                    inflowsOnPipesText[pipeIndex] = childTwo.GetComponent<TMP_Text>();
                    inflowsOnPipesText[pipeIndex].text = doubleInFlowsOnPipes[pipeIndex][0].ToString("f2");
                }
            }
        }
    }

    //przypisywanie wartosci rozbiorow na odcinkach
    public void SetConsumptionValues(decimal[] nodesConsumptions, decimal[] pipesConsumptions)
    {
        consumptionsOnNodesText = new TMP_Text[nodesConsumptions.Length];
        consumptionsOnPipesText = new TMP_Text[pipesConsumptions.Length];

        string node = "wezelek";
        for (int i = 0; i < nodesConsumptions.Length; i++)
        {
            AssignChildTransform(i, node);
            consumptionsOnNodesText[i] = childNode.GetComponent<TMP_Text>();
            consumptionsOnNodesText[i].text = nodesConsumptions[i].ToString("f2");
            consumptionsOnNodesText[i].color = Color.red;
        }
        for (int i = 0; i < pipesConsumptions.Length; i++)
        {
            AssignChildTransform(i);
            consumptionsOnPipesText[i] = childOne.GetComponent<TMP_Text>();
            consumptionsOnPipesText[i].text = pipesConsumptions[i].ToString("f2");
            consumptionsOnPipesText[i].color = Color.red;
        }
    }

    //przypisywanie wartosci odplywow na odcinkach
    public void UpdatePipesOutflow(decimal[] pipesOutflows, bool[] flowDirection)
    {
        outflowsOnPipesText = new TMP_Text[pipesOutflows.Length];
        for(int i = 0; i < pipesOutflows.Length; i++)
        {
            AssignChildTransform(i);

            if (pipesOutflows[i] != 0)
            {
                if (flowDirection[i] == true)
                {
                    outflowsOnPipesText[i] = childTwo.GetComponent<TMP_Text>();
                }
                else if (flowDirection[i] == false)
                {
                    outflowsOnPipesText[i] = childZero.GetComponent<TMP_Text>();
                }
                outflowsOnPipesText[i].text = pipesOutflows[i].ToString("f2");
                outflowsOnPipesText[i].color = Color.green;
            }
        }
    }

    public void UpdateOutflowsOnNodes(decimal[] nodesOutflow)
    {
        updatedInfo.text = "click r to reset \noutflows on nodes: ";
        for (int nodeIndex = 0; nodeIndex < nodesOutflow.Length; nodeIndex++)
        {
            updatedInfo.text = updatedInfo.text + $"\nOutflow on node {nodeIndex}:  {nodesOutflow[nodeIndex].ToString("f2")}";
        }
    }

    public void OpenCalculationTable()
    {
        bool isActive = CalculationTableObject.activeSelf;

        if (isActive == true)
        {
            CalculationTableObject.SetActive(false);
        }
        else
        {
            CalculationTableObject.SetActive(true);
        }
    }

}
