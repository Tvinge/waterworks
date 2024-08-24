using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class IterationManager : MonoBehaviour
{
    #region //startup

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
    List<int> adjustedDiameterValuesCounterUp = Enumerable.Repeat(0, 9).ToList();
    List<int> adjustedDiameterValuesCounterDown = Enumerable.Repeat(0, 9).ToList();
    List<bool> unadjustableDiameterValuesMin = Enumerable.Repeat(false, 9).ToList(); 
    List<bool> unadjustableDiameterValuesMax = Enumerable.Repeat(false, 9).ToList(); 




    private void Awake()
    {
        dataLoader = FindObjectOfType<DataLoader>();
        appLogic = FindObjectOfType<AppLogic>();

        dataLoader.updateCoefficientData += OnCoefficientDataUpdate;
        appLogic.updateDataVersion += OnDataUpdated;
        appLogic.resetSimulation += Reset;
    }

    void OnCoefficientDataUpdate(DataLoader.CoefficientsData d)
    {
        coefficientsData = d;
    }
    void OnDataUpdated(DataVersion d)
    {
        dataVersion = d;
    }
    private void Reset()
    {
        adjustedDiameterValuesCounterUp = Enumerable.Repeat(0, 9).ToList();
        adjustedDiameterValuesCounterDown = Enumerable.Repeat(0, 9).ToList();
        unadjustableDiameterValuesMin = Enumerable.Repeat(false, 9).ToList();
        unadjustableDiameterValuesMax = Enumerable.Repeat(false, 9).ToList();
    }
    public void InvokeUpdateIteration()
    {
        PipeModellingHub();
        //updateIterationResultsData.Invoke(ringDatas);
    }
    #endregion




    void PipeModellingHub()//later on acomodate more than 1 ring
    {
        List<RingData> ringDatas = CreateRingDatas();
        int ringCount = ringDatas.Count;
        for (int i = 0; i < ringCount; i++)
        {
            ringDatas[i] = CalculateFirstIteration(ringDatas[i], i);                 //no need to calculate pipes which are not part of the ring
        }
        SetDeltaDesignFlowForRing(ringDatas, pipesPerRing);
        Pipe pipe = FindPipesInMultipleRings(ringDatas);                            //atm 2 rings == 1 pipe in common
        CalculateDeltaDesingFlowForPipesInMultipleRings(ringDatas, pipe.index);
        CheckValues(ringDatas);
        updateIterationResultsData?.Invoke(ringDatas);
        CalculateNextIterationUnlessConditionsAreMet(ringDatas, pipe.index);

        
    }

    public void CalculateNextIterationUnlessConditionsAreMet(List<RingData> ringDatas, int pipeIndex)
    {
        int iterationsCount = 1;
        int countOfadjustments = 0;
        int ringCount = ringDatas.Count;
        List<List<bool>> conditions;
        bool znak = true;
        conditions = CheckValues(ringDatas);

        while (IsNextIterrationNeccesary(ringDatas) == true)
        {
            if (iterationsCount > 20)
                break;
            else if (conditions[0].Any(c => !c))
            {
                znak = false;
                for (int j = 0; j < ringCount; j++)
                {
                    ringDatas[j].Iterations.Add(CalculateNextIteration(ringDatas[j], znak));
                }
            }
            //checks if all elements are true, and if any elements are false
            else if (conditions[0].All(c => c) && conditions[1].Any(c => !c))
            {
                //increase d
                AdjustPipeDiameter(ringDatas, ringCount, true);
                //iterationsCount = 0;
                for (int j = 0; j < ringCount; j++)
                {
                    RingData tempRingData = CalculateAdjustedFirstIteration(ringDatas[j]);
                    ringDatas[j].Iterations.Add(tempRingData.Iterations.Last());//get values from first iteration
                }
            }
            else if (conditions[0].All(c => c) && conditions[1].All(c => c) && conditions[2].Any(c => !c))
            {
                //decrese d
                AdjustPipeDiameter(ringDatas, ringCount, false);
                //iterationsCount = 0;
                for (int j = 0; j < ringCount; j++)
                {
                    RingData tempRingData = CalculateAdjustedFirstIteration(ringDatas[j]);
                    ringDatas[j].Iterations.Add(tempRingData.Iterations.Last());//get values from first iteration
                }
            }
            else
            {
                Debug.LogWarning("Check conditions for iterations, iteration: " + iterationsCount);
            }


            SetDeltaDesignFlowForRing(ringDatas, pipesPerRing);
            CalculateDeltaDesingFlowForPipesInMultipleRings(ringDatas, pipeIndex);
            iterationsCount++;
            Debug.Log("ITERATION: " + iterationsCount);

            CalculateIterationVelocity(ringDatas);
            conditions = CheckValues(ringDatas);

            updateIterationResultsData?.Invoke(ringDatas);
        }
        Debug.Log("skibidi");
    }
    void CalculateIterationVelocity(List<RingData> ringDatas)
    {
        foreach (var ringData in ringDatas)
        {
            foreach (var pipeFinalCalculation in ringData.Iterations.Last().pipeCalculations)
            {
                //var pipeKey = ringData.PipesDictionary.ElementAt(pipeFinalCalculation.Index).Key;
                //ringData.PipesDictionary.TryGetValue(pipeKey, out Pipe pipeI);
                Pipe pipe = ringData.Pipes.Find(x => x.index == pipeFinalCalculation.Index);
                pipeFinalCalculation.finalVelocity = CalculateVelocityFlow(pipeFinalCalculation, pipe);
            }
            
        }
        //updateIterationResultsData?.Invoke(ringDatas);
    }
    public Pipe DeepCopyPipe(Pipe originalPipe)
    {
        Pipe pipe = originalPipe.DeepCopy(originalPipe);
        return pipe;
    }
    #region //meta iteration stuff
    public List<RingData> CreateRingDatas()
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

        ringdata1.PipesInMultipleRings.Add(new PipeKey(3, 4), true);
        ringdata2.PipesInMultipleRings.Add(new PipeKey(3, 4), true);

        ringDatas.Add(ringdata1);
        ringDatas.Add(ringdata2);

        return ringDatas;
    }

    public RingData CalculateFirstIteration(RingData ringData, int ringDataCounter)
    {
        List<RingData.PipeCalculation> pipeCalculations = new List<RingData.PipeCalculation>();
        RingData.IterationData iterationData;

        ringData.ringIndex = ringDataCounter;

        decimal[] kValues = new decimal[pipesPerRing];
        ringData.Pipes = new List<Pipe>();
        for (int i = 0; i < pipesPerRing; i++)
        {
            //int pipeIndex = ringData.PipesDictionary.ElementAt(i).Value.index;
            PipeKey key = ringData.PipesDictionary.ElementAt(i).Key;
            Pipe pipe = ringData.PipesDictionary.ElementAt(i).Value;

            pipe.flowDirection = dataVersion.flowDirection[pipe.index];
            pipe.inflow = dataVersion.pipesInflows[pipe.index];
            pipe.rozbiory = dataVersion.pipesRozbiory[pipe.index];
            pipe.outflow = dataVersion.pipesOutflows[pipe.index];
            pipe.length = dataVersion.pipesLength[pipe.index];
            pipe.velocity = 1.0m;

            pipe.designFlow = CalculateDesingFlow(pipe);
            pipe.diameter = CalculateDiameter(pipe);
            pipe.roundedDiameter = RoundingPipeDiameter(pipe);
            pipe.velocity = CalculateVelocityFlow(pipe);
            pipe.cmValue = GetCMValueFromTable(pipe);
            pipe.kValue = CalculateKValue(pipe);
            kValues[i] = pipe.kValue;
            ringData.Pipes.Add(pipe);

            if (pipeCalculations.Count < pipesPerRing)
            {
                pipeCalculations.Add(CalculateIteration(pipe, ringData));
            }
            
        }
        iterationData = new RingData.IterationData(pipeCalculations);
        ringData.Iterations = new List<RingData.IterationData>();
        ringData.Iterations.Add(iterationData);
        ringData.Iterations.Last().deltaDesignFlowForWholeRing = CalculateDeltaFlow(pipeCalculations);

        return ringData;
    }

    RingData CalculateAdjustedFirstIteration(RingData ringData)
    {
        List<RingData.PipeCalculation> pipeCalculations = new List<RingData.PipeCalculation>();
        RingData.IterationData iterationData;
        for (int i = 0; i < pipesPerRing; i++)
        {
            //Pipe pipe = ringData.PipesDictionary.ElementAt(i).Value;
            Pipe pipe = ringData.Pipes[i];

            if (pipeCalculations.Count < pipesPerRing)
            {
                pipeCalculations.Add(CalculateIteration(pipe, ringData));
            }

        }
        iterationData = new RingData.IterationData(pipeCalculations);
        ringData.Iterations.Add(iterationData);
        ringData.Iterations.Last().deltaDesignFlowForWholeRing = CalculateDeltaFlow(pipeCalculations);

        return ringData;
    }

    RingData.IterationData CalculateNextIteration(RingData ringData, bool znak )
    {
        List<RingData.PipeCalculation> newPipeCalculations = new List<RingData.PipeCalculation>();
        RingData.IterationData iterationData;

        for (int i = 0; i < pipesPerRing; i++)
        {
            RingData.PipeCalculation pipeCalculation;
            if (znak == false)
            {
                pipeCalculation = ringData.Iterations.Last().pipeCalculations[i];
                newPipeCalculations.Add(CalculateIteration(pipeCalculation, ringData.Pipes[i]));
            }
            else
            {
                RingData ringDataTemp = CalculateFirstIteration(ringData, ringData.ringIndex);
                newPipeCalculations.Add(ringDataTemp.Iterations.First().pipeCalculations[i]);
            }            
        }
        iterationData = new RingData.IterationData(newPipeCalculations);
        iterationData.deltaDesignFlowForWholeRing = CalculateDeltaFlow(newPipeCalculations);

        return iterationData;
    }

    RingData.PipeCalculation CalculateIteration(Pipe pipe, RingData ringData)//for first iteration
    {
        pipeCalculation = new RingData.PipeCalculation();
        pipeCalculation.Index = pipe.index;

        //checks if diameter was adjusted
        if (ringData.Iterations == null || pipe.roundedDiameter == ringData.Iterations.First().pipeCalculations.Find(x => x.Index == pipe.index).Diameter) 
        {
            pipeCalculation.KValue = pipe.kValue;
            pipeCalculation.Diameter = pipe.roundedDiameter;
        }
        else
        {
            pipe.cmValue = GetCMValueFromTable(pipe);
            pipe.kValue = CalculateKValue(pipe);
            pipeCalculation.Diameter = pipe.roundedDiameter;
            pipeCalculation.KValue = pipe.kValue;

        }        
        pipeCalculation.DesignFlow = IncludeDirectionInDesignFlow(pipe, ringData);
        pipeCalculation.HeadLoss = CalculateHeadLoss(pipe);
        pipeCalculation.Quotient = CalculateQuotientOfHeadLossAndDesignFlow(pipeCalculation);
        return pipeCalculation;
    }
    RingData.PipeCalculation CalculateIteration(RingData.PipeCalculation pipeCalculation, Pipe pipe) //for other iterations
    {
        RingData.PipeCalculation nextPipeCalculation = new RingData.PipeCalculation();
        nextPipeCalculation.Index = pipeCalculation.Index;
        nextPipeCalculation.Diameter = pipeCalculation.Diameter;
        nextPipeCalculation.KValue = pipe.kValue;
        nextPipeCalculation.DesignFlow = IteratedDesignFlow(pipeCalculation);
        nextPipeCalculation.HeadLoss = CalculateHeadLoss(nextPipeCalculation, pipe.flowDirection);
        nextPipeCalculation.Quotient = CalculateQuotientOfHeadLossAndDesignFlow(nextPipeCalculation);
        return nextPipeCalculation;
    }

    #endregion
    #region //iteration conditions etc
    void AdjustPipeDiameter(List<RingData> ringDatas, int ringCount, bool increase)
    {
        int indexOfmaxPipe = GetIndexOfPipeWithMaxVelocity(ringDatas);
        int indexOfminPipe = GetIndexOfPipeWithMinVelocity(ringDatas);
        if (increase == true)
            AdjustPipeDiameterSmall(ringDatas, indexOfmaxPipe,adjustedDiameterValuesCounterUp, increase);
        else
            AdjustPipeDiameterSmall(ringDatas, indexOfminPipe,adjustedDiameterValuesCounterDown, increase);


    }
    void IsAdjustable (List<RingData> ringDatas, int index, bool increase)
    {
        foreach (var ringData in ringDatas)
        {
            foreach (var pipe in ringData.Pipes)
            {
                int closestIndex = FindClosestIndex(coefficientsData.diameter, pipe.diameter);

                if (coefficientsData.diameter[closestIndex] == coefficientsData.diameter.Max() && increase == true)
                {
                    unadjustableDiameterValuesMin[index] = true;
                }
                    
                if (coefficientsData.diameter[closestIndex] == coefficientsData.diameter.Min() && increase == false)
                {
                    unadjustableDiameterValuesMax[index] = true;
                }
            }
        }
    }
    int GetIndexOfPipeWithMaxVelocity(List<RingData> ringDatas)
    {
        decimal maximalVelocity = 0;
        int indexOfmaxPipe = -1;
        foreach (var ringData in ringDatas)
        {
            foreach (var pipe in ringData.Iterations.Last().pipeCalculations)
            {
                if (pipe.finalVelocity > maximalVelocity)
                {
                    maximalVelocity = pipe.finalVelocity;
                    indexOfmaxPipe = pipe.Index;
                }
            }
        }
        return indexOfmaxPipe;
    }
    int GetIndexOfPipeWithMinVelocity(List<RingData> ringDatas)
    {
        decimal minimalVelocity = 10000000;
        int indexOfminPipe = -1;
        foreach (var ringData in ringDatas)
        {
            foreach (var pipe in ringData.Iterations.Last().pipeCalculations)
            {
                if (pipe.finalVelocity < minimalVelocity)
                {
                    minimalVelocity = pipe.finalVelocity;
                    indexOfminPipe = pipe.Index;
                }
            }
        }
        return indexOfminPipe;
    }
    void AdjustPipeDiameterSmall(List<RingData> ringDatas, int index,List<int>adjusted, bool increase)
    {
        bool changeCounter = true;
        foreach (var ringData in ringDatas)
        {
            foreach (var pipe in ringData.Pipes)
            {
                if (pipe.index == index)
                {
                    if (unadjustableDiameterValuesMax[index] == true && increase == true)
                        break;
                    if (unadjustableDiameterValuesMin[index] == true && increase == false)
                        break;
      
                    decimal roundedDiameter = FindNextDiameter(coefficientsData.diameter, pipe.roundedDiameter, adjusted, index, increase);
                    decimal velocity = CalculateVelocityFlow(pipe);

                    ringData.Iterations.Last().pipeCalculations[index].Diameter = roundedDiameter;
                    pipe.roundedDiameter = roundedDiameter;

                    ringData.Iterations.Last().pipeCalculations[index].finalVelocity = velocity;
                    pipe.velocity = velocity;

                    Debug.Log("adjusted pipe: " + pipe.index + " new diamater = " + pipe.roundedDiameter);
                }
            }
        }
        //if (increase == true && changeCounter == true)
        //    adjustedDiameterValuesCounterUp[index] += 1;
        //else if (increase == false && changeCounter == true)
        //    adjustedDiameterValuesCounterDown[index] += 1;
    }
    public List<List<bool>> CheckValues(List<RingData> ringDatas)
    {
        List<bool> sumOfHeadLossList = new List<bool>();//2 entries, each for one ring
        List<bool> headLossList = new List<bool>();//all pipes from both rings in one list
        List<bool> velocityList = new List<bool>();

        foreach (var ringData in ringDatas)
        {
            var pipeCalculations = ringData.Iterations.Last().pipeCalculations;
            sumOfHeadLossList.Add(IsSumOfHeadLossSmallerThanLevelOfAcurracy(pipeCalculations));
            ringData.Iterations.Last().sumOfHeadlossBool = sumOfHeadLossList.Last(); //adds bool to current ringData 

            foreach (var pipeCalculation in pipeCalculations)
            {
                headLossList.Add(IsHeadLossSmallerThanLevelOfAcurracy(pipeCalculation));
                velocityList.Add(IsVelocityGraterOrEqualThanLevelOfAcurracy(pipeCalculation));
            }

            ringData.Iterations.Last().sumOfHeadloss = pipeCalculations.Sum(pipeCalculations => pipeCalculations.HeadLoss);
            ringData.Iterations.Last().headlossList = headLossList;
            ringData.Iterations.Last().velocityList = velocityList;
        }
        return new List<List<bool>> { sumOfHeadLossList, headLossList, velocityList };
    }
    bool IsNextIterrationNeccesary(List<RingData> ringDatas)
    {
        List<bool> sumOfHeadlossList = ConditionsForIteration(ringDatas)[0];
        List<bool> headlossList = ConditionsForIteration(ringDatas)[1];
        List<bool> velocityList = ConditionsForIteration(ringDatas)[2];

        if (sumOfHeadlossList.Contains(false) || headlossList.Contains(false) || velocityList.Contains(false))
        {
            return true; //next iteration is neccesary
        }
        else
        {
            return false; //stop iterating
        }
    }
    bool IsSumOfHeadLossSmallerThanLevelOfAcurracy(List<RingData.PipeCalculation> pipeCalculations)
    {
        decimal headLossSum = pipeCalculations.Sum(pipeCalculations => pipeCalculations.HeadLoss);
        decimal levelOfAccuracy = 0.5m;

        if ((decimal)Mathf.Abs((float)headLossSum) <= levelOfAccuracy)
            return true;//good case
        else
            return false;//more calculation
    }
    bool IsHeadLossSmallerThanLevelOfAcurracy(RingData.PipeCalculation pipeCalculation)
    {
        decimal levelOfAccuracy = 5m;

        if ((decimal)Mathf.Abs((float)pipeCalculation.HeadLoss) < levelOfAccuracy)
            return true;
        else
            return false;
    }
    bool IsVelocityGraterOrEqualThanLevelOfAcurracy(RingData.PipeCalculation pipeCalculation)
    {
        decimal levelOfAccuracy = 0.5m;

        if ((decimal)Mathf.Abs((float)pipeCalculation.finalVelocity) >= levelOfAccuracy)
            return true;
        else
            return false;
    }
    #endregion
    #region //basic minor calculations
    decimal CalculateDesingFlow(Pipe pipe)
    {
        const decimal param = 0.55m;
        decimal tempOutflow = 0;

        if (pipe.rozbiory > pipe.inflow)
            tempOutflow = pipe.rozbiory - pipe.inflow;
        else
            tempOutflow = pipe.outflow;

        decimal designFlow = param * pipe.rozbiory + tempOutflow;
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
        int flowDirection = 0;
        if (pipe.flowDirection == true)
            flowDirection = -1;
        else
            flowDirection = 1;

        decimal velocity = 4 * pipeCalculation.DesignFlow / 1000 / (decimal)Mathf.PI / (decimal)Mathf.Pow((float)pipe.roundedDiameter, 2) * flowDirection;
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
    #endregion
    #region//Iteration minor calculations
    public Pipe FindPipesInMultipleRings(List<RingData> ringDatas)
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
    public List<RingData> CalculateDeltaDesingFlowForPipesInMultipleRings(List<RingData> ringDatas, int i)
    {
        var firstRing = ringDatas[0].Iterations.Last().pipeCalculations.Find(x => x.Index == i);
        var secondRing = ringDatas[1].Iterations.Last().pipeCalculations.Find(x => x.Index == i);

        decimal flow1 = firstRing.DeltaDesignFlow;
        decimal flow2 = secondRing.DeltaDesignFlow;

        firstRing.DeltaDesignFlow = flow1 - flow2;
        secondRing.DeltaDesignFlow = flow2 - flow1;

        return ringDatas;
    }
    public void SetDeltaDesignFlowForRing(List<RingData> ringDatas, int pipesPerRing)
    {
        int ringCount = ringDatas.Count;
        for (int i = 0; i < ringCount; i++)
        {
            for (int j = 0; j < pipesPerRing; j++)
            {
                ringDatas[i].Iterations.Last().pipeCalculations[j].DeltaDesignFlow = ringDatas[i].Iterations.Last().deltaDesignFlowForWholeRing;
            }
        }
    }
    decimal IncludeDirectionInDesignFlow(Pipe pipe, RingData ringData)
    {
        int flowDirection = 0;
        bool znak = Direction(pipe.index, ringData);
        if (znak == false)
        {
            flowDirection = 1;
        }
        else
        {
            flowDirection = -1;
        }
        pipe.flowDirection = znak;

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
        int flowDirection = 0;
        if (pipe.flowDirection == false)
        {
            flowDirection = 1;
        }
        else
        {
            flowDirection = -1;
        }
        //int flowDirection = GetNumberSign(pipe.designFlow);
        decimal headLoss = pipe.kValue * (decimal)Mathf.Pow((float)pipe.designFlow, 2) / 1000000 * flowDirection;
        return headLoss;
    }
    int GetNumberSign(decimal number)
    {
        if (number > 0)
            return 1;
        else if (number < 0)
            return -1;
        else
            return 0;
    }
    decimal CalculateHeadLoss(RingData.PipeCalculation pipeCalculation, bool flowDirection)
    {
        int flowDirectionInt = GetNumberSign(pipeCalculation.DesignFlow);
        decimal headLoss = pipeCalculation.KValue * (decimal)Mathf.Pow((float)pipeCalculation.DesignFlow, 2) / 1000000 * flowDirectionInt;
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
    public decimal FindNextDiameter(decimal[] array, decimal roundedD, List<int> adjustedList, int index, bool increase)
    {
        decimal closestValue = array[0];
        decimal smallestDifference = Math.Abs(roundedD - closestValue);
        decimal maxValue = array.Max();
        decimal minValue = array.Min();
        int timesOfAdjustingThisValue = adjustedList[index];

        int inArrayIndex = Array.IndexOf(array, roundedD);

        if (increase == true)
            return array[inArrayIndex + 1];
        else
            return array[inArrayIndex - 1];



        //if (increase == true)
        //{
        //    for (int i = 1; i < array.Length; i++)
        //    {
        //        decimal difference = Math.Abs(roundedD - array[i]);
        //        if (difference < smallestDifference)
        //        {
        //            smallestDifference = difference;
        //            if (array[i + timesOfAdjustingThisValue] == maxValue)
        //            {
        //                unadjustableDiameterValuesMax[index] = true;
        //            }
        //            if (difference == 0)
        //                closestValue = array[i + timesOfAdjustingThisValue + 1];
        //            else
        //                closestValue = array[i + timesOfAdjustingThisValue];
        //        }
        //    }
        //    if (closestValue == array.Last())
        //        unadjustableDiameterValuesMin[index] = true;

        //    adjustedDiameterValuesCounterUp = adjustedList;
        //    adjustedDiameterValuesCounterUp[index]++;
        //}
        //else
        //{
        //    for (int i = array.Length - 1; i>= 0; i--)
        //    {
        //        decimal difference = Math.Abs(roundedD - array[i]);
        //        if (difference < smallestDifference)
        //        {
        //            smallestDifference = difference;
        //            if (array[i - timesOfAdjustingThisValue] == minValue)
        //            {
        //                unadjustableDiameterValuesMin[index] = true;
        //            }
        //            if (difference == 0)
        //                closestValue = array[i - timesOfAdjustingThisValue - 1];
        //            else
        //                closestValue = array[i - timesOfAdjustingThisValue];
        //        }
        //    }
        //    if (closestValue == array[0])
        //        unadjustableDiameterValuesMin[index] = true;

        //    adjustedDiameterValuesCounterDown = adjustedList;
        //    adjustedDiameterValuesCounterDown[index]++;
        //}

        //if (closestValue == roundedD)
        //{
        //    if (increase == true)
        //    {
        //        adjustedDiameterValuesCounterUp = adjustedList;
        //        adjustedDiameterValuesCounterUp[index]++;
        //        closestValue = FindClosestValue(array, roundedD, adjustedDiameterValuesCounterUp, index, increase);
        //    }
        //    else
        //    {

        //        adjustedDiameterValuesCounterDown = adjustedList;
        //        adjustedDiameterValuesCounterDown[index]++;
        //        closestValue = FindClosestValue(array, roundedD, adjustedDiameterValuesCounterDown, index, increase);
        //    }
        //}


        //if (closestValue == array[0])
        //{
        //    closestValue = array[timesOfAdjustingThisValue];
        //}

        return closestValue;
    }

    #endregion iterations
    #region //helping and unspecified methods
    bool Direction(int pipeIndex, RingData ringData)
    {

        bool znak = false; // dodatnie
        if (pipeIndex == 4)
        {
            znak = true; //ujemny
        }
        if (pipeIndex == 5)
        {
            znak = true;
        }
        if (pipeIndex == 7)
        {
            znak = true;
        }

        if (ringData.ringIndex == 0 & pipeIndex == 3)
        {
            znak = false;
        }
        if (ringData.ringIndex == 1 & pipeIndex == 3)
        {
            znak = true;
        }

        var pipe = ringData.Pipes.Find(p => p.index == pipeIndex);
        if (pipe != null)
        {
            pipe.flowDirection = znak;
        }

        return znak;
    }

    List<List<bool>> ConditionsForIteration(List<RingData> ringDatas)
    {
        List<bool> sumOfHeadlossList = new List<bool>();
        List<bool> headlossList = new List<bool>();
        List<bool> velocityList = new List<bool>();

        foreach (var ringData in ringDatas)
        {
            sumOfHeadlossList.Add(ringData.Iterations.Last().sumOfHeadlossBool);
            headlossList.AddRange(ringData.Iterations.Last().headlossList);
            velocityList.AddRange(ringData.Iterations.Last().velocityList);
        }

        List<List<bool>> conditions = new List<List<bool>>();
        conditions.Add(sumOfHeadlossList);
        conditions.Add(headlossList);
        conditions.Add(velocityList);

        return conditions;
    }
    #endregion


}
