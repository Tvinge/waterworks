using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;


public class UIManager : MonoBehaviour
{
    [SerializeField] GameObject wodociag;
    [SerializeField] AppLogic appLogic;

    //Rozbior
    [SerializeField] TMP_Text[] rozbioryNaOdcinkachText;
    [SerializeField] TMP_Text[] rozbioryNaWezlachText;
    //Zasilanie
    [SerializeField] TMP_Text zasilanieZPompowniText;
    [SerializeField] TMP_Text zasilanieZeZbiornikaText;
    //doubleInflowsOnPipes 
    [SerializeField] TMP_Text[] wplywyNaOdcinkachText;
    [SerializeField] TMP_Text[] wplywyNaWezlachText;
    //odplyw
    [SerializeField] TMP_Text[] odplywyNaOdcinkachText;
    [SerializeField] TMP_Text[] odplywyNaWezlachText;

    [SerializeField] TMP_Text updatedInfo;

    [SerializeField] Transform dzieckoZero; 
    [SerializeField] Transform dzieckoOne; 
    [SerializeField] Transform dzieckoDwa;
    [SerializeField] Transform dzieckoWezel;

    private DataVersion previousDataVersion;


    private void Start()
    {
        appLogic.updateData += OnDataUpdated;
        previousDataVersion = default(DataVersion);
    }

    void OnDataUpdated(DataVersion d)
    {
        int index = d.index;
        float[] nodesRozbiory = d.nodesRozbiory;//
        float[] nodesOutflow = d.nodesOutflow;
        float[] pipesRozbiory = d.pipesRozbiory;//
        bool[] kierunekPrzeplywu = d.kierunekPrzeplywu;
        float[] pipesOutflows = d.pipesOutflows;
        float[] pipesInflows = d.pipesInflows;
        float[][] doubleInflowsOnPipes = d.doubleInflowsOnPipes;

        PrzypiszWartosciRozbiorow(nodesRozbiory, pipesRozbiory);
        UpdateOutflowsOnNodes(nodesOutflow);
        UpdatePipesOutflow(pipesOutflows, kierunekPrzeplywu);
        UpdateInflowValues(pipesOutflows, pipesInflows, doubleInflowsOnPipes, kierunekPrzeplywu);

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
    public void UpdateInflowValues(float[] pipesOutflows, float[] pipesInflows, float[][] doubleInFlowsOnPipes, bool[] kierunekPrzeplywu)
    {
        for (int pipeIndex = 0; pipeIndex < pipesOutflows.Length; pipeIndex++)
        {
            AssignChildTransform(pipeIndex);
            if (pipesOutflows[pipeIndex] != 0)
            {
                if (kierunekPrzeplywu[pipeIndex] == true)
                {
                    wplywyNaOdcinkachText[pipeIndex] = dzieckoZero.GetComponent<TMP_Text>();
                    wplywyNaOdcinkachText[pipeIndex].text = pipesInflows[pipeIndex].ToString();
                }
                else if (kierunekPrzeplywu[pipeIndex] == false)
                {
                    wplywyNaOdcinkachText[pipeIndex] = dzieckoDwa.GetComponent<TMP_Text>();
                    wplywyNaOdcinkachText[pipeIndex].text = pipesInflows[pipeIndex].ToString();
                }
            }
            else if (pipesOutflows[pipeIndex] == 0)
            {
                if (kierunekPrzeplywu[pipeIndex] == true)
                {
                    wplywyNaOdcinkachText[pipeIndex] = dzieckoZero.GetComponent<TMP_Text>();
                    wplywyNaOdcinkachText[pipeIndex].text = doubleInFlowsOnPipes[pipeIndex][0].ToString();

                    wplywyNaOdcinkachText[pipeIndex] = dzieckoDwa.GetComponent<TMP_Text>();
                    wplywyNaOdcinkachText[pipeIndex].text = doubleInFlowsOnPipes[pipeIndex][1].ToString();
                }
                else if (kierunekPrzeplywu[pipeIndex] == false)
                {
                    wplywyNaOdcinkachText[pipeIndex] = dzieckoZero.GetComponent<TMP_Text>();
                    wplywyNaOdcinkachText[pipeIndex].text = doubleInFlowsOnPipes[pipeIndex][1].ToString();

                    wplywyNaOdcinkachText[pipeIndex] = dzieckoDwa.GetComponent<TMP_Text>();
                    wplywyNaOdcinkachText[pipeIndex].text = doubleInFlowsOnPipes[pipeIndex][0].ToString();
                }
            }
        }
    }

    //przypisywanie wartosci rozbiorow na odcinkach
    public void PrzypiszWartosciRozbiorow(float[] nodesRozbiory, float[] pipesRozbiory)
    {
        string node = "wezelek";
        for (int i = 0; i < nodesRozbiory.Length; i++)
        {
            AssignChildTransform(i, node);
            rozbioryNaWezlachText[i] = dzieckoWezel.GetComponent<TMP_Text>();
            rozbioryNaWezlachText[i].text = nodesRozbiory[i].ToString();
            rozbioryNaWezlachText[i].color = Color.red;
        }
        for (int i = 0; i < pipesRozbiory.Length; i++)
        {
            AssignChildTransform(i);
            rozbioryNaOdcinkachText[i] = dzieckoOne.GetComponent<TMP_Text>();
            rozbioryNaOdcinkachText[i].text = pipesRozbiory[i].ToString();
            rozbioryNaOdcinkachText[i].color = Color.red;
        }
    }

    //przypisywanie wartosci odplywow na odcinkach
    public void UpdatePipesOutflow(float[] pipesOutflows, bool[] kierunekPrzeplywu)
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
                odplywyNaOdcinkachText[i].text = pipesOutflows[i].ToString();
                odplywyNaOdcinkachText[i].color = Color.green;
            }
        }
    }

    public void UpdateOutflowsOnNodes(float[] nodesOutflow)
    {
        updatedInfo.text = "rozbiory na wezlach: ";
        for (int nodeIndex = 0; nodeIndex < nodesOutflow.Length; nodeIndex++)
        {
            updatedInfo.text = updatedInfo.text + $"\nRozbior na wezle {nodeIndex}:  {nodesOutflow[nodeIndex]}";
        }
    }
}
