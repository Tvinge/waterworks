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

        dataVersion.supplyFromPumpStation = baseMultiplier * Random.Range(40, 60)/100 * nodeCount + Random.Range(0, 100);
        dataVersion.supplyFromReservoir = 0;
        dataVersion.coefficient = 1 + Random.Range(0, 100)/100;

        int rozbiorPointsCount = dataVersion.nodesConsumptions.Length + dataVersion.pipesConsumptions.Length - 4;
        decimal[] consumptionDistibution = GenerateRandomArray(rozbiorPointsCount, totalRozbior);
        for (int i = 1; i < dataVersion.nodesConsumptions.Length - 1; i++)
        {
            dataVersion.nodesConsumptions[i] = consumptionDistibution[i - 1];
        }
        for (int i = 1; i < dataVersion.pipesConsumptions.Length - 1; i++)
        {
            dataVersion.pipesConsumptions[i] = consumptionDistibution[i + 5];
        }

        for(int i = 0; i < dataVersion.nodesHeight.Length; i++)
        {
            dataVersion.nodesHeight[i] = Random.Range(0, 1000);
        }
        for (int i = 0; i < dataVersion.buildingsHeight.Length; i++)
        {
            dataVersion.pipesLength[i] = Random.Range(200, 900);
            dataVersion.buildingsHeight[i] = 5 * Random.Range(1, 7);
        }

        dataVersion.nodesOutflows[0] = dataVersion.supplyFromPumpStation;
        dataVersion.nodesOutflows[7] = dataVersion.supplyFromReservoir;

        appLogic.ResetApp();
        appLogic.updateDataVersion.Invoke(dataVersion);

        return dataVersion;
    }

    public static decimal[] GenerateRandomArray(int length, decimal totalConsumption)
    {
        decimal[] array = new decimal[length];
        decimal remainingConsumption = totalConsumption;
        float randomnessParameter = (float)totalConsumption / 100;

        decimal divisionParameter = totalConsumption / length;

        for (int i = 0; i < length; i++)
        {
            if (i == length - 1)
            {
                array[i] = remainingConsumption;
                break;
            }
            decimal randomValue = (decimal)Random.Range(-1 * randomnessParameter, randomnessParameter);
            array[i] = divisionParameter + randomValue;
            remainingConsumption -= array[i];
        }
        return array;
    }
}
