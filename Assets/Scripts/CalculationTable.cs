using NSubstitute.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;




partial class CalculationTable : MonoBehaviour
{
    [SerializeField] GameObject CalculationTableObject;
    [SerializeField] GameObject BaseTable;
    [SerializeField] GameObject IterationTablePrefab;
    [SerializeField] GameObject CellPrefab;

    [SerializeField] GameObject HorizontalHeader;
    [SerializeField] GameObject VerticalHeader;
    [SerializeField] GameObject CellsContainer;

    [SerializeField] AppLogic appLogic;
    [SerializeField] IterationManager iterationManager;

    List<GameObject> iterationTables = new List<GameObject>();

    Cell cell;
    //float TableWidth;
    //float TableHeight;
    bool isFirstInvokeOfDataUpdated = true;

    private void Awake()
    {
        appLogic = FindObjectOfType<AppLogic>();
        iterationManager = FindObjectOfType<IterationManager>();

        appLogic.updateDataVersion += OnDataUpdated;
        iterationManager.updateIterationResultsData += OnIterationDataUpdated;
    }

    private void Start()
    {
        cell = new Cell();
        //TableContainer = GetComponent<Transform>().GetChild(1).gameObject;
        
    }

    void OnDataUpdated(DataVersion dataVersion)
    {
        //List<string> verticalHeaderList = new List<string>();
        PropertyInfo[] properties = FillPropertiesArray();
        int horizontalCellsCount = properties.Length;
        int verticalCellsCount = dataVersion.nodesRozbiory.Length;

        if (isFirstInvokeOfDataUpdated)
        {
            TableSetupOnFirstDataChange(verticalCellsCount, horizontalCellsCount, properties);
            isFirstInvokeOfDataUpdated = false;
        }

        UpdateCellText(verticalCellsCount, horizontalCellsCount, dataVersion, properties);
    }
   
    void TableSetupOnFirstDataChange(int verticalCellsCount, int horizontalCellsCount, PropertyInfo[] properties)
    {
        //RectTransform rectTransform = CalculationTableObject.GetComponent<RectTransform>();
        RectTransform baseTableRectTransform = BaseTable.GetComponent<RectTransform>();

        int TableWidth = horizontalCellsCount * (int)cell.width + (int)cell.width;
        int TableHeight = verticalCellsCount * (int)cell.height + (int)cell.height;
        SetTablesChildrenSizes(baseTableRectTransform, new Vector2Int(TableWidth, TableHeight));

        DestroyCellsInTable();
        CreateCellsInTable(verticalCellsCount, horizontalCellsCount, BaseTable);

        for (int i = 0; i < horizontalCellsCount; i++)
        {
            HorizontalHeader.transform.GetChild(i + 1).GetChild(1).GetComponent<TextMeshProUGUI>().text = properties[i].Name;
        }
        for (int i = 0; i < verticalCellsCount; i++)
        {
            VerticalHeader.transform.GetChild(i + 1).GetChild(1).GetComponent<TextMeshProUGUI>().text = i.ToString();
            //VerticalHeader.transform.GetChild(i + 1).GetChild(1).GetComponent<TextMeshProUGUI>().text = verticalHeaderList(i);
        }
    }

    void OnIterationDataUpdated(List<RingData> ringDatas)
    {
        GameObject newIterationTable = Instantiate(IterationTablePrefab, CalculationTableObject.transform.GetChild(1));
        iterationTables.Add(newIterationTable);

        Type type = typeof(RingData.PipeCalculation);
        PropertyInfo[] allProperties = new PropertyInfo[6];
        allProperties[0] = type.GetProperty("Diameter");
        allProperties[1] = type.GetProperty("DesignFlow");
        allProperties[2] = type.GetProperty("HeadLoss");
        allProperties[3] = type.GetProperty("Quotient");
        allProperties[4] = type.GetProperty("DeltaDesignFlow");
        allProperties[5] = type.GetProperty("finalVelocity");

        int verticalCellsCount = 8;
        int horizontalCellsCount = allProperties.Length;

        //if (ringDatas.Last().Iterations.Last().pipeCalculations.Last().finalVelocity != 0) //no matter which one
        //{
        //    allProperties[5] = type.GetProperty("finalVelocity");
        //    horizontalCellsCount = allProperties.Length;
        //}
        //else
        //{
        //    horizontalCellsCount = allProperties.Length - 1;
        //}


        
        //RectTransform calculationTableRectTransform = CalculationTableObject.GetComponent<RectTransform>();
        RectTransform newIterationTableRectTransform = newIterationTable.GetComponent<RectTransform>();

        int TableWidth = horizontalCellsCount * (int)cell.width;
        int TableHeight = verticalCellsCount * (int)cell.height + (int)cell.height;

        SetTablesChildrenSizes(newIterationTableRectTransform, new Vector2Int(TableWidth, TableHeight));

        CreateCellsInTable(verticalCellsCount, horizontalCellsCount, newIterationTable);
        PopulateCellsWithText(horizontalCellsCount, verticalCellsCount, allProperties, ringDatas);
        
        foreach(var ringData in ringDatas)
        {
            int i = ringData.ringIndex;
            var iterationTableAddons = iterationTables.Last().transform.GetChild(2);
            var sumOfHeadloss = iterationTableAddons.GetChild(i).GetChild(1).GetComponent<TextMeshProUGUI>();

            AddToTableAdditionalIterationInfo(ringData, sumOfHeadloss);
            ChangeColor(ringData, sumOfHeadloss, horizontalCellsCount, verticalCellsCount);
        }

    }
    void AddToTableAdditionalIterationInfo(RingData ringData, TMPro.TextMeshProUGUI sumOfHeadloss)
    {
        //var sumOfQuotient = iterationTableAddons.GetChild(i+2).GetChild(1).GetComponent<TextMeshProUGUI>();

        sumOfHeadloss.text = "HlSum: " + ringData.Iterations.Last().sumOfHeadloss.ToString("f2");
        //sumOfQuotient.text = "QuoSum: " + ringData.Iterations.Last().sumOfQuotients.ToString("f2");

    }
    
    void ChangeColor(RingData ringData, TMPro.TextMeshProUGUI sumOfHeadloss, int horizontalCellsCount, int verticalCellsCount)
    {
        if (ringData.Iterations.Last().sumOfHeadlossBool == true)
            sumOfHeadloss.color = Color.green;
        else
            sumOfHeadloss.color = Color.red;
      
        for (int i = 0; i < verticalCellsCount; i++)
        {
            if (ringData.Iterations.Last().headlossList == null)
                break;

            if (ringData.Iterations.Last().headlossList[i] == true) 
            {
                iterationTables.Last().transform.GetChild(1).GetChild(horizontalCellsCount * (i) + 2).GetChild(1).GetComponent<TextMeshProUGUI>().color = Color.green;
            }
            else
            {
                iterationTables.Last().transform.GetChild(1).GetChild(horizontalCellsCount * (i) + 2).GetChild(1).GetComponent<TextMeshProUGUI>().color = Color.red;
            }

            if (ringData.Iterations.Last().velocityList[i] == false)
            {
                iterationTables.Last().transform.GetChild(1).GetChild(horizontalCellsCount * (i) + 5).GetChild(1).GetComponent<TextMeshProUGUI>().color = Color.red;
            }
            else
            {
                iterationTables.Last().transform.GetChild(1).GetChild(horizontalCellsCount * (i) + 5).GetChild(1).GetComponent<TextMeshProUGUI>().color = Color.green;
            }

        }
    }

    void SetTablesChildrenSizes(RectTransform rectTransform, Vector2Int vector)
    {
        
        Vector2 parentVector = rectTransform.transform.parent.GetComponent<RectTransform>().sizeDelta;

        Vector2 vectoro = new Vector2();
        vectoro.x = 2000;

        if (parentVector.x < vectoro.x)//NewIterationTable
        {
            parentVector.x = vector.x;
            rectTransform.transform.parent.GetComponent<RectTransform>().sizeDelta = parentVector;

            rectTransform.sizeDelta = vector;
            rectTransform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(vector.x, cell.height);
            rectTransform.GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(vector.x, vector.y - cell.height);
            rectTransform.GetChild(2).GetComponent<RectTransform>().sizeDelta = new Vector2(vector.x, cell.height);
        }
        else //BaseTable
        {
            parentVector = vector;

            rectTransform.transform.parent.GetComponent<RectTransform>().sizeDelta = parentVector;
            rectTransform.sizeDelta = vector;
            rectTransform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(vector.x, cell.height);
            rectTransform.GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(cell.width, vector.y);
            rectTransform.GetChild(2).GetComponent<RectTransform>().sizeDelta = new Vector2(vector.x - cell.width, vector.y - cell.height);
        }
    }

    void CreateCellsInTable(float verticalCellsCount, float horizontalCellsCount, GameObject parent)
    {

        int horizontalCells = (int)horizontalCellsCount;
        int verticalCells = (int)verticalCellsCount;
        int cellsCount = horizontalCells * verticalCells;

        //CheckTablePopulation();

        int childCounter = 0;
        foreach (var child in parent.transform)
        {
            childCounter++;
        }
        if (childCounter == 3)
        {
            SpawnCells(horizontalCells, parent.transform.GetChild(0));
            SpawnCells(cellsCount, parent.transform.GetChild(1));
        }
        else
        {
            SpawnCells(horizontalCells + 1, parent.transform.GetChild(0));
            SpawnCells(verticalCells + 1, parent.transform.GetChild(1));
            SpawnCells(cellsCount, parent.transform.GetChild(2));
        }
    }

    void PopulateCellsWithText(int horizontalCellsCount, int verticalCellsCount, PropertyInfo[] allProperties, List<RingData> ringDatas)
    {

        for (int i = 0; i < horizontalCellsCount; i++)
        {
            iterationTables.Last().transform.GetChild(0).GetChild(i).GetChild(1).GetComponent<TextMeshProUGUI>().text = allProperties[i].Name;
        }
        for (int i = 0; i < verticalCellsCount; i++)
        {
            //VerticalHeader.transform.GetChild(i).GetChild(1).GetComponent<TextMeshProUGUI>().text = verticalHeaderList(i);
        }

        int ringDataCounter = 0;
        int pipesPerRing = ringDatas[ringDataCounter].pipesPerRing;
        for (int i = 0; i < verticalCellsCount; i++)
        {

            if (i == pipesPerRing)
            {
                ringDataCounter = 1;
            }

            for (int j = 0; j < horizontalCellsCount; j++)
            {
                string propertyName = allProperties[j].Name;
                int pipeIndex = i - ringDataCounter * pipesPerRing;
                int cellIndex = j + horizontalCellsCount * i;

                object propertyValue = ReflectionHelper.GetPropertyValue(ringDatas[ringDataCounter].Iterations.Last().pipeCalculations[pipeIndex], propertyName);
                decimal value = (decimal)propertyValue;
                iterationTables.Last().transform.GetChild(1).GetChild(cellIndex).GetChild(1).GetComponent<TextMeshProUGUI>().text = value.ToString("f2");
            }
        }
    }




    void UpdateCellText(int verticalCellsCount, int horizontalCellsCount, DataVersion dataVersion, PropertyInfo[] properties)
    {
        for (int i = 0; i < verticalCellsCount; i++)
        {
            for (int j = 0; j < horizontalCellsCount; j++)
            {
                object propertyValueObject = ReflectionHelper.GetPropertyValue(dataVersion, properties[j].Name);
                if (propertyValueObject is decimal[] propertyValueArray)
                {
                    //TODO: if isVisible - allow for displaying in table - 
                    decimal value = propertyValueArray[i];
                    CellsContainer.transform.GetChild(j + horizontalCellsCount * i).GetChild(1).GetComponent<TextMeshProUGUI>().text = value.ToString();
                }
            }
        }
    }
    PropertyInfo[] FillPropertiesArray()
    {
        PropertyInfo[] properties = typeof(DataVersion).GetProperties().Where(p => p.PropertyType == typeof(decimal[])).ToArray();
        return properties;
    }
    /*
    PropertyInfo[] FillPropertiesArrayy(List<RingData> ringDatas)
    {
        List<PropertyInfo[]> pipesProperties = ;
        foreach (var ringData in ringDatas)
        {
            List<PropertyInfo[]> pipesProperties = ringData.GetPipesProperties();

            return pipesProperties;
        }
        return pipesProperties;
   
}*/

    void SpawnCells(int cellsToSpawn, Transform parentToSpawn)
    {
        Debug.Log("Cells Count = " + cellsToSpawn + ", in parent: " + parentToSpawn);
        for (int i = 0; i < cellsToSpawn; i++)
        {
            GameObject cell = Instantiate(CellPrefab, parentToSpawn);
            cell.GetComponent<RectTransform>().sizeDelta = new Vector2(this.cell.width, this.cell.height);
        }
    }

    void DestroyCellsInTable()
    {
        foreach (Transform child in HorizontalHeader.transform)
        {
            //child.GetChild(1).GetComponent<TextMeshPro>().text = "";
            Destroy(child.gameObject);
        }
        foreach (Transform child in VerticalHeader.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in CellsContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }
}


public static class ReflectionHelper
{
    public static object GetPropertyValue(object obj, string propertyName)
    {
        // Use reflection to get the property by name
        PropertyInfo propertyInfo = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (propertyInfo != null)
        {
            // Get the value of the property for the given object
            return propertyInfo.GetValue(obj, null);
        }
        return null;
    }
}

