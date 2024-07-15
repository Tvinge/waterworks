using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DataLoader
{
    // Static variable that holds the single instance of the class
    private static DataLoader instance;
    // Private constructor prevents instantiation from other classes
    private DataLoader() { }

    public static DataLoader Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new DataLoader();
            }
            return instance;
        }
    }

    public Action<CoefficientsData> updateCoefficientData;

    public int setsOfData = 16;
    int nodeColumns = 4;
    int pipeColumns = 7;
    int nodeCount = 8;
    int pipeCount = 9;
    int lambdaRows = 24;
    int lambdaColumns = 11;
    int cmRows = 25;
    int cmColumns = 10;

    [System.Serializable]
    public class CoefficientsData
    {
        public decimal[] diameter;
        public decimal[] velocity;

        [Header("Lambda")]
        public decimal[] m1;
        public decimal[] lambda;
        public decimal[] m2;

        [Header("Cm")]
        public decimal[] opornoscC;
        public decimal[] przeplywnoscM;
    }
    [System.Serializable]
    public class CoefficientsList
    {
        public CoefficientsData[] coefficients;
    }

    public CoefficientsList coefficientsList = new CoefficientsList();

    [System.Serializable]
    public class DataSet
    {

        public int dataset;
        public decimal zasilanieZPompowni;
        public decimal wspolczynnik;

        [Header("Node")]
        public int[] nodeID;
        public decimal[] nodeRozbiory;
        public decimal[] nodeHeight;

        [Header("Pipe")]
        public int[] pipeID;
        public decimal[] pipeRozbiory;
        public decimal[] pipeLength;
        public int[] pipeHeight;
    }




    [System.Serializable]
    public class DataSetList
    {
        public DataSet[] dataSet;
    }

    public DataSetList myDataSetList = new DataSetList();

    public void Start()
    {
        myDataSetList.dataSet = new DataSet[setsOfData];
        for (int i = 0; i < setsOfData; i++)
        {
            myDataSetList.dataSet[i] = new DataSet();
        }

        coefficientsList.coefficients = new CoefficientsData[4];
        for (int i = 0; i < 4; i++)
        {
            coefficientsList.coefficients[i] = new CoefficientsData();
        }

        ReadPipeData();
        ReadNodeData();
        ReadLambdaCoefficient();
        ReadCmCoefficient();

        updateCoefficientData?.Invoke(coefficientsList.coefficients[0]);
    }



    public DataVersion ConvertDatasetToDataVersion(int DataVersionIndex)
    {
        DataVersion dataVersion = new DataVersion();
        DataSet dataSet = new DataSet();
        dataSet = myDataSetList.dataSet[DataVersionIndex];
        
        dataVersion.zasilanieZPompowni = dataSet.zasilanieZPompowni;
        dataVersion.wspolczynnik = dataSet.wspolczynnik;
        dataVersion.nodesRozbiory = dataSet.nodeRozbiory;
        dataVersion.pipesRozbiory = dataSet.pipeRozbiory;
        //Debug.Log($" pipeV: {dataVersion.pipesRozbiory} node: {dataVersion.nodesRozbiory}");
        //Debug.Log($" pipeS: {dataSet.pipeRozbiory} node: {dataSet.nodeRozbiory}");
        return dataVersion;
    }



    public DataSet[] ReadPipeData()
    {
        TextAsset fileContents = Resources.Load<TextAsset>("pipeData");
        string[] data = fileContents.text.Split(new string[] { ";", "\n" }, StringSplitOptions.None);

        int rows = data.Length / pipeColumns - 1;

        var m = myDataSetList.dataSet;

        for (int i = 0; i < rows; i += pipeCount)
        {
            if (i == 0)
            {
                m[i].dataset = int.Parse(data[pipeColumns * (i + 1)]);
                m[i].zasilanieZPompowni = decimal.Parse(data[pipeColumns * (i + 1) + 5]);
                m[i].wspolczynnik = decimal.Parse(data[pipeColumns * (i + 1) + 6]);

                m[i].pipeID = new int[pipeCount];
                m[i].pipeRozbiory = new decimal[pipeCount];
                m[i].pipeLength = new decimal[pipeCount];
                m[i].pipeHeight = new int[pipeCount];

                for (int j = 0; j < pipeCount; j++)
                {
                    m[i].pipeID[j] = int.Parse(data[pipeColumns * (i + j + 1) + 1]);
                    m[i].pipeRozbiory[j] = decimal.Parse(data[pipeColumns * (i + j + 1) + 2]);
                    m[i].pipeLength[j] = decimal.Parse(data[pipeColumns * (i + j + 1) + 3]);
                    m[i].pipeHeight[j] = int.Parse(data[pipeColumns * (i + j + 1) + 4]);
                }
            }
            else
            {
                m[i / pipeCount] = new DataSet();
                m[i / pipeCount].dataset = int.Parse(data[pipeColumns * (i + 1)]);
                m[i / pipeCount].zasilanieZPompowni = decimal.Parse(data[pipeColumns * (i + 1) + 5]);
                m[i / pipeCount].wspolczynnik = decimal.Parse(data[pipeColumns * (i + 1) + 6]);

                m[i / pipeCount].pipeID = new int[pipeCount];
                m[i / pipeCount].pipeRozbiory = new decimal[pipeCount];
                m[i / pipeCount].pipeLength = new decimal[pipeCount];
                m[i / pipeCount].pipeHeight = new int[pipeCount];

                for (int j = 0; j < pipeCount; j++)
                {
                    m[i / pipeCount].pipeID[j] = int.Parse(data[pipeColumns * (i + j + 1) + 1]);
                    m[i / pipeCount].pipeRozbiory[j] = decimal.Parse(data[pipeColumns * (i + j + 1) + 2]);
                    m[i / pipeCount].pipeLength[j] = decimal.Parse(data[pipeColumns * (i + j + 1) + 3]);
                    m[i / pipeCount].pipeHeight[j] = int.Parse(data[pipeColumns * (i + j + 1) + 4]);
                }
            }
        }
        return m;
    }

    public DataSet[] ReadNodeData()
    {
        TextAsset fileContents = Resources.Load<TextAsset>("nodeData");
        string[] data = fileContents.text.Split(new string[] { ";", "\n" }, StringSplitOptions.None);

        int rows = data.Length / nodeColumns - 1;

        var m = myDataSetList.dataSet;

        for (int i = 0; i < rows; i += nodeCount)
        {
            if (i == 0)
            {
                m[i].dataset = int.Parse(data[nodeColumns * (i + 1)]);

                m[i].nodeID = new int[nodeCount];
                m[i].nodeRozbiory = new decimal[nodeCount];
                m[i].nodeHeight = new decimal[nodeCount];

                for (int j = 0; j < nodeCount; j++)
                {
                    m[i].nodeID[j] = int.Parse(data[nodeColumns * (i + j + 1) + 1]);
                    m[i].nodeRozbiory[j] = decimal.Parse(data[nodeColumns * (i + j + 1) + 2]);
                    m[i].nodeHeight[j] = decimal.Parse(data[nodeColumns * (i + j + 1) + 3]);
                }
            }
            else
            {
                m[i / nodeCount].dataset = int.Parse(data[nodeColumns * (i + 1)]);

                m[i / nodeCount].nodeID = new int[nodeCount];
                m[i / nodeCount].nodeRozbiory = new decimal[nodeCount];
                m[i / nodeCount].nodeHeight = new decimal[nodeCount];

                for (int j = 0; j < nodeCount; j++)
                {
                    m[i / nodeCount].nodeID[j] = int.Parse(data[nodeColumns * (i + j + 1) + 1]);
                    m[i / nodeCount].nodeRozbiory[j] = decimal.Parse(data[nodeColumns * (i + j + 1) + 2]);
                    m[i / nodeCount].nodeHeight[j] = decimal.Parse(data[nodeColumns * (i + j + 1) + 3]);
                }
            }
        }
        return m;
    }
    public CoefficientsData[] ReadLambdaCoefficient()
    {
        TextAsset fileContents = Resources.Load<TextAsset>("lambdaCoefficient");

        string[] data = fileContents.text.Split(new string[] { ";", "\n" }, StringSplitOptions.None);

        var m = coefficientsList.coefficients;

        int o = lambdaRows - 2;
        m[0].diameter = new decimal[o];
        m[0].velocity = new decimal[o];
        m[0].m1 = new decimal[o];
        m[0].lambda = new decimal[o];
        m[0].m2 = new decimal[o];

        for (int i = 2; i < lambdaRows; i++)
        {
            m[0].diameter[i - 2] = decimal.Parse(data[lambdaColumns * i]);
            m[0].velocity[i - 2] = decimal.Parse(data[(lambdaColumns * i) + 1]);
            m[0].m1[i - 2] = decimal.Parse(data[lambdaColumns * i + 2]);
            m[0].lambda[i - 2] = decimal.Parse(data[lambdaColumns * i + 3]);
            m[0].m2[i - 2] = decimal.Parse(data[lambdaColumns * i + 4]);
        }
        return m;
    }

    public CoefficientsData[] ReadCmCoefficient()
    {
        TextAsset fileContents = Resources.Load<TextAsset>("cmCoefficient");

        string[] data = fileContents.text.Split(new string[] { ";", "\n" }, StringSplitOptions.None);

        var m = coefficientsList.coefficients;

        int o = cmRows - 3;
        //m[0].diameter = new decimal[cmRows];
        //m[0].velocity = new decimal[cmRows];
        m[0].opornoscC = new decimal[o];
        m[0].przeplywnoscM = new decimal[o];

        for (int i = 3; i < cmRows; i++)
        {
            //m[0].diameter[i] = decimal.Parse(data[cmColumns * i]);
            //m[0].velocity[i] = decimal.Parse(data[(cmColumns * i) + 1]);
            m[0].opornoscC[i - 3] = decimal.Parse(data[cmColumns * i + 4]);
            m[0].przeplywnoscM[i - 3] = decimal.Parse(data[cmColumns * i + 5]);
        }
        return m;
    }

    public CoefficientsData ReadCoefficients()
    {
        CoefficientsData coefficients = new CoefficientsData();

        ReadLambdaCoefficient();
        ReadCmCoefficient();

        return coefficients;
    }


}