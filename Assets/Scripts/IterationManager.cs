using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.AI;

public class IterationManager : MonoBehaviour
{
    public Action<List<RingData>> updateIterationResultsData;

    AppLogic appLogic;
    DataLoader dataLoader;
    RingData ringData;
    RingData.PipeCalculation pipeCalculation;
    //RingData.IterationData iterationData;
    DataLoader.CoefficientsData coefficientsData = new DataLoader.CoefficientsData();
    DataVersion dataVersion = new DataVersion();
    List<RingData> ringDatas = new List<RingData>();

    int pipesPerRing = 4;
    const decimal G = 9.80665m;
    //const decimal levelOfAccuracy = 0.5m;
    decimal V = 1.0m;

    private void Awake()
    {
        dataLoader = FindObjectOfType<DataLoader>();
        appLogic = FindObjectOfType<AppLogic>();

        dataLoader.updateCoefficientData += OnCoefficientDataUpdate;
        appLogic.updateDataVersion += OnDataUpdated;
    }

    void OnCoefficientDataUpdate(DataLoader.CoefficientsData d)
    {
        coefficientsData = d;
    }
    void OnDataUpdated(DataVersion d)
    {
        dataVersion = d;
    }

    public void InvokeUpdateIteration()
    {
        PipeModellingHub();
        //updateIterationResultsData.Invoke(ringDatas);
    }

    void PipeModellingHub()//later on acomodate more than 1 ring
    {
        int ringCount = 2;
        List<RingData> ringDatas = new List<RingData>();
        for (int i = 0; i < ringCount; i++)
        {
            ringDatas.Add(CalculateFirstIteration(i));
        }

        for (int i = 0; i < ringCount; i++)
        {
            for (int j = 0; j < pipesPerRing; j++)
            {
                ringDatas[i].Iterations.Last().pipeCalculations[j].DeltaDesignFlow = ringDatas[i].deltaDesignFlowForWholeRing;
            }
        }
        /*
        int iterationsCount = 0;
        while (IsNextIterrationNeccesary() == true)
        {
            for (int j = 0; j < ringCount; j++)
            {
                ringDatas[j].Iterations.Add(CalculateNextIteration());
            }
            iterationsCount++;
            if (iterationsCount > 100)
            {
                Debug.Log("TOO MANY ITERATIONS");
                break;
            }
        }*/
        //CalculateVelocityFlow();

        updateIterationResultsData?.Invoke(ringDatas);
    }

    Pipe TransferPipeDataToNewPipeObject(int pipeIndex)
    {
        Pipe pipe = new Pipe(V);

        pipe.index = pipeIndex;
        pipe.length = dataVersion.pipeLenght[pipeIndex];
        pipe.inflow = dataVersion.pipesInflows[pipeIndex];
        pipe.outflow = dataVersion.pipesOutflows[pipeIndex];
        pipe.rozbiory = dataVersion.pipesRozbiory[pipeIndex];

        return pipe;
    }

    RingData CalculateFirstIteration(int ringDataCounter)
    {
        ringData = new RingData(pipesPerRing);
        List<RingData.PipeCalculation> pipeCalculations = new List<RingData.PipeCalculation>();
        RingData.IterationData iterationData;

        decimal[] kValues = new decimal[pipesPerRing];

        for (int i = 0; i < pipesPerRing; i++)
        {
            Pipe pipe = TransferPipeDataToNewPipeObject(i + ringDataCounter * pipesPerRing);

            pipe.designFlow = CalculateDesingFlow(pipe);
            pipe.diameter = CalculateDiameter(pipe);
            pipe.roundedDiameter = RoundingPipeDiameter(pipe);
            pipe.velocity = CalculateVelocityFlow(pipe);
            pipe.cmValue = GetCMValueFromTable(pipe);
            pipe.kValue = CalculateKValue(pipe);
            kValues[i] = pipe.kValue;

            //ringData.Pipes.Add(new PipeKey("node1", "node2"), pipe);

            if (pipeCalculations.Count < pipesPerRing)
            {
                pipeCalculations.Add(CalculateIteration(pipe));
            }
        }

        ringData.deltaDesignFlowForWholeRing = CalculateDeltaFlow(pipeCalculations);


        iterationData = new RingData.IterationData(pipeCalculations);
        ringData.Iterations = new List<RingData.IterationData>();
        ringData.Iterations.Add(iterationData);

        return ringData;
    }

    List<RingData.PipeCalculation> CalculateDeltaFlowAfterIteration(List<RingData.PipeCalculation> pipeCalculations)
    {


        return pipeCalculations;
    }
    
    RingData.IterationData CalculateNextIteration()
    {
        List<RingData.PipeCalculation> pipeCalculations = new List<RingData.PipeCalculation>();
        for (int i = 0; i < pipesPerRing; i++)
        {
            RingData.PipeCalculation pipeCalculation = ringData.Iterations.Last().pipeCalculations[i];
            pipeCalculations.Add(CalculateIteration(pipeCalculation));
        }

        return new RingData.IterationData(pipeCalculations);
    }

    RingData.PipeCalculation CalculateIteration(Pipe pipe)
    {
        pipeCalculation = new RingData.PipeCalculation();
        pipeCalculation.KValue = pipe.kValue;
        pipeCalculation.DesignFlow = IncludeDirectionInDesignFlow(pipe);
        pipeCalculation.HeadLoss = CalculateHeadLoss(pipe);
        pipeCalculation.Quotient = CalculateQuotientOfHeadLossAndDesignFlow(pipeCalculation);
        //pipeCalculation.DeltaDesignFlow = CalculateDeltaFlow(pipeCalculation);

        return pipeCalculation;
    }
    RingData.PipeCalculation CalculateIteration(RingData.PipeCalculation pipeCalculation)
    {
        RingData.PipeCalculation nextPipeCalculation = new RingData.PipeCalculation();
        nextPipeCalculation.KValue = pipeCalculation.KValue;
        nextPipeCalculation.DesignFlow = IteratedDesignFlow(pipeCalculation);
        nextPipeCalculation.HeadLoss = CalculateHeadLoss(nextPipeCalculation);
        nextPipeCalculation.Quotient = CalculateQuotientOfHeadLossAndDesignFlow(nextPipeCalculation);
        //nextPipeCalculation.DeltaDesignFlow = CalculateDeltaFlow(nextPipeCalculation);

        return nextPipeCalculation;
    }

    bool IsNextIterrationNeccesary()
    {
        List<bool> sumOfHeadLossList = new List<bool>();
        List<bool> headLossList = new List<bool>();
        List<bool> velocityList = new List<bool>();

        foreach (var ringData in ringDatas)
        {
            sumOfHeadLossList.Add(IsSumOfHeadLossSmallerThanLevelOfAcurracy());

            foreach (var pipeCalculation in ringData.Iterations.Last().pipeCalculations)
            {
                headLossList.Add(IsHeadLossSmallerThanLevelOfAcurracy());
                velocityList.Add(IsVelocityGraterOrEqualThanLevelOfAcurracy());
            }
        }

        if (sumOfHeadLossList.Contains(false))
        {
            return true;
        }
        else
        {
            if (headLossList.Contains(false) || velocityList.Contains(false))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }

    //pipedatasetup
    decimal CalculateDesingFlow(Pipe pipe)
    {
        const decimal param = 0.55m;
        decimal designFlow = param * pipe.rozbiory + pipe.outflow;
        return designFlow;
    }

    decimal CalculateDiameter(Pipe pipe)
    {
        //dm3/s => m3/s
        decimal diameter = (decimal)Mathf.Sqrt((float)(4 * pipe.designFlow / 1000 / (decimal)Mathf.PI / pipe.velocity));
        return diameter;
    }

    decimal RoundingPipeDiameter(Pipe pipe)
    {
        decimal roundedDiameter = FindClosestValue(coefficientsData.diameter, pipe.diameter);
        return roundedDiameter;
    }

    decimal CalculateVelocityFlow(Pipe pipe)
    {
        //decimal velocity = 4 * pipe.DesignFlow / 1000 / (decimal)Mathf.PI / (decimal)Mathf.Pow((float)pipe.Diameter, 2) / 1000000;
        decimal velocity = (4 * pipe.designFlow / 1000) / (decimal)Mathf.PI / (decimal)Mathf.Pow((float)pipe.roundedDiameter, 2);

        //if (v >= 0.5m)
        return velocity;
    }
    decimal CalculateVelocityFlow(Pipe pipe, RingData.PipeCalculation pipeCalculation)
    {
        decimal velocity = 4 * pipeCalculation.DesignFlow / 1000 / (decimal)Mathf.PI / (decimal)Mathf.Pow((float)pipe.diameter, 2) / 1000000;
        return velocity;
    }

    decimal GetCMValueFromTable(Pipe pipe)
    {
        int index = Array.IndexOf(coefficientsData.diameter, pipe.roundedDiameter);
        decimal pipeCM = coefficientsData.opornoscC[index];
        return pipeCM;
    }

    decimal CalculateKValue(Pipe pipe)
    {
        decimal kValue = pipe.cmValue * pipe.length;
        return kValue;
    }

    //Iterations
    decimal IncludeDirectionInDesignFlow(Pipe pipe)
    {
        if (pipe.flowDirection != 1)
        {
            pipe.flowDirection = 1;
        }
        decimal designFlowWithDirection = pipe.designFlow * (decimal)pipe.flowDirection;
        return designFlowWithDirection;
    }
    decimal IteratedDesignFlow(RingData.PipeCalculation pipeCalculation)
    {
        decimal iteratedDesignFlow = pipeCalculation.DesignFlow + pipeCalculation.DeltaDesignFlow;
        return iteratedDesignFlow;
    }

    decimal CalculateHeadLoss(Pipe pipe)
    {
        //dm3/s => m
        decimal headLoss = pipe.kValue * (decimal)Mathf.Pow((float)pipe.designFlow, 2) / 1000000;
        return headLoss;
    }
    decimal CalculateHeadLoss(RingData.PipeCalculation pipeCalculation)
    {
        decimal headLoss = pipeCalculation.KValue * (decimal)Mathf.Pow((float)pipeCalculation.DesignFlow, 2) / 1000000;
        return headLoss;
    }

    decimal CalculateQuotientOfHeadLossAndDesignFlow(RingData.PipeCalculation pipeCalculation) //moze iteration data?
    {
        decimal quotient = pipeCalculation.HeadLoss / pipeCalculation.DesignFlow;
        return quotient;
    }

    decimal CalculateDeltaFlow(List<RingData.PipeCalculation> pipeCalculations)
    {
        //var pipeCalculations = ringData.Iterations.Last().pipeCalculations;
        decimal sumOfHeadLoss = pipeCalculations.Sum(pipeCalculations => pipeCalculations.HeadLoss);
        decimal sumOfQuotient = pipeCalculations.Sum(pipeCalculations => pipeCalculations.Quotient);

        decimal deltaFlow = -1 * sumOfHeadLoss / (2 * sumOfQuotient);
        return deltaFlow;
    }

    //conditions for next iteration
    bool IsSumOfHeadLossSmallerThanLevelOfAcurracy()
    {
        var pipeCalculations = ringData.Iterations.Last().pipeCalculations;
        decimal headLoss = pipeCalculations.Sum(pipeCalculations => pipeCalculations.HeadLoss);
        decimal levelOfAccuracy = 0.5m;

        if (headLoss <= (decimal)Mathf.Abs((float)levelOfAccuracy))
            return true;
        else
            return false;
    }
    bool IsHeadLossSmallerThanLevelOfAcurracy()
    {
        decimal levelOfAccuracy = 5m;

        if (pipeCalculation.HeadLoss < (decimal)Mathf.Abs((float)levelOfAccuracy))
            return true;
        else
            return false;
    }
    bool IsVelocityGraterOrEqualThanLevelOfAcurracy()
    {
        decimal levelOfAccuracy = 0.5m;

        if (pipeCalculation.finalVelocity >= (decimal)Mathf.Abs((float)levelOfAccuracy))
            return true;
        else
            return false;
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
