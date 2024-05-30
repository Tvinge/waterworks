using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;
using System;
using System.Reflection;

public class UIManager : MonoBehaviour
{
    public Action updateDataset;

    [SerializeField] GameObject wodociag;
    [SerializeField] AppLogic appLogic;
    [SerializeField] DataLoader dataLoader;

    [SerializeField] TMP_Dropdown dropdown;
    [SerializeField] TMP_Text updatedInfo;

    [SerializeField] TMP_Text[] rozbioryNaOdcinkachText;
    [SerializeField] TMP_Text[] rozbioryNaWezlachText;
    [SerializeField] TMP_Text zasilanieZPompowniText;
    [SerializeField] TMP_Text zasilanieZeZbiornikaText;
    [SerializeField] TMP_Text[] wplywyNaOdcinkachText;
    [SerializeField] TMP_Text[] wplywyNaWezlachText;
    [SerializeField] TMP_Text[] odplywyNaOdcinkachText;
    [SerializeField] TMP_Text[] odplywyNaWezlachText;

    [SerializeField] Transform dzieckoZero;
    [SerializeField] Transform dzieckoOne;
    [SerializeField] Transform dzieckoDwa;
    [SerializeField] Transform dzieckoWezel;

    private DataVersion previousDataVersion;

    List<string> dropdownMenuOptions = new List<string>();

    private void Awake()
    {
        appLogic.updateDataVersion += OnDataUpdated;
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
    }

    public void HandleDropDownInputData(int DatasetIndex)
    {
        appLogic.updateDataVersion?.Invoke(dataLoader.ConvertDatasetToDataVersion(DatasetIndex));
    }

    void OnDataUpdated(DataVersion d)
    {
        Debug.Log("dataevent in UImanager");
        PrzypiszWartosciRozbiorow(d.nodesRozbiory, d.pipesRozbiory);
        UpdateOutflowsOnNodes(d.nodesOutflows);
        UpdatePipesOutflow(d.pipesOutflows, d.kierunekPrzeplywu);
        UpdateInflowValues(d.pipesOutflows, d.pipesInflows, d.doubleInflowsOnPipes, d.kierunekPrzeplywu);

        previousDataVersion = d;
    }

    void AssignChildTransform(int Index)
    {
        dzieckoZero = GetComponent<Transform>().GetChild(1).GetChild(0).GetChild(Index).GetChild(0).GetChild(0).GetChild(0);
        dzieckoOne = GetComponent<Transform>().GetChild(1).GetChild(0)?.GetChild(Index)?.GetChild(0)?.GetChild(1)?.GetChild(0);
        dzieckoDwa = GetComponent<Transform>().GetChild(1).GetChild(0).GetChild(Index).GetChild(0).GetChild(2).GetChild(0);
    }

    void AssignChildTransform(int Index, string node)
    {
        dzieckoWezel = GetComponent<Transform>().GetChild(1).GetChild(1).GetChild(Index).GetChild(2).GetChild(0);
    }

    //przypisywanie wartosci wplywow na odcinkach
    public void UpdateInflowValues(decimal[] pipesOutflows, decimal[] pipesInflows, decimal[][] doubleInFlowsOnPipes, bool[] kierunekPrzeplywu)
    {
        for (int pipeIndex = 0; pipeIndex < pipesOutflows.Length; pipeIndex++)
        {
            AssignChildTransform(pipeIndex);
            if (pipesOutflows[pipeIndex] != 0)
            {
                if (kierunekPrzeplywu[pipeIndex] == true)
                {
                    wplywyNaOdcinkachText[pipeIndex] = dzieckoZero.GetComponent<TMP_Text>();
                    wplywyNaOdcinkachText[pipeIndex].text = pipesInflows[pipeIndex].ToString("f2");
                }
                else if (kierunekPrzeplywu[pipeIndex] == false)
                {
                    wplywyNaOdcinkachText[pipeIndex] = dzieckoDwa.GetComponent<TMP_Text>();
                    wplywyNaOdcinkachText[pipeIndex].text = pipesInflows[pipeIndex].ToString("f2");
                }
            }
            else if (pipesOutflows[pipeIndex] == 0)
            {
                if (kierunekPrzeplywu[pipeIndex] == true)
                {
                    wplywyNaOdcinkachText[pipeIndex] = dzieckoZero.GetComponent<TMP_Text>();
                    wplywyNaOdcinkachText[pipeIndex].text = doubleInFlowsOnPipes[pipeIndex][0].ToString("f2");

                    wplywyNaOdcinkachText[pipeIndex] = dzieckoDwa.GetComponent<TMP_Text>();
                    wplywyNaOdcinkachText[pipeIndex].text = doubleInFlowsOnPipes[pipeIndex][1].ToString("f2");
                }
                else if (kierunekPrzeplywu[pipeIndex] == false)
                {
                    wplywyNaOdcinkachText[pipeIndex] = dzieckoZero.GetComponent<TMP_Text>();
                    wplywyNaOdcinkachText[pipeIndex].text = doubleInFlowsOnPipes[pipeIndex][1].ToString("f2");

                    wplywyNaOdcinkachText[pipeIndex] = dzieckoDwa.GetComponent<TMP_Text>();
                    wplywyNaOdcinkachText[pipeIndex].text = doubleInFlowsOnPipes[pipeIndex][0].ToString("f2");
                }
            }
        }
    }

    //przypisywanie wartosci rozbiorow na odcinkach
    public void PrzypiszWartosciRozbiorow(decimal[] nodesRozbiory, decimal[] pipesRozbiory)
    {
        string node = "wezelek";
        for (int i = 0; i < nodesRozbiory.Length; i++)
        {
            AssignChildTransform(i, node);
            rozbioryNaWezlachText[i] = dzieckoWezel.GetComponent<TMP_Text>();
            rozbioryNaWezlachText[i].text = nodesRozbiory[i].ToString("f2");
            rozbioryNaWezlachText[i].color = Color.red;
        }
        for (int i = 0; i < pipesRozbiory.Length; i++)
        {
            AssignChildTransform(i);
            rozbioryNaOdcinkachText[i] = dzieckoOne.GetComponent<TMP_Text>();
            rozbioryNaOdcinkachText[i].text = pipesRozbiory[i].ToString("f2");
            rozbioryNaOdcinkachText[i].color = Color.red;
        }
    }

    //przypisywanie wartosci odplywow na odcinkach
    public void UpdatePipesOutflow(decimal[] pipesOutflows, bool[] kierunekPrzeplywu)
    {
        for(int i = 0; i < pipesOutflows.Length; i++)
        {
            AssignChildTransform(i);
            if (pipesOutflows[i] != 0)
            {
                if (kierunekPrzeplywu[i] == true)
                {
                    odplywyNaOdcinkachText[i] = dzieckoDwa.GetComponent<TMP_Text>();
                }
                else if (kierunekPrzeplywu[i] == false)
                {
                    odplywyNaOdcinkachText[i] = dzieckoZero.GetComponent<TMP_Text>();
                }
                odplywyNaOdcinkachText[i].text = pipesOutflows[i].ToString("f2");
                odplywyNaOdcinkachText[i].color = Color.green;
            }
        }
    }

    public void UpdateOutflowsOnNodes(decimal[] nodesOutflow)
    {
        updatedInfo.text = "rozbiory na wezlach: ";
        for (int nodeIndex = 0; nodeIndex < nodesOutflow.Length; nodeIndex++)
        {
            updatedInfo.text = updatedInfo.text + $"\nRozbior na wezle {nodeIndex}:  {nodesOutflow[nodeIndex]}";
        }
    }
}
