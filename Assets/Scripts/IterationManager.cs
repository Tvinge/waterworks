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
        List<RingData> ringDatas = CreateRingDatas();
        int ringCount = ringDatas.Count;
        int iterationsCount = 0;

        for (int i = 0; i < ringCount; i++)
        {
            ringDatas[i] = CalculateFirstIteration(ringDatas[i], i);                 //no need to calculate pipes which are not part of the ring
        }
        SetDeltaDesignFlowForRing(ringCount, pipesPerRing, ringDatas);
        Pipe pipe = FindPipesInMultipleRings(ringDatas);                            //atm 2 rings == 1 pipe in common
        CalculateDeltaDesingFlowForPipesInMultipleRings(ringDatas, pipe.index);
        updateIterationResultsData?.Invoke(ringDatas);

        while (IsNextIterrationNeccesary(ringDatas) == true)
        {
            for (int j = 0; j < ringCount; j++)
            {
                Debug.Log("DESIGNFLOW IN LAST ITERATION: " + ringDatas[0].Iterations.Last().pipeCalculations[0].DesignFlow);
                Debug.Log("DELTADESIGNFLOW IN LAST ITERATION: " + ringDatas[0].Iterations.Last().pipeCalculations[0].DeltaDesignFlow);

                ringDatas[j].Iterations.Add(CalculateNextIteration(ringDatas[j]));
            }
            SetDeltaDesignFlowForRing(ringCount, pipesPerRing, ringDatas);
            CalculateDeltaDesingFlowForPipesInMultipleRings(ringDatas, pipe.index);
            updateIterationResultsData?.Invoke(ringDatas);
            
            iterationsCount++;
            if (iterationsCount > 5)
            {
                Debug.Log("TOO MANY ITERATIONS");
                break;
            }
            Debug.Log("ITERATION: " + iterationsCount);

        }
        foreach (var ringData in ringDatas)
        {
            foreach (var pipeFinalCalculation in ringData.Iterations.Last().pipeCalculations)
            {/*
                foreach (var pipeKey in ringData.PipesDictionary.Keys)
                {
                    ringData.PipesDictionary.TryGetValue(pipeKey, out Pipe pipeI);
                    pipeFinalCalculation.finalVelocity = CalculateVelocityFlow(pipeFinalCalculation, pipeI);
                }*/
                
                foreach (var pipeI in ringData.Pipes)
                {
                    pipeFinalCalculation.finalVelocity = CalculateVelocityFlow(pipeFinalCalculation, pipeI);
                }
            }
        }

    }
        
    void SetDeltaDesignFlowForRing(int ringCount, int pipesPerRing, List<RingData> ringDatas)
    {
        for (int i = 0; i < ringCount; i++)
        {
            for (int j = 0; j < pipesPerRing; j++)
            {
                ringDatas[i].Iterations.Last().pipeCalculations[j].DeltaDesignFlow = ringDatas[i].Iterations.Last().deltaDesignFlowForWholeRing;
            }
        }
    }

    Pipe FindPipesInMultipleRings(List<RingData> ringDatas)
    {
        for (int i = 0; i < ringDatas.Count; i++)
        {
            foreach (var pipeKey in ringDatas[i].PipesDictionary.Keys)
            {
                for (int j = 0; j < ringDatas.Count; j++)
                {
                    if (i == j) continue; // Skip checking against itself

                    if (ringDatas[j].PipesDictionary.ContainsKey(pipeKey))
                    {
                        ringDatas[i].PipesDictionary.TryGetValue(pipeKey, out Pipe pipeI);
                        
                        return pipeI;
                    }
                }
            }
        }
        return null;
    }
    List<RingData> CreateRingDatas()
    {
        //Hardcoded values for now, implement graphsearching algorithm for waterwork with dynamic amount of pipes and nodes.

        List<RingData> ringDatas = new List<RingData>();
        
        RingData ringdata1 = new RingData(4);
        RingData ringdata2 = new RingData(4);

        ringdata1.PipesDictionary.Add(new PipeKey(1, 2), new Pipe(1));
        ringdata1.PipesDictionary.Add(new PipeKey(2, 4), new Pipe(2));
        ringdata1.PipesDictionary.Add(new PipeKey(4, 3), new Pipe(3));
        ringdata1.PipesDictionary.Add(new PipeKey(3, 1), new Pipe(4));

        ringdata2.PipesDictionary.Add(new PipeKey(4, 6), new Pipe(5));
        ringdata2.PipesDictionary.Add(new PipeKey(6, 5), new Pipe(6));
        ringdata2.PipesDictionary.Add(new PipeKey(5, 3), new Pipe(7));
        ringdata2.PipesDictionary.Add(new PipeKey(3, 4), new Pipe(3));

        ringDatas.Add(ringdata1);
        ringDatas.Add(ringdata2);

        return ringDatas;
    }
    List<RingData> CalculateDeltaDesingFlowForPipesInMultipleRings(List<RingData> ringDatas, int i)
    {
        var firstRing = ringDatas[0].Iterations.Last().pipeCalculations.Find(x => x.Index == i);
        var secondRing = ringDatas[1].Iterations.Last().pipeCalculations.Find(x => x.Index == i);

        decimal flow1 = firstRing.DeltaDesignFlow;
        decimal flow2 = secondRing.DeltaDesignFlow;

        firstRing.DeltaDesignFlow  = flow1 - flow2;
        secondRing.DeltaDesignFlow  = flow2 - flow1;

        return ringDatas;
    }
    RingData CalculateFirstIteration(RingData ringData, int ringDataCounter)
    {
        List<RingData.PipeCalculation> pipeCalculations = new List<RingData.PipeCalculation>();
        RingData.IterationData iterationData;

        decimal[] kValues = new decimal[pipesPerRing];
        ringData.Pipes = new List<Pipe>();
        for (int i = 0; i < pipesPerRing; i++)
        {
            int pipeIndex = ringData.PipesDictionary.ElementAt(i).Value.index;
            int pipeIndexForArrays = i + ringDataCounter * pipesPerRing;
            Pipe pipe = dataVersion.Pipes[pipeIndexForArrays];

            pipe.index = pipeIndex;
            pipe.flowDirection = dataVersion.kierunekPrzeplywu[pipeIndexForArrays];
            pipe.inflow = dataVersion.pipesInflows[pipeIndexForArrays];
            pipe.outflow = dataVersion.pipesOutflows[pipeIndexForArrays];
            pipe.velocity = 1.0m;

            pipe.designFlow = CalculateDesingFlow(pipe);
            pipe.diameter = CalculateDiameter(pipe);
            pipe.roundedDiameter = RoundingPipeDiameter(pipe);
            pipe.velocity = CalculateVelocityFlow(pipe);
            pipe.cmValue = GetCMValueFromTable(pipe);
            pipe.kValue = CalculateKValue(pipe);
            kValues[i] = pipe.kValue;

            if (pipeCalculations.Count < pipesPerRing)
            {
                pipeCalculations.Add(CalculateIteration(pipe));
            }
            ringData.Pipes.Add(pipe);
        }
        iterationData = new RingData.IterationData(pipeCalculations);
        ringData.Iterations = new List<RingData.IterationData>();
        ringData.Iterations.Add(iterationData);
        ringData.Iterations.Last().deltaDesignFlowForWholeRing = CalculateDeltaFlow(pipeCalculations);

        return ringData;
    }
    RingData.IterationData CalculateNextIteration(RingData ringData)
    {
        List<RingData.PipeCalculation> newPipeCalculations = new List<RingData.PipeCalculation>();
        RingData.IterationData iterationData;

        for (int i = 0; i < pipesPerRing; i++)
        {
            var pipeCalculation = ringData.Iterations.Last().pipeCalculations[i];
            newPipeCalculations.Add(CalculateIteration(pipeCalculation));
        }
        iterationData = new RingData.IterationData(newPipeCalculations);
        iterationData.deltaDesignFlowForWholeRing = CalculateDeltaFlow(newPipeCalculations);

        return iterationData;
    }

    RingData.PipeCalculation CalculateIteration(Pipe pipe)
    {
        pipeCalculation = new RingData.PipeCalculation();
        pipeCalculation.Index = pipe.index;
        pipeCalculation.KValue = pipe.kValue;
        pipeCalculation.DesignFlow = IncludeDirectionInDesignFlow(pipe);
        pipeCalculation.HeadLoss = CalculateHeadLoss(pipe);
        pipeCalculation.Quotient = CalculateQuotientOfHeadLossAndDesignFlow(pipeCalculation);
        return pipeCalculation;
    }
    RingData.PipeCalculation CalculateIteration(RingData.PipeCalculation pipeCalculation)
    {
        RingData.PipeCalculation nextPipeCalculation = new RingData.PipeCalculation();
        nextPipeCalculation.Index = pipeCalculation.Index;
        nextPipeCalculation.KValue = pipeCalculation.KValue;
        nextPipeCalculation.DesignFlow = IteratedDesignFlow(pipeCalculation);
        nextPipeCalculation.HeadLoss = CalculateHeadLoss(nextPipeCalculation);
        nextPipeCalculation.Quotient = CalculateQuotientOfHeadLossAndDesignFlow(nextPipeCalculation);
        return nextPipeCalculation;
    }

    bool IsNextIterrationNeccesary(List<RingData> ringDatas)
    {
        List<bool> sumOfHeadLossList = new List<bool>();
        List<bool> headLossList = new List<bool>();
        List<bool> velocityList = new List<bool>();

        foreach (var ringData in ringDatas)
        {
            var pipeCalculations = ringData.Iterations.Last().pipeCalculations;
            sumOfHeadLossList.Add(IsSumOfHeadLossSmallerThanLevelOfAcurracy(pipeCalculations));

            foreach (var pipeCalculation in pipeCalculations)
            {
                headLossList.Add(IsHeadLossSmallerThanLevelOfAcurracy(pipeCalculation));
                velocityList.Add(IsVelocityGraterOrEqualThanLevelOfAcurracy(pipeCalculation));
            }
        }

        if (sumOfHeadLossList.Contains(false)) 
        {
            return true; //next iteration is neccesary
        }
        else
        {
            if (headLossList.Contains(false) || velocityList.Contains(false))
            { 
                return true; //next iteration is neccesary
            }
            else
            {
                return false; //stop iterating
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
    decimal CalculateVelocityFlow(RingData.PipeCalculation pipeCalculation, Pipe pipe)
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
        int flowDirection = 0;

        if (pipe.flowDirection == true)
            flowDirection = 1;
        else
            flowDirection = -1;

        decimal designFlowWithDirection = pipe.designFlow * flowDirection;
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
    bool IsSumOfHeadLossSmallerThanLevelOfAcurracy(List<RingData.PipeCalculation> pipeCalculations)
    {
        decimal headLoss = pipeCalculations.Sum(pipeCalculations => pipeCalculations.HeadLoss);
        decimal levelOfAccuracy = 0.5m;

        if (headLoss <= (decimal)Mathf.Abs((float)levelOfAccuracy))
            return true;
        else
            return false;
    }
    bool IsHeadLossSmallerThanLevelOfAcurracy(RingData.PipeCalculation pipeCalculation)
    {
        decimal levelOfAccuracy = 5m;

        if (pipeCalculation.HeadLoss < (decimal)Mathf.Abs((float)levelOfAccuracy))
            return true;
        else
            return false;
    }
    bool IsVelocityGraterOrEqualThanLevelOfAcurracy(RingData.PipeCalculation pipeCalculation)
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
