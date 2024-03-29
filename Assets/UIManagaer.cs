using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;


public class UIManagaer : MonoBehaviour
{
    [SerializeField] AppLogic appLogic;
    [SerializeField] GameObject wodociag;

    //Rozbior
    [SerializeField] TMP_Text[] rozbioryNaOdcinkachText;
    [SerializeField] TMP_Text[] rozbioryNaWezlachText;
    //Zasilanie
    [SerializeField] TMP_Text zasilanieZPompowniText;
    [SerializeField] TMP_Text zasilanieZeZbiornikaText;
    //doubleInFlowsOnPipes 
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

    private void Start()
    {
    }        
    private void Update()
    {

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
    public void PrzypiszWartosciWplywow(int pipeIndex)
    {
        AssignChildTransform(pipeIndex);
        //Debug.Log(appLogic.kierunekPrzeplywu[i]);
        if (appLogic.pipesOutFlows[pipeIndex] != 0)
        {
            if (appLogic.kierunekPrzeplywu[pipeIndex] == true)
            {
                wplywyNaOdcinkachText[pipeIndex] = dzieckoZero.GetComponent<TMP_Text>();
                wplywyNaOdcinkachText[pipeIndex].text = appLogic.pipesInFlows[pipeIndex].ToString();
            }
            else if (appLogic.kierunekPrzeplywu[pipeIndex] == false)
            {
                wplywyNaOdcinkachText[pipeIndex] = dzieckoDwa.GetComponent<TMP_Text>();
                wplywyNaOdcinkachText[pipeIndex].text = appLogic.pipesInFlows[pipeIndex].ToString();
            }
        }
        else if (appLogic.pipesOutFlows[pipeIndex] == 0)
        {
            if (appLogic.kierunekPrzeplywu[pipeIndex] == true)
            {
                wplywyNaOdcinkachText[pipeIndex] = dzieckoZero.GetComponent<TMP_Text>();
                wplywyNaOdcinkachText[pipeIndex].text = appLogic.doubleInFlowsOnPipes[pipeIndex][0].ToString();

                wplywyNaOdcinkachText[pipeIndex] = dzieckoDwa.GetComponent<TMP_Text>();
                wplywyNaOdcinkachText[pipeIndex].text = appLogic.doubleInFlowsOnPipes[pipeIndex][1].ToString();
            }
            else if (appLogic.kierunekPrzeplywu[pipeIndex] == false)
            {
                wplywyNaOdcinkachText[pipeIndex] = dzieckoZero.GetComponent<TMP_Text>();
                wplywyNaOdcinkachText[pipeIndex].text = appLogic.doubleInFlowsOnPipes[pipeIndex][1].ToString();

                wplywyNaOdcinkachText[pipeIndex] = dzieckoDwa.GetComponent<TMP_Text>();
                wplywyNaOdcinkachText[pipeIndex].text = appLogic.doubleInFlowsOnPipes[pipeIndex][0].ToString();
            }
        }
    }

    //przypisywanie wartosci rozbiorow na odcinkach
    public void PrzypiszWartosciRozbiorow()
    {
        string node = "wezelek";
        for (int i = 0; i < appLogic.pipesRozbiory.Length; i++)
        {
            AssignChildTransform(i);
            rozbioryNaOdcinkachText[i] = dzieckoOne.GetComponent<TMP_Text>();
            rozbioryNaOdcinkachText[i].text = appLogic.pipesRozbiory[i].ToString();
            rozbioryNaOdcinkachText[i].color = Color.red;
        }
        for (int i = 0; i < appLogic.rozbioryNaWezlach.Length; i++)
        {
            AssignChildTransform(i, node);
            rozbioryNaWezlachText[i] = dzieckoWezel.GetComponent<TMP_Text>();
            rozbioryNaWezlachText[i].text = appLogic.rozbioryNaWezlach[i].ToString();
            rozbioryNaWezlachText[i].color = Color.red;
        }
    }

    //przypisywanie wartosci odplywow na odcinkach
    public void PrzypiszWartosciOdplywow(int i)
    {
        AssignChildTransform(i);
        if (appLogic.pipesOutFlows[i] != 0)
        {
            if (appLogic.kierunekPrzeplywu[i] == true)
            {
                odplywyNaOdcinkachText[i] = dzieckoDwa.GetComponent<TMP_Text>();
            }
            else if (appLogic.kierunekPrzeplywu[i] == false)
            {
                odplywyNaOdcinkachText[i] = dzieckoZero.GetComponent<TMP_Text>();
            }
            odplywyNaOdcinkachText[i].text = appLogic.pipesOutFlows[i].ToString();
            odplywyNaOdcinkachText[i].color = Color.green;
        }
        else if(appLogic.pipesOutFlows[i] == 0)
        {
            Debug.Log("niezaktualizowano odplywow.");
        }
    }


    public void Wezly()
    {
        updatedInfo.text = "rozbiory na wezlach: ";
        for (int node = 0; node < 8; node++)
        {
            updatedInfo.text = updatedInfo.text + $"\nRozbior na wezle {node}:  {appLogic.nodesOutFlow[node]}";
            //Debug.Log(updatedInfo.text);
        }
    }

}
