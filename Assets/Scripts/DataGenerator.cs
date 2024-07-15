using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;


public class DataGenerator : MonoBehaviour
{
    AppLogic appLogic;
    int nodeCount = 8;
    int baseMultiplier = 20;
    decimal totalRozbior;
    DataVersion generatedDataVersion;

    private void Start()
    {
        appLogic = FindObjectOfType<AppLogic>();
        totalRozbior = baseMultiplier * nodeCount + Random.Range(0, 100);
    }

    public void GenerateData()
    {
        GetGeneratedData();
    }
    
    DataVersion GetGeneratedData()
    {
        DataVersion dataVersion = new DataVersion();
        float randomValue = Mathf.PerlinNoise(0.1f, 0.9f);

        dataVersion.zasilanieZPompowni = baseMultiplier * Random.Range(40, 60)/100 * nodeCount + Random.Range(0, 100);
        dataVersion.zasilanieZeZbiornika = 0;
        dataVersion.wspolczynnik = 1 + Random.Range(0, 100)/100;

        int rozbiorPointsCount = dataVersion.nodesRozbiory.Length + dataVersion.pipesRozbiory.Length - 4;
        decimal[] rozbiorDistribution = GenerateRandomArray(rozbiorPointsCount, totalRozbior);
        for (int i = 1; i < dataVersion.nodesRozbiory.Length - 1; i++)
        {
            dataVersion.nodesRozbiory[i] = rozbiorDistribution[i - 1];
        }
        for (int i = 1; i < dataVersion.pipesRozbiory.Length - 1; i++)
        {
            dataVersion.pipesRozbiory[i] = rozbiorDistribution[i + 5];
        }

        for(int i = 0; i < dataVersion.polozenieWezlow.Length; i++)
        {
            dataVersion.polozenieWezlow[i] = Random.Range(0, 1000);
        }
        for (int i = 0; i < dataVersion.wysokoscZabudowy.Length; i++)
        {
            dataVersion.dlugoscOdcinka[i] = Random.Range(200, 900);
            dataVersion.wysokoscZabudowy[i] = 5 * Random.Range(1, 7);
        }

        dataVersion.nodesOutflows[0] = dataVersion.zasilanieZPompowni;
        dataVersion.nodesOutflows[7] = dataVersion.zasilanieZeZbiornika;

        appLogic.updateDataVersion.Invoke(dataVersion);
        return dataVersion;
    }

    public static decimal[] GenerateRandomArray(int length, decimal totalRozbior)
    {
        decimal[] array = new decimal[length];
        decimal remainingRozbior = totalRozbior;
        float randomnessParameter = (float)totalRozbior / 100;

        decimal divisionParameter = totalRozbior / length;

        for (int i = 0; i < length; i++)
        {
            if (i == length - 1)
            {
                array[i] = remainingRozbior;
                break;
            }
            decimal randomValue = (decimal)Random.Range(-1 * randomnessParameter, randomnessParameter);
            array[i] = divisionParameter + randomValue;
            remainingRozbior -= array[i];
        }
        return array;
    }
}
