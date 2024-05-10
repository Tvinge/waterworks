using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DataLoader : MonoBehaviour
{
    public TextAsset textAssetData;
    public TextAsset textAssetNodeData;
    public TextAsset textAssetPipeData;

    public int setsOfData = 16;
    int columnNodeCount = 4;
    int columnPipeCount = 7;
    int nodeCount = 8;
    int pipeCount = 9;

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
        ReadPipeData();
        ReadNodeData();
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
        Debug.Log($" pipeV: {dataVersion.pipesRozbiory} node: {dataVersion.nodesRozbiory}");
        Debug.Log($" pipeS: {dataSet.pipeRozbiory} node: {dataSet.nodeRozbiory}");
        return dataVersion;
    }

    public DataSet[] ReadPipeData()
    {
        string[] data = textAssetPipeData.text.Split(new String[] { ";", "\n" }, StringSplitOptions.None);

        int rows = data.Length / columnPipeCount - 1;

        var m = myDataSetList.dataSet;

        for (int i = 0; i < rows; i += pipeCount)
        {
            if (i == 0)
            {
                m[i].dataset = int.Parse(data[columnPipeCount * (i + 1)]);
                m[i].zasilanieZPompowni = decimal.Parse(data[columnPipeCount * (i + 1) + 5]);
                m[i].wspolczynnik = decimal.Parse(data[columnPipeCount * (i + 1) + 6]);

                m[i].pipeID = new int[pipeCount];
                m[i].pipeRozbiory = new decimal[pipeCount];
                m[i].pipeLength = new decimal[pipeCount];
                m[i].pipeHeight = new int[pipeCount];

                for (int j = 0; j < pipeCount; j++)
                {
                    m[i].pipeID[j] = int.Parse(data[columnPipeCount * (i + j + 1) + 1]);
                    m[i].pipeRozbiory[j] = decimal.Parse(data[columnPipeCount * (i + j + 1) + 2]);
                    m[i].pipeLength[j] = decimal.Parse(data[columnPipeCount * (i + j + 1) + 3]);
                    m[i].pipeHeight[j] = int.Parse(data[columnPipeCount * (i + j + 1) + 4]);
                }
            }
            else
            {
                m[i / pipeCount] = new DataSet();
                m[i / pipeCount].dataset = int.Parse(data[columnPipeCount * (i + 1)]);
                m[i / pipeCount].zasilanieZPompowni = decimal.Parse(data[columnPipeCount * (i + 1) + 5]);
                m[i / pipeCount].wspolczynnik = decimal.Parse(data[columnPipeCount * (i + 1) + 6]);

                m[i / pipeCount].pipeID = new int[pipeCount];
                m[i / pipeCount].pipeRozbiory = new decimal[pipeCount];
                m[i / pipeCount].pipeLength = new decimal[pipeCount];
                m[i / pipeCount].pipeHeight = new int[pipeCount];

                for (int j = 0; j < pipeCount; j++)
                {
                    m[i / pipeCount].pipeID[j] = int.Parse(data[columnPipeCount * (i + j + 1) + 1]);
                    m[i / pipeCount].pipeRozbiory[j] = decimal.Parse(data[columnPipeCount * (i + j + 1) + 2]);
                    m[i / pipeCount].pipeLength[j] = decimal.Parse(data[columnPipeCount * (i + j + 1) + 3]);
                    m[i / pipeCount].pipeHeight[j] = int.Parse(data[columnPipeCount * (i + j + 1) + 4]);
                }
            }
        }
        return m;
    }

    public DataSet[] ReadNodeData()
    {
        string[] data = textAssetNodeData.text.Split(new String[] { ";", "\n" }, StringSplitOptions.None);

        int rows = data.Length / columnNodeCount - 1;

        var m = myDataSetList.dataSet;

        for (int i = 0; i < rows; i += nodeCount)
        {
            if (i == 0)
            {
                m[i].dataset = int.Parse(data[columnNodeCount * (i + 1)]);

                m[i].nodeID = new int[nodeCount];
                m[i].nodeRozbiory = new decimal[nodeCount];
                m[i].nodeHeight = new decimal[nodeCount];

                for (int j = 0; j < nodeCount; j++)
                {
                    m[i].nodeID[j] = int.Parse(data[columnNodeCount * (i + j + 1) + 1]);
                    m[i].nodeRozbiory[j] = decimal.Parse(data[columnNodeCount * (i + j + 1) + 2]);
                    m[i].nodeHeight[j] = decimal.Parse(data[columnNodeCount * (i + j + 1) + 3]);
                }
            }
            else
            {
                m[i / nodeCount].dataset = int.Parse(data[columnNodeCount * (i + 1)]);

                m[i / nodeCount].nodeID = new int[nodeCount];
                m[i / nodeCount].nodeRozbiory = new decimal[nodeCount];
                m[i / nodeCount].nodeHeight = new decimal[nodeCount];

                for (int j = 0; j < nodeCount; j++)
                {
                    m[i / nodeCount].nodeID[j] = int.Parse(data[columnNodeCount * (i + j + 1) + 1]);
                    m[i / nodeCount].nodeRozbiory[j] = decimal.Parse(data[columnNodeCount * (i + j + 1) + 2]);
                    m[i / nodeCount].nodeHeight[j] = decimal.Parse(data[columnNodeCount * (i + j + 1) + 3]);
                }
            }
        }
        return m;
    }


}