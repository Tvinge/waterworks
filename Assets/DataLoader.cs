using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DataLoader : MonoBehaviour
{
    public TextAsset textAssetNodeData;
    public TextAsset textAssetPipeData;

    int setsOfData = 16;
    int columnNodeCount = 4;
    int columnPipeCount = 5;
    int nodeCount = 8;
    int pipeCount = 9;

    [System.Serializable]
    public class DataSet
    {

        public int dataset;


        [Header("Node")]
        public int[] nodeID;
        public float[] nodeRozbiory;
        public float[] nodeHeight;

        [Header("Pipe")]
        public int[] pipeID;
        public float[] pipeRozbiory;
        public float[] pipeLength;
        public int[] pipeHeight;

    }

    [System.Serializable]
    public class DataSetList
    {
        public DataSet[] dataSet;
    }

    public DataSetList myDataSetList = new DataSetList();

    private void Start()
    {

        myDataSetList.dataSet = new DataSet[setsOfData];
        for (int i = 0; i < setsOfData; i++)
        {
            myDataSetList.dataSet[i] = new DataSet();
        }
        ReadPipeData();
        ReadNodeData();

    }
    void ReadPipeData()
    {
        string[] data = textAssetPipeData.text.Split(new String[] { ";", "\n" }, StringSplitOptions.None);

        int rows = data.Length / columnPipeCount - 1;

        for (int i = 0; i < rows; i += pipeCount)
        {
            if (i == 0)
            {
                myDataSetList.dataSet[i].dataset = int.Parse(data[columnPipeCount * (i + 1)]);

                myDataSetList.dataSet[i].pipeID = new int[pipeCount];
                myDataSetList.dataSet[i].pipeRozbiory = new float[pipeCount];
                myDataSetList.dataSet[i].pipeLength = new float[pipeCount];
                myDataSetList.dataSet[i].pipeHeight = new int[pipeCount];

                for (int j = 0; j < pipeCount; j++)
                {
                    myDataSetList.dataSet[i].pipeID[j] = int.Parse(data[columnPipeCount * (i + j + 1) + 1]);
                    myDataSetList.dataSet[i].pipeRozbiory[j] = float.Parse(data[columnPipeCount * (i + j + 1) + 2]);
                    myDataSetList.dataSet[i].pipeLength[j] = float.Parse(data[columnPipeCount * (i + j + 1) + 3]);
                    myDataSetList.dataSet[i].pipeHeight[j] = int.Parse(data[columnPipeCount * (i + j + 1) + 4]);
                }
            }
            else
            {
                myDataSetList.dataSet[i / pipeCount] = new DataSet();
                myDataSetList.dataSet[i / pipeCount].dataset = int.Parse(data[columnPipeCount * (i + 1)]);

                myDataSetList.dataSet[i / pipeCount].pipeID = new int[pipeCount];
                myDataSetList.dataSet[i / pipeCount].pipeRozbiory = new float[pipeCount];
                myDataSetList.dataSet[i / pipeCount].pipeLength = new float[pipeCount];
                myDataSetList.dataSet[i / pipeCount].pipeHeight = new int[pipeCount];

                for (int j = 0; j < pipeCount; j++)
                {
                    myDataSetList.dataSet[i / pipeCount].pipeID[j] = int.Parse(data[columnPipeCount * (i + j + 1) + 1]);
                    myDataSetList.dataSet[i / pipeCount].pipeRozbiory[j] = float.Parse(data[columnPipeCount * (i + j + 1) + 2]);
                    myDataSetList.dataSet[i / pipeCount].pipeLength[j] = float.Parse(data[columnPipeCount * (i + j + 1) + 3]);
                    myDataSetList.dataSet[i / pipeCount].pipeHeight[j] = int.Parse(data[columnPipeCount * (i + j + 1) + 4]);
                }
            }
        }
    }

    void ReadNodeData()
    {
        string[] data = textAssetNodeData.text.Split(new String[] { ";", "\n" }, StringSplitOptions.None);

        int rows = data.Length / columnNodeCount - 1;

        for (int i = 0; i < rows; i += nodeCount)
        {
            if (i == 0)
            {
                myDataSetList.dataSet[i].dataset = int.Parse(data[columnNodeCount * (i + 1)]);

                myDataSetList.dataSet[i].nodeID = new int[nodeCount];
                myDataSetList.dataSet[i].nodeRozbiory = new float[nodeCount];
                myDataSetList.dataSet[i].nodeHeight = new float[nodeCount];

                for (int j = 0; j < nodeCount; j++)
                {
                    myDataSetList.dataSet[i].nodeID[j] = int.Parse(data[columnNodeCount * (i + j + 1) + 1]);
                    myDataSetList.dataSet[i].nodeRozbiory[j] = float.Parse(data[columnNodeCount * (i + j + 1) + 2]);
                    myDataSetList.dataSet[i].nodeHeight[j] = float.Parse(data[columnNodeCount * (i + j + 1) + 3]);
                }
            }
            else
            {
                myDataSetList.dataSet[i / nodeCount] = new DataSet();
                myDataSetList.dataSet[i / nodeCount].dataset = int.Parse(data[columnNodeCount * (i + 1)]);

                myDataSetList.dataSet[i / nodeCount].nodeID = new int[nodeCount];
                myDataSetList.dataSet[i / nodeCount].nodeRozbiory = new float[nodeCount];
                myDataSetList.dataSet[i / nodeCount].nodeHeight = new float[nodeCount];

                for (int j = 0; j < nodeCount; j++)
                {
                    myDataSetList.dataSet[i / nodeCount].nodeID[j] = int.Parse(data[columnNodeCount * (i + j + 1) + 1]);
                    myDataSetList.dataSet[i / nodeCount].nodeRozbiory[j] = float.Parse(data[columnNodeCount * (i + j + 1) + 2]);
                    myDataSetList.dataSet[i / nodeCount].nodeHeight[j] = float.Parse(data[columnNodeCount * (i + j + 1) + 3]);
                }
            }
        }
    }
}
/*
void ReadCSV()
{
string[] data = textAssetNodeData.text.Split(new String[] { ";", "\n" }, StringSplitOptions.None);
for (int i = 0; i < data.Length; i++)
{
    Debug.Log("data number " + i + ", " + data[i]);
}

int rows = data.Length / columnCount - 1;
myDataSetList.dataSet = new DataSet[(rows) / 8];




for (int i = 0; i < rows; i += 8)
{
    if (i == 0)
    {
        myDataSetList.dataSet[i] = new DataSet();
        myDataSetList.dataSet[i].dataset = int.Parse(data[columnCount * (i + 1)]);

        myDataSetList.dataSet[i].nodeID = new int[8];
        myDataSetList.dataSet[i].nodeRozbiory = new float[8];
        myDataSetList.dataSet[i].nodeHeight = new float[8];



        for (int j = 0; j < 8; j++)
        {
            myDataSetList.dataSet[i].nodeID[j] = int.Parse(data[columnCount * (i + j + 1) + 1]);
            myDataSetList.dataSet[i].nodeRozbiory[j] = float.Parse(data[columnCount * (i + j + 1) + 2]);
            myDataSetList.dataSet[i].nodeHeight[j] = float.Parse(data[columnCount * (i + j + 1) + 3]);
        }

    }
    else
    {
        myDataSetList.dataSet[i / 8] = new DataSet();
        myDataSetList.dataSet[i / 8].dataset = int.Parse(data[columnCount * (i + 1)]);

        myDataSetList.dataSet[i / 8].nodeID = new int[8];
        myDataSetList.dataSet[i / 8].nodeRozbiory = new float[8];
        myDataSetList.dataSet[i / 8].nodeHeight = new float[8];


        for (int j = 0; j < 8; j++)
        {
            myDataSetList.dataSet[i / 8].nodeID[j] = int.Parse(data[columnCount * (i + j + 1) + 1]);
            myDataSetList.dataSet[i / 8].nodeRozbiory[j] = float.Parse(data[columnCount * (i + j + 1) + 2]);
            myDataSetList.dataSet[i / 8].nodeHeight[j] = float.Parse(data[columnCount * (i + j + 1) + 3]);
        }
    }


}
*/


