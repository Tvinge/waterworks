using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IterationManager : MonoBehaviour
{
    DataLoader dataLoader = DataLoader.Instance;
    RingData ringData;
    RingData.IterationData iterationData;
    DataLoader.CoefficientsData coefficientsData = new DataLoader.CoefficientsData();

    List<RingData> ringDatas = new List<RingData>();

    int pipesPerRing = 4;
    const decimal Pi = (decimal)Mathf.PI;
    const decimal G = 9.80665m;
    const decimal levelOfAccuracy = 0.5m;
    decimal V = 1.0m;

    private void Awake()
    {
        dataLoader.updateCoefficientData += OnCoefficientDataUpdate;
    }
    void OnCoefficientDataUpdate(DataLoader.CoefficientsData d)
    {
        coefficientsData = d;
    }

    void PipeModellingHub()//later on acomodate more than 1 ring
    {
        ringData = new RingData(pipesPerRing);
        ringData.iterations = new List<RingData.IterationData>();

        CalculateDesingFlow();
        CalculateDiameter(ringData.pipeDesignFlow, ringData.pipeVelocity);
        RoundingPipeDiameter();
        CheckVelocityFlow(ringData.pipeDesignFlow, ringData.roundedPipeDiameters);
        GetCMValueFromTable();
        CalculateKValue(ringData.pipeCM, ringData.pipeLengths);

        //warunek iteracji dla wszystkich pierscieni!
        {
            iterationData = new RingData.IterationData(pipesPerRing);
            IncludeDirectionInDesignFlow();
            CalculateHeadLoses();
            CalculateQuotientOfHeadLosesAndDesignFlow(iterationData.headLoses, iterationData.designFlow);
            CalculateDeltaFlow();
            ringData.iterations.Add(iterationData);
        }

        ringDatas.Add(ringData);
    }

    decimal[] CalculateDesingFlow()
    {
        const decimal param = 0.55m;
        for (int i = 0; i < pipesPerRing; i++)
        {
            ringData.pipeDesignFlow[i] = param * ringData.pipeRozbiory[i] + ringData.pipeOutflows[i];
        }
        return ringData.pipeDesignFlow;
    }

    decimal[] CalculateDiameter(decimal[] flow, decimal[] velocity)
    {
        //dm3/s => m3/s
        for (int i = 0; i < pipesPerRing; i++)
        {
            ringData.pipeDiameters[i] = (decimal)Mathf.Sqrt((float)(4 * flow[i] / 1000 / Pi / velocity[i]));
        }
        return ringData.pipeDiameters;
    }

    decimal[] RoundingPipeDiameter()
    {
        for (int i = 0; i < pipesPerRing; i++)
        {
            ringData.roundedPipeDiameters[i] = FindClosestValue(coefficientsData.diameter, ringData.pipeDiameters[i]);
        }
        return ringData.roundedPipeDiameters;
    }

    decimal[] CheckVelocityFlow(decimal[] flow, decimal[] diameter)
    {
        for (int i = 0; i < pipesPerRing; i++)
        {
            ringData.pipeVelocity[i] = 4 * flow[i] / 1000 / Pi / (decimal)Mathf.Pow((float)diameter[i], 2) / 1000000;
        }
        //if (v >= 0.5m)
        return ringData.pipeVelocity;
    }

    decimal[] GetCMValueFromTable()
    {
        int[] indexes = new int[4];
        for (int i = 0; i < pipesPerRing; i++)
        {
            indexes[i] = Array.IndexOf(coefficientsData.diameter, ringData.roundedPipeDiameters[i]);
            ringData.pipeCM[i] = coefficientsData.opornoscC[indexes[i]];
        }
        return ringData.pipeCM;
    }

    decimal[] CalculateKValue(decimal[] Cm, decimal[] length)
    {
        for (int i = 0; i < pipesPerRing; i++)
        {
            ringData.pipeK[i] = Cm[i] * length[i];
        }
        return ringData.pipeK;
    }

    decimal[] IncludeDirectionInDesignFlow()
    {
        for (int i = 0; i < pipesPerRing; i++)
        {
            iterationData.designFlow[i] = ringData.pipeDesignFlow[i] * (decimal)ringData.pipeFlowDirection[i];
        }
        return iterationData.designFlow;
    }

    decimal[] CalculateHeadLoses()
    {
        for (int i = 0; i < pipesPerRing; i++)
        {
            iterationData.headLoses[i] = ringData.pipeK[i] * (decimal)Mathf.Pow((float)ringData.pipeDesignFlow[i], 2);
        }
        return iterationData.headLoses;
    }

    decimal[] CalculateQuotientOfHeadLosesAndDesignFlow(decimal[] headLoses, decimal[] designFlow)
    {
        for (int i = 0; i < pipesPerRing; i++)
        {
            iterationData.quotient[i] = headLoses[i] / designFlow[i];
        }
        return iterationData.quotient;
    }

    decimal CalculateDeltaFlow()
    {
        decimal sumOfHeadLoses = 0;
        decimal sumOfQuotient = 0;

        for (int i = 0; i < pipesPerRing; i++)
        {
            sumOfHeadLoses += iterationData.headLoses[i];
            sumOfQuotient += iterationData.quotient[i];
        }

        decimal deltaFlow = -1 * sumOfHeadLoses / (2 * sumOfQuotient);
        return deltaFlow;
    }

    public int FindClosestIndex(decimal[] array, decimal x)
    {
        int closestIndex = 0;
        decimal closestDifference = Math.Abs(array[0] - x);

        for (int i = 1; i < array.Length; i++)
        {
            decimal difference = Math.Abs(array[i] - x);
            if (difference < closestDifference)
            {
                closestDifference = difference;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    public static decimal FindClosestValue(decimal[] array, decimal x)
    {
        decimal closestValue = array[0];
        decimal smallestDifference = Math.Abs(x - closestValue);

        for (int i = 1; i < array.Length; i++)
        {
            decimal difference = Math.Abs(x - array[i]);
            if (difference < smallestDifference)
            {
                smallestDifference = difference;
                closestValue = array[i];
            }
        }
        return closestValue;
    }


}
