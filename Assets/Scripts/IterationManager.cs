using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;


public class IterationManager : MonoBehaviour
{
    #region //startup

    public Action<List<RingData>, int, bool, int> updateIterationResultsData;
    Action changeDiameter;

    AppLogic appLogic;
    DataLoader dataLoader;
    RingData ringData;
    RingData.PipeCalculation pipeCalculation;
    DataLoader.CoefficientsData coefficientsData = new DataLoader.CoefficientsData();
    DataVersion dataVersion = new DataVersion();
    DataVersions dataVersions = new DataVersions();
    List<RingData> ringDatas = new List<RingData>();
    List<List<RingData>> ringDatasList = new List<List<RingData>>();
    List<bool> isDataCalculated = new List<bool>();

    int optimazationCounter = 0;
    int iterationCounter = 0;
    int maxIterationCount = 200;
    int pipesPerRing = 4;
    const decimal G = 9.80665m;
    decimal V = 1.0m;
    List<int> adjustedDiameterValuesCounterUp = Enumerable.Repeat(0, 9).ToList();
    List<int> adjustedDiameterValuesCounterDown = Enumerable.Repeat(0, 9).ToList();
    List<bool> unadjustableDiameterValuesMin = Enumerable.Repeat(false, 9).ToList(); 
    List<bool> unadjustableDiameterValuesMax = Enumerable.Repeat(false, 9).ToList();

    List<List<bool>> conditionsFirst;
    List<List<bool>> conditionsSecond;

    int pipeIndexInMuliltpleRings;
    bool areVelocitiesInMaxDataGreaterThanX = false;
    bool areVelocitiesInMinDataGreaterThanX = false;


    List<decimal> pipeDiameters;

    private void Awake()
    {
        dataLoader = FindObjectOfType<DataLoader>();
        appLogic = FindObjectOfType<AppLogic>();

        dataLoader.updateCoefficientData += OnCoefficientDataUpdate;
        appLogic.updateDataVersions += OnDataUpdates;
        appLogic.resetSimulation += Reset;

        isDataCalculated = Enumerable.Repeat(false, 3).ToList();

    }

    void OnCoefficientDataUpdate(DataLoader.CoefficientsData d)
    {
        coefficientsData = d;
    }
    void OnDataUpdates(DataVersions datas)
    {
        dataVersions = datas;
    }
    private void Reset()
    {
        adjustedDiameterValuesCounterUp = Enumerable.Repeat(0, 9).ToList();
        adjustedDiameterValuesCounterDown = Enumerable.Repeat(0, 9).ToList();
        unadjustableDiameterValuesMin = Enumerable.Repeat(false, 9).ToList();
        unadjustableDiameterValuesMax = Enumerable.Repeat(false, 9).ToList();

        int optimazationCounter = 0;
        iterationCounter = 0;
        ringDatasList.Clear();
        InitializeConditionLists();
        isDataCalculated = Enumerable.Repeat(false, 3).ToList();
    }
    public void InvokeUpdateIteration()
    {
        int iterationCount = 0;
        //int maxIterations = 100;

        InitializeConditionLists();
        PipeModellingHub(iterationCount);

        //PipeModellingHub(dataVersions.dataVersions[0], 0, iterationCount);//MAX
        //pipeDiameters = ringDatas[0].Pipes.Select(x => (decimal)x.roundedDiameter).ToList();
        ////pipeDiameters.AddRange(ringDatas[0].Pipes.Select(x => (float)x.roundedDiameter).ToList());
        //pipeDiameters.AddRange(ringDatas[1].Pipes.Select(x => (decimal)x.roundedDiameter).ToList());
        //PipeModellingHub(dataVersions.dataVersions[1], 1, iterationCount);//MIN
        //PipeModellingHub(dataVersions.dataVersions[2], 2, iterationCount);//MAXFIRE
    }
    void InitializeConditionLists()
    {
        conditionsFirst = new List<List<bool>>
        {
            Enumerable.Repeat(false, 9).ToList(),
            Enumerable.Repeat(false, 9).ToList(),
            Enumerable.Repeat(false, 9).ToList()
        };
        conditionsSecond = new List<List<bool>>
        {
            Enumerable.Repeat(false, 9).ToList(),
            Enumerable.Repeat(false, 9).ToList(),
            Enumerable.Repeat(false, 9).ToList()
        };
    }
    bool IsFirstAndSecondConditionMet(List<List<bool>> condtions)
    { 
        return condtions[0].All(c => c) && condtions[1].All(c => c);
        //return true if both conditions are met
    }
    bool AreVelocitiesGreaterThanX(int iterationsCount, int maxIterations)
    {
        if (iterationsCount > maxIterations)
            return true;

        if (conditionsFirst[0].Any(c => !c) || conditionsFirst[1].Any(c => !c) || conditionsSecond[0].Any(c => !c) || conditionsSecond[1].Any(c => !c))
            return false; //all other conditions must be met before checking velocity
        else
        {
            var falseIndexesList1 = conditionsFirst[2]
                .Select((value, index) => new { value, index })
                .Where(x => !x.value)
                .Select(x => x.index)
                .ToList();

            // Get indexes of false values in list2
            var falseIndexesList2 = conditionsSecond[2]
                .Select((value, index) => new { value, index })
                .Where(x => !x.value)
                .Select(x => x.index)
                .ToList();

            // Check if there are any common indexes with false values in both lists
            foreach (var index in falseIndexesList1)
            {
                if (falseIndexesList2.Contains(index))
                {
                    return false;
                }
            }
            Debug.Log ("Velocities are greater than 0.5m/s");
            return true;
        }
    }
    #endregion
    void PipeModellingHub(int iterationCount)//DataVersion data, int dataType, int iterationCount)
    {
        var ringDatas1 = CreateRingDatas();
        var ringDatas2 = CreateRingDatas();
        var ringDatas3 = CreateRingDatas();
        ringDatasList.Add(ringDatas1);
        ringDatasList.Add(ringDatas2);
        ringDatasList.Add(ringDatas3);
        pipeDiameters = new List<decimal>(9);
        for (int i = 0; i < 9; i++)
        {
            pipeDiameters.Add(0); // Add default value (e.g., 0) to each element
        }
        int ringCount = ringDatas1.Count;

        foreach (var data in dataVersions.dataVersions)
        {
            int dataType = Array.IndexOf(dataVersions.dataVersions, data);         
            for (int i = 0; i < ringCount; i++)
            {
                ringDatasList[dataType][i] = CalculateFirstIteration(ringDatasList[dataType][i], data, i);                 //no need to calculate pipes which are not part of the ring
            }
            SetDeltaDesignFlowForRing(ringDatasList[dataType], pipesPerRing);
            pipeIndexInMuliltpleRings = FindPipesInMultipleRings(ringDatasList[dataType]).index;                            //atm 2 rings == 1 pipe in common
            CalculateDeltaDesingFlowForPipesInMultipleRings(ringDatasList[dataType], pipeIndexInMuliltpleRings);
            CheckValues(ringDatasList[dataType], dataType);
            updateIterationResultsData?.Invoke(ringDatasList[dataType], dataType, true, iterationCount);
        }
        CalculateNextIterationUnlessConditionsAreMet(ringDatasList, pipeIndexInMuliltpleRings, iterationCount);
    }
    public void CalculateNextIterationUnlessConditionsAreMet(List<List<RingData>> ringDatasList, int pipeIndex, int iterationCount)
    {
        int ringCount = ringDatasList[0].Count;

        List<List<bool>>[] conditions = new List<List<bool>>[3];
        
        for (int i = 0; i < ringDatasList.Count; i++)
        {
            conditions[i] = CheckValues(ringDatasList[i], i);
        }

        while (IsNextIterrationNeccesary(ringDatasList, iterationCount))
        {
            if (iterationCount > maxIterationCount)
                break;

            DecideTypeOfCalculationDueToConditions(ringDatasList, conditions, ringCount, iterationCount);
            int dataType = 0;
            foreach (var ringDatas in ringDatasList)
            {
                SetDeltaDesignFlowForRing(ringDatas, pipesPerRing);
                CalculateDeltaDesingFlowForPipesInMultipleRings(ringDatas, pipeIndex); 
                CalculateIterationVelocity(ringDatas);

                for (int i = 0; i < ringDatasList.Count; i++)
                {
                    conditions[i] = CheckValues(ringDatasList[i], i);
                }
                updateIterationResultsData?.Invoke(ringDatas, dataType, false, iterationCount);
                dataType++;
            }
            iterationCount++;
        }
        Debug.Log("Iterations done on " + iterationCount + " iteration.");
    }
    void CalculateIterationVelocity(List<RingData> ringDatas)
    {
        foreach (var ringData in ringDatas)
        {
            foreach (var pipeFinalCalculation in ringData.Iterations.Last().pipeCalculations)
            {
                Pipe pipe = ringData.Pipes.Find(x => x.index == pipeFinalCalculation.Index);
                pipeFinalCalculation.finalVelocity = CalculateVelocityFlow(pipeFinalCalculation, pipe);
            }
        }
        //updateIterationResultsData?.Invoke(ringDatas);
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
    public RingData CalculateFirstIteration(RingData ringData, DataVersion data, int ringDataCounter)
    {
        List<RingData.PipeCalculation> pipeCalculations = new List<RingData.PipeCalculation>();
        RingData.IterationData iterationData;

        ringData.ringIndex = ringDataCounter;

        decimal[] kValues = new decimal[pipesPerRing];

        ringData.Pipes = new List<Pipe>();
        ringData.Nodes = new List<Node>();

        for (int i = 0; i < pipesPerRing; i++)
        {
            Pipe pipe = PopulatePipeVariables(ringData, data, kValues, i);
            //if (pipeDiameters != null)
            //    pipe.roundedDiameter = pipeDiameters[ringDataCounter * pipesPerRing + i];
            Node node = PopulateNodeVariables(ringData,data, i);
            if (pipeCalculations.Count < pipesPerRing)
                pipeCalculations.Add(CalculateIteration(pipe, ringData));
        }
        iterationData = new RingData.IterationData(pipeCalculations);
        ringData.Iterations = new List<RingData.IterationData>();
        ringData.Iterations.Add(iterationData);
        ringData.Iterations.Last().deltaDesignFlowForWholeRing = CalculateDeltaFlow(pipeCalculations);

        return ringData;
    }
    Pipe PopulatePipeVariables(RingData ringData, DataVersion dataVersion, decimal[] kValues, int i)
    {
        int dataType = Array.IndexOf(dataVersions.dataVersions, dataVersion);
        PipeKey key = ringData.PipesDictionary.ElementAt(i).Key;
        Pipe pipe = ringData.PipesDictionary.ElementAt(i).Value;

        pipe.flowDirection = dataVersion.flowDirection[pipe.index];
        pipe.inflow = dataVersion.pipesInflows[pipe.index];
        pipe.consumption = dataVersion.pipesConsumptions[pipe.index];
        pipe.outflow = dataVersion.pipesOutflows[pipe.index];
        pipe.length = dataVersion.pipesLength[pipe.index];
        pipe.velocity = 1.0m;
        pipe.designFlow = CalculateDesingFlow(pipe);

        if (dataType == 0)
        {
            pipe.diameter = CalculateDiameter(pipe);
            pipe.roundedDiameter = RoundingPipeDiameter(pipe);
            pipeDiameters[ringData.ringIndex * ringData.pipesPerRing + i] = pipe.roundedDiameter;
        }
        else
            pipe.roundedDiameter = pipeDiameters[ringData.ringIndex * ringData.pipesPerRing + i];

        //pipe.PipeRoundedDiameter = new PipeRoundedDiameter();
        //if (dataType == 0)
        //{
        //    pipe.diameter = CalculateDiameter(pipe);
        //    pipe.PipeRoundedDiameter.RoundedDiameter = RoundingPipeDiameter(pipe);
        //}
        //else
        //{

        //}
        pipe.velocity = CalculateVelocityFlow(pipe);
        pipe.cmValue = GetCMValueFromTable(pipe);
        pipe.kValue = CalculateKValue(pipe);
        kValues[i] = pipe.kValue;
        ringData.Pipes.Add(pipe);

        //ringData.SharedPipeDiameters.Add(pipe.PipeRoundedDiameter.RoundedDiameter);
        return pipe;
    }
    Node PopulateNodeVariables(RingData ringData, DataVersion data, int pipeInt)
    {
        int ringIndex = ringData.ringIndex;
        int i = ringData.pipesPerRing * ringIndex + pipeInt;
        Node node = data.Nodes[i];

        node.index = i;
        node.height = data.nodesHeight[i];
        node.consumption = data.nodesConsumptions[i];
        node.outflow = data.nodesOutflows[i];
        node.inflow = 0;
        ringData.Nodes.Add(node);

        return node;
    }
    RingData CalculateAdjustedFirstIteration(RingData ringData)
    {
        List<RingData.PipeCalculation> pipeCalculations = new List<RingData.PipeCalculation>();
        RingData.IterationData iterationData;
        for (int i = 0; i < pipesPerRing; i++)
        {
            Pipe pipe = ringData.Pipes[i];

            if (pipeCalculations.Count < pipesPerRing)
                pipeCalculations.Add(CalculateIteration(pipe, ringData));
        }
        iterationData = new RingData.IterationData(pipeCalculations);
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
            RingData.PipeCalculation pipeCalculation;
            pipeCalculation = ringData.Iterations.Last().pipeCalculations[i];
            newPipeCalculations.Add(CalculateIteration(pipeCalculation, ringData.Pipes[i]));         
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
    void DecideTypeOfCalculationDueToConditions(List<List<RingData>> ringDatasList, List<List<bool>>[] conditions, int ringCount, int iterationCount) //flag the change of diameter so it only happens once per itteration?
    {
        for (int i = 0; i < ringDatasList.Count; i++)
        {
            List<List<bool>> con = conditions[i];

            if (isDataCalculated[i] == true)
                continue;
            //if (IsTheSamePipeBeingAdjustedOverAndOverAgain(i))
            //{
            //    //while (AreVelocitiesGreaterThanX(iterationCount, maxIterationCount))
            //    ConditionsBasic(ringDatasList, ringCount);
            //    continue;
            //}


            if (iterationCounter == 5)
            {
                if (optimazationCounter == 10)
                {
                    iterationCounter = 0;
                    optimazationCounter = 0;
                    continue;
                }
                ConditionsBasic(ringDatasList, ringCount);
                optimazationCounter++;
                break;
            }
            iterationCounter++;
            if (con[0].Any(c => !c) || con[1].Any(c => !c))
            {
                {
                    IncreaseDiameter(ringDatasList, ringCount, i);
                    break;
                }
            }
            else if (con[0].All(c => c) && con[1].All(c => c) && con[2].Any(c => !c))
            {
                ConditionsAllGoodExceptVelocity(ringDatasList, ringCount, i);                               //decrese d
                isDataCalculated.ForEach(x => x = false);
                break;
            }
            else
                isDataCalculated[i] = true;
        }
    }
    bool DoSomething(List<List<bool>>[] conditions)
    {
        if (conditionsFirst[0].Any(c => !c) && conditionsFirst[1].Any(c => !c) && conditionsSecond[0].Any(c => !c) & conditionsSecond[1].Any(c => !c) )
        {
            if (conditionsFirst[2].Any(c => !c) && conditionsSecond[2].Any(c => !c))
                return true;
        }
        else
            return false;
        return true;
    }
    bool IsTheSamePipeBeingAdjustedOverAndOverAgain(int index)
    {
        for (int i = 0; i < adjustedDiameterValuesCounterDown.Count; i++)
        {
            if (adjustedDiameterValuesCounterDown[i] > 10)
            {
                if (adjustedDiameterValuesCounterUp[i] > 10)
                {
                    //unadjustableDiameterValuesMin[i] = true;
                    //unadjustableDiameterValuesMax[i] = true;
                    return true;
                }
            }
        }
        return false;
    }
    bool IncreaseDiameter(List<List<RingData>> ringDatasList, int ringCount, int dataType)
    {
        if (dataType == 2)
            return false;

        decimal maxValue = 0;
        int maxIndex = 0;
        decimal minValue= 0;
        int minIndex = 0;

        int tempIndex = 0;
        foreach (var ringDatas in ringDatasList)
        {
            for (int j = 0; j < ringCount; j++)
            {
                int iterationsCount = ringDatas[j].Iterations.Count();

                var pipeCalculations = ringDatas[j].Iterations.Last().pipeCalculations;

                if (pipeCalculations.Max(x => x.HeadLoss) > maxValue)
                {
                    maxValue = pipeCalculations.Max(x => x.HeadLoss);
                    tempIndex = pipeCalculations.FindIndex(x => x.HeadLoss == maxValue);
                    maxIndex = pipeCalculations[tempIndex].Index;
                }

                if (pipeCalculations.Min(x => x.HeadLoss) < minValue)
                {
                    minValue = pipeCalculations.Min(x => x.HeadLoss);
                    tempIndex = pipeCalculations.FindIndex(x => x.HeadLoss == minValue);
                    minIndex = pipeCalculations[tempIndex].Index;
                }
            }
        }

        if (Math.Abs(maxValue) > Math.Abs(minValue)) //increase
            AdjustPipeDiameterSmall(ringDatasList[dataType], maxIndex, adjustedDiameterValuesCounterDown, true);
        else //increase
            AdjustPipeDiameterSmall(ringDatasList[dataType], minIndex, adjustedDiameterValuesCounterUp, true);

        foreach (var ringDatas in ringDatasList)
        {
            for (int j = 0; j < ringCount; j++)
            {
                RingData tempRingData = CalculateAdjustedFirstIteration(ringDatas[j]);
                var iterations = ringDatas[j].Iterations;
                iterations.Add(tempRingData.Iterations.Last());//get values from first iteration
            }
        }
        return true;
    }
    bool ConditionsBasic(List<List<RingData>> ringDatasList, int ringCount)
    {
        foreach (var ringDatas in ringDatasList)
        {
            for (int j = 0; j < ringCount; j++)
            {
                ringDatas[j].Iterations.Add(CalculateNextIteration(ringDatas[j]));
            }
        }
        return true;
    }
    bool ConditionsWholeRingGoodPipesBad(List<List<RingData>> ringDatasList, int ringCount, int dataType)
    {
        if (dataType == 2)
            return false;

        AdjustPipeDiameter(ringDatasList[dataType], ringCount, true);
        foreach (var ringDatas in ringDatasList)
        {
            for (int j = 0; j < ringCount; j++)
            {
                RingData tempRingData = CalculateAdjustedFirstIteration(ringDatas[j]);
                ringDatas[j].Iterations.Add(tempRingData.Iterations.Last());//get values from first iteration
            }
        }
        return true;
    }
    bool ConditionsAllGoodExceptVelocity(List<List<RingData>> ringDatasList, int ringCount, int dataType)
    {
        if (dataType == 2)
            return false;

        AdjustPipeDiameter(ringDatasList[dataType], ringCount, false);
        foreach (var ringDatas in ringDatasList)
        {
            for (int j = 0; j < ringCount; j++)
            {
                RingData tempRingData = CalculateAdjustedFirstIteration(ringDatas[j]);
                ringDatas[j].Iterations.Add(tempRingData.Iterations.Last());//get values from first iteration
            }
        }
        return true;
    }
    void AdjustPipeDiameter(List<RingData> ringDatas, int ringCount, bool increase)
    {
        int indexOfmaxPipe = GetIndexOfPipeWithMaxVelocity(ringDatas);
        int indexOfminPipe = GetIndexOfPipeWithMinVelocity(ringDatas);
        if (increase == true)
            AdjustPipeDiameterSmall(ringDatas, indexOfmaxPipe, adjustedDiameterValuesCounterUp, increase);
        else
            AdjustPipeDiameterSmall(ringDatas, indexOfminPipe, adjustedDiameterValuesCounterDown, increase);
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
    void AdjustPipeDiameterSmall(List<RingData> ringDatas, int index, List<int>adjusted, bool increase)
    {
        // if its not possible to further adjust the diameter, get the next pipe outflowing from the same node and adjust its diameter( 1-2 -> 2-4)
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
      
                    decimal roundedDiameter = FindNextDiameter(coefficientsData.diameter, pipe.roundedDiameter, index, increase);
                    decimal velocity = CalculateVelocityFlow(pipe);

                    var pc = ringData.Iterations.Last().pipeCalculations.Find(x => x.Index == index);
                    
                    pc.Diameter = roundedDiameter;
                    pipe.roundedDiameter = roundedDiameter;
                    pipeDiameters[pipe.index] = roundedDiameter;

                    pc.finalVelocity = velocity;
                    pipe.velocity = velocity;

                    Debug.Log("adjusted pipe: " + pipe.index + " new diamater = " + pipe.roundedDiameter);
                }
            }
        }       
        
        foreach (var ringDatas1 in ringDatasList)
        {
            foreach (var ringData in ringDatas1)
            {
                foreach (var pipe in ringData.Pipes)
                {
                    if (pipe.index == index)
                    {
                        pipe.roundedDiameter = pipeDiameters[pipe.index];
                    }
                }
            }
        }
    }
    public List<List<bool>> CheckValues(List<RingData> ringDatas, int dataType)
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

            //headLossList.Clear();
            //velocityList.Clear();
        }
        List<List<bool>> conditions = new List<List<bool>> { sumOfHeadLossList, headLossList, velocityList };

        if (dataType == 0)
            conditionsFirst = conditions;
        else if (dataType == 1)
            conditionsSecond = conditions;

        return conditions;
    }
    bool IsNextIterrationNeccesary(List<List<RingData>> ringDatasList, int iterationCount)
    {
        List<bool> sumOfHeadlossList = new();
        List<bool> headlossList = new();
        List<bool> velocityListOne = new();
        List<bool> velocityListTwo = new();

        if (iterationCount > maxIterationCount)
            return false;

        int dataType = 0;
        foreach (var ringDatas in ringDatasList)
        {
            sumOfHeadlossList.AddRange(ConditionsForIteration(ringDatas)[0]);
            headlossList.AddRange(ConditionsForIteration(ringDatas)[1]);
          
            if (dataType == 0)
                velocityListOne.AddRange(ConditionsForIteration(ringDatas)[2]);
            else if (dataType == 1)
                velocityListTwo.AddRange(ConditionsForIteration(ringDatas)[2]);

            dataType++;
        }

        if (sumOfHeadlossList.Contains(false) || headlossList.Contains(false))
            return true;
        else
        {
            for (int i = 0; i < velocityListOne.Count; i++)
            {
                if (velocityListOne[i] == false && velocityListTwo[i] == false)
                    return true;
            }
            return false;
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

        if (pipe.consumption > pipe.inflow)
            tempOutflow = pipe.consumption - pipe.inflow;
        else
            tempOutflow = pipe.outflow;

        decimal designFlow = param * pipe.consumption + tempOutflow;
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
        decimal temp = ringDatas[1].Iterations.Last().pipeCalculations[0].DeltaDesignFlow;
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
    decimal CalculateQuotientOfHeadLossAndDesignFlow(RingData.PipeCalculation pipeCalculation) 
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
    public decimal FindNextDiameter(decimal[] array, decimal roundedD,  int index, bool increase)
    {
        decimal maxValue = array.Max();
        decimal minValue = array.Min();

        int inArrayIndex = Array.IndexOf(array, roundedD);

        if (increase == true)
        {
            if (array[inArrayIndex] == maxValue)
            {
                unadjustableDiameterValuesMax[index] = true;
                return roundedD;
            }
            adjustedDiameterValuesCounterUp[index] += 1;
            return array[inArrayIndex + 1];
        }
        else
        {
            if (array[inArrayIndex] == minValue)
            {
                unadjustableDiameterValuesMin[index] = true;
                return roundedD;
            }
            adjustedDiameterValuesCounterDown[index] += 1;
            return array[inArrayIndex - 1];
        }
    }

    #endregion iterations
    #region //helping and unspecified methods
    bool Direction(int pipeIndex, RingData ringData)
    {

        bool znak = false; // > 0
        if (pipeIndex == 4)
        {
            znak = true; // < 0
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
