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

    //wplywy 
    [SerializeField] TMP_Text[] wplywyNaOdcinkachText;
    [SerializeField] TMP_Text[] wplywyNaWezlachText;

    //odplyw
    [SerializeField] TMP_Text[] odplywyNaOdcinkachText;
    [SerializeField] TMP_Text[] odplywyNaWezlachText;

    [SerializeField] TMP_Text updatedInfo;
    void Start()
    {
    }
    private void Update()
    {
    }

    //przypisywanie wartosci wplywow na odcinkach
    public void PrzypiszWartosciWplywow(int i)
    {
        Debug.Log(appLogic.kierunekPrzeplywu[i]);
        if (appLogic.kierunekPrzeplywu[i] == true)
        {
            wplywyNaOdcinkachText[i] = GetComponent<Transform>().GetChild(1).GetChild(0).GetChild(i).GetChild(0).GetChild(0).GetChild(0).GetComponent<TMP_Text>();
            wplywyNaOdcinkachText[i].text = appLogic.wplywyNaOdcinkach[i].ToString();
        }
        else if (appLogic.kierunekPrzeplywu[i] == false)
        {
            wplywyNaOdcinkachText[i] = GetComponent<Transform>().GetChild(1).GetChild(0).GetChild(i).GetChild(0).GetChild(2).GetChild(0).GetComponent<TMP_Text>();
            wplywyNaOdcinkachText[i].text = appLogic.wplywyNaOdcinkach[i].ToString();
        }
    }

    //przypisywanie wartosci rozbiorow na odcinkach
    public void PrzypiszWartosciRozbiorow()
    {
        for (int i = 0; i < appLogic.rozbioryNaOdcinkach.Length; i++)
        {
            rozbioryNaOdcinkachText[i] = GetComponent<Transform>().GetChild(1).GetChild(0)?.GetChild(i)?.GetChild(0)?.GetChild(1)?.GetChild(0)?.GetComponent<TMP_Text>();
            rozbioryNaOdcinkachText[i].text = appLogic.rozbioryNaOdcinkach[i].ToString();
            rozbioryNaOdcinkachText[i].color = Color.red;
        }
        for (int i = 0; i < appLogic.rozbioryNaWezlach.Length; i++)
        {
            rozbioryNaWezlachText[i] = GetComponent<Transform>().GetChild(1).GetChild(1).GetChild(i).GetChild(2).GetChild(0).GetComponent<TMP_Text>();
            rozbioryNaWezlachText[i].text = appLogic.rozbioryNaWezlach[i].ToString();
            rozbioryNaOdcinkachText[i].color = Color.red;
        }
    }

    //przypisywanie wartosci odplywow na odcinkach
    public void PrzypiszWartosciOdplywow(int i)
    {
        if (appLogic.kierunekPrzeplywu[i] == true)
        {
            odplywyNaOdcinkachText[i] = wodociag.GetComponent<Transform>().GetChild(0).GetChild(i).GetChild(0).GetChild(2).GetChild(0).GetComponent<TMP_Text>();
            odplywyNaOdcinkachText[i].text = appLogic.odplywyNaOdcinkach[i].ToString();
        }
        else if (appLogic.kierunekPrzeplywu[i] == false)
        {
            odplywyNaOdcinkachText[i] = wodociag.GetComponent<Transform>().GetChild(0).GetChild(i).GetChild(0).GetChild(0).GetChild(0).GetComponent<TMP_Text>();
            odplywyNaOdcinkachText[i].text = appLogic.odplywyNaOdcinkach[i].ToString();
        }
    }

    public void Wezly()
    {
        updatedInfo.text = "rozbiory na wezlach: ";
        for (int node = 0; node < 8; node++)
        {
            updatedInfo.text = updatedInfo.text + $"\nRozbior na wezle {node}:  {appLogic.odplywyNaWezlach[node]}";
            //Debug.Log(updatedInfo.text);
        }
    }
}
