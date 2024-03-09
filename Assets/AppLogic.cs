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

//separate logic from ui display
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
    int[] polozenieWezlow = {145, 147, 146, 151, 154, 159, 168, 192};


    //Wezel Output
    //Woda wplywajaca do wezla
    float[] wplywyNaWezlach = new float[8];

    //
    public float[] odplywyNaWezlach { get; private set; } = new float[8];


    //Odcinek Input
    //Maksymalne godzinowe rozbiory na odcinkach - Qhmax[dm^3/s] 
    public float[] rozbioryNaOdcinkach { get; private set; } = { 0f, 21f, 25f, 11f, 32f, 15f, 26f, 15f, 0f };

    //Dlugosci odcinkow [m]
    float[] dlugoscOdcinka = { 150f, 400f, 350f, 320f, 290f, 300f, 315f, 290f, 250f };

    //wysokosc zabudowy na dlugosci odcinka [m]
    float[] wysokoscZabudowy = { 0f, 20f, 25f, 15f, 20f, 15f, 15f, 15f, 0f };

    //przechowuje pozycje rur
    Vector3[] pipesPositions = Enumerable.Repeat(Vector3.zero, 9).ToArray();

    Vector3[] nodesPositions = Enumerable.Repeat(Vector3.zero, 8).ToArray();
    //Zasilanie z pompowni - Qp [dm3/s]
    float zasilanieZPompowni = 188;
        
    //Zasilanie ze zbiornika - Qz [dm^3/s]
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

    Collider2D[] kolizje;
    //Ilosc pierscieni
    int ringCount = 2;


    int k = 0;
    int l = 0;
    int m = 0;

    //Odcinek Output
    //Odplyw wody po odjeciu rozbiorow na odcinkach - Qhmax[dm^3/s] 
    public float[] odplywyNaOdcinkach { get; private set; } = new float[9];
    public float[] wplywyNaOdcinkach { get; private set; } = new float[9];

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

 /// <summary>
 /// max value w searchfornextnode... nie jest aktualizowana po obliczeniu nastepnych odcinkow
 /// zachowac wlasciwosc funkcji, tak by nie resetowala sie tablica/lista na kazdym jej wywolaniu
 /// </summary>


    void Start()
    {
        
        //starting preparations, setting values etc
        ObliczQzbiornika();
        //Debug.Log("Qhmax wynosi = " + maxQh);
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
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (m < 9)
            {
                SearchForNextNodeIndexAndCalculateIt();
                m += 1;
                //Debug.Log(m);
            }

        }
    }

    //pozwala wziac do operacji najwysza wartosc z wplywy na odcinkach a nastepnie ja usuwa
    void SearchForNextNodeIndexAndCalculateIt()
    {
        // ZnajdŸ indeksy dla ka¿dej wartoœci w tablicy
        var indexesAndValues = wplywyNaOdcinkach.Select((value, index) => new IndexedValue { Index =  index, Value = value });

        // Posortuj indeksy i wartoœci wed³ug wartoœci w malej¹cej kolejnoœci
        sortedIndexesAndValues = indexesAndValues.OrderByDescending(item => item.Value).ToList();

        foreach (var indexToRemove in indexesToRemove)
        {
            sortedIndexesAndValues.RemoveAll(item => item.Index == indexToRemove);
        }

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

        if (sortedIndexesAndValues.Any())
        {
            var maxElement = sortedIndexesAndValues.First();
            int maxNodeIndex = maxElement.Index;
            float maxValue = maxElement.Value;

            indexesToRemove.Add(maxNodeIndex);

            Debug.Log($"Wplyw na odcinku: {maxValue}");
            //miejsce na operacje do wykonania
            RozplywWody(maxNodeIndex);
        }



    }
    void RozplywWody(int pipeIndex)
    {
        List<int> adjacentNodes = _pipesAdjacentNodes[pipeIndex];
        if (odplywyNaWezlach[adjacentNodes[0]] == 0 || odplywyNaWezlach[adjacentNodes[1]] == 0)
        {
            ObliczOdplywNaOdcinku(pipeIndex);
            Debug.Log($"odplyw na odcinku {pipeIndex} {odplywyNaOdcinkach[pipeIndex]}");

            if (odplywyNaWezlach[adjacentNodes[0]] > odplywyNaWezlach[adjacentNodes[1]])
            {
                //do gory i w prawo przeplyw -> true 
                KierunekPrzeplywu(pipeIndex, true);
                nadpiszWartosciWplywow.Invoke(pipeIndex);
                nadpiszWartosciOdplywow.Invoke(pipeIndex);
                ObliczOdplywNaWezle(adjacentNodes[1]);
                ObliczWplywNaOdcinkachObokWezla(adjacentNodes[1]);
            }
            else
            {
                //w dol i w lewo przeplyw -> false
                KierunekPrzeplywu(pipeIndex, false);
                nadpiszWartosciWplywow.Invoke(pipeIndex);
                nadpiszWartosciOdplywow.Invoke(pipeIndex);
                ObliczOdplywNaWezle(adjacentNodes[0]);
                ObliczWplywNaOdcinkachObokWezla(adjacentNodes[0]);
            }
        }
        //gdy w z obu wezlow wplywa woda do tego samego odcinka
        else if (odplywyNaWezlach[adjacentNodes[0]] != 0 && odplywyNaWezlach[adjacentNodes[1]] != 0)
        {
            if (odplywyNaWezlach[adjacentNodes[0]] + odplywyNaWezlach[adjacentNodes[1]] == rozbioryNaOdcinkach[pipeIndex])
            {
                //good
            }
            else
            {

            }
        }
        else
        {
            Debug.Log("kys");
        }


    }

    //zrobic to z try-catch?
    void ObliczOdplywNaWezle(int nodeIndex)
    {
        //tworzy liste z indexami pobliskich rur
        List<int> adjacentPipes = _nodeAndAdjacentPipes[nodeIndex];
        List<int> emptyPipeIndexes = new List<int>();


        for (int i = 0; i < adjacentPipes.Count; i++)
        {
            int secondNodeIndex = -1;

            if (adjacentPipes[i] == 0)
            {
                secondNodeIndex = ReturnAdjacentNode(adjacentPipes[i], nodeIndex);
            }


            if (secondNodeIndex == -1)
            {
                Debug.Log($"ObliczOdplywNaWezle nie znalazl sasiedniego wezla dla rury {adjacentPipes[i]}");
            }
            else
            {
                //odplywyNaWezlach[secondNodeIndex]
                //gdy wyplywa woda z wezla
                if (wplywyNaOdcinkach[adjacentPipes[i]] != 0)
                {
                    //wplywyNaOdcinkach[adjacentPipes[i]] =
                }
                //gdy nie starczylo wody w wezle na rozbior
                else
                {

                }
            }
        } 
        odplywyNaWezlach[nodeIndex] = wplywyNaWezlach[nodeIndex] - rozbioryNaWezlach[nodeIndex];
        updateOdplywyZWezlow.Invoke(nodeIndex);
    }




    void ObliczWplywNaOdcinkachObokWezla(int nodeIndex)
    {
        //tworzy liste z indexami pobliskich rur
        List<int> adjacentPipes = _nodeAndAdjacentPipes[nodeIndex];
        List<int> emptyPipeIndexes = new List<int>();

        for (int i = 0; i < adjacentPipes.Count; i++)
        {
            //sprawdza skad plynie woda
            if (odplywyNaOdcinkach[adjacentPipes[i]] <= 0)
            {
                emptyPipeIndexes.Add(adjacentPipes[i]);
            }
        }

        //oblicza wplyw na odcinku
        for (int i = 0; i < emptyPipeIndexes.Count; i++)
        {
            // if()
            wplywyNaOdcinkach[emptyPipeIndexes[i]] = odplywyNaWezlach[nodeIndex] / emptyPipeIndexes.Count;
        }
    }
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


        odplywyNaWezlach[p] = zasilanieZPompowni;
        wplywyNaOdcinkach[p] = odplywyNaWezlach[p];
        KierunekPrzeplywu(p, true);
        nadpiszWartosciWplywow.Invoke(p);

        odplywyNaWezlach[zNode] = zasilanieZeZbiornika;
        wplywyNaOdcinkach[zPipe] = odplywyNaWezlach[zNode];
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

    void ObliczOdplywNaOdcinku(int pipeIndex)
    {
        odplywyNaOdcinkach[pipeIndex] = wplywyNaOdcinkach[pipeIndex] - rozbioryNaOdcinkach[pipeIndex];
    }

    void KierunekPrzeplywu(int i, bool kierunekPrzeplyw)
    {
        kierunekPrzeplywu[i] = kierunekPrzeplyw;
    }

    //ten override chyba useless
    void KierunekPrzeplywu(int i, bool kierunekPrzeplyw, bool kierunekZegar)
    {
        kierunekPrzeplywu[i] = kierunekPrzeplyw;
        kierunekRuchuWskazowekZegara[i] = kierunekZegar;
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
    //Returns adjacent node, avoids nodeIndex argument
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


}