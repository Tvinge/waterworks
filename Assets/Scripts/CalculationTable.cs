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

    [SerializeField] List<BaseTable> BaseTables;
    //[SerializeField] GameObject BaseTable;
    [SerializeField] GameObject IterationTablePrefab;
    [SerializeField] GameObject CellPrefab;


    [Serializable]
    public class BaseTable
    {
        public GameObject BaseTableObject;
        public GameObject HorizontalHeader;
        public GameObject VerticalHeader;
        public GameObject CellsContainer;
    }


    [SerializeField] AppLogic appLogic;
    [SerializeField] IterationManager iterationManager;

    DataVersions DataVersions;

    List<List<GameObject>> iterationTableForDiffDatas = new()
{
    new List<GameObject>(),
    new List<GameObject>(),
    new List<GameObject>()
};
    //List<GameObject> iterationTables = new List<GameObject>();

    Cell cell;
    //float TableWidth;
    //float TableHeight;
    bool isFirstInvokeOfDataUpdated = true;
    bool isFirstInvokeOfDataUpdates = true;

    Vector2 baseTableVector = new Vector2(0, 0);

    private void Awake()
    {
        appLogic = FindObjectOfType<AppLogic>();
        iterationManager = FindObjectOfType<IterationManager>();

        //appLogic.updateDataVersion += OnDataUpdated;
        appLogic.updateDataVersions += OnDatasUpdated;
        appLogic.resetSimulation += ResetCalculationTable;
        iterationManager.updateIterationResultsData += OnIterationDataUpdated;
    }

    private void Start()
    {
        cell = new Cell();
        //TableContainer = GetComponent<Transform>().GetChild(1).gameObject;        
    }

    void OnDatasUpdated(DataVersions dataVersions)
    {
        for (int i = 0; i < 3; i++)
        {
            DataVersion dataVersion = dataVersions.dataVersions[i];
            OnDataUpdated(dataVersion, BaseTables[i]);
        }
    }
    void OnDataUpdated(DataVersion dataVersion, BaseTable tableToUpdate)
    {
        PropertyInfo[] properties = FillPropertiesArray();
        int horizontalCellsCount = properties.Length;
        int verticalCellsCount = dataVersion.nodesConsumptions.Length;

        if (isFirstInvokeOfDataUpdates)
        {
            for (int i = 0; i < 3; i++)
            {
                TableSetupOnFirstDataChange(BaseTables[i], verticalCellsCount, horizontalCellsCount, properties);
            }
        }
        isFirstInvokeOfDataUpdates = false;

        UpdateCellText(tableToUpdate, verticalCellsCount, horizontalCellsCount, dataVersion, properties);
    }
   void ResetCalculationTable()
    {
        //iterationTableForDiffDatas.Clear();
        for (int i = 0; i < CalculationTableObject.transform.GetChild(1).childCount; i++) 
        {
            if (CalculationTableObject.transform.GetChild(1).GetChild(i).childCount == 0)
                continue;
            foreach (Transform child in CalculationTableObject.transform.GetChild(1).GetChild(i))
            {
                Destroy(child.gameObject);
            }
            //Destroy(CalculationTableObject.transform.GetChild(1).GetChild(i).gameObject); 
        }
        CalculationTableObject.GetComponent<RectTransform>().sizeDelta = baseTableVector;
    }

    void TableSetupOnFirstDataChange(BaseTable tableToUpdate, int verticalCellsCount, int horizontalCellsCount, PropertyInfo[] properties)
    {
        RectTransform baseTableRectTransform = tableToUpdate.BaseTableObject.GetComponent<RectTransform>();
        Transform tables = tableToUpdate.BaseTableObject.transform.parent;

        int dataTypesCount = 3;

        int TableWidth = horizontalCellsCount * (int)cell.width + (int)cell.width;
        int TableHeight = verticalCellsCount * (int)cell.height + (int)cell.height;
        Vector2 parentVector = new Vector2(TableWidth, dataTypesCount * TableHeight + dataTypesCount * cell.height);
        
        tables.GetComponent<RectTransform>().sizeDelta = parentVector;
        CalculationTableObject.GetComponent<RectTransform>().sizeDelta = parentVector;
        baseTableVector = parentVector;

        SetTablesChildrenSizes(baseTableRectTransform, new Vector2Int(TableWidth, TableHeight));

        DestroyCellsInTable(tableToUpdate);
        CreateCellsInTable(verticalCellsCount, horizontalCellsCount, tableToUpdate.BaseTableObject);

        for (int i = 0; i < horizontalCellsCount; i++)
        {
            tableToUpdate.HorizontalHeader.transform.GetChild(i + 1).GetChild(1).GetComponent<TextMeshProUGUI>().text = properties[i].Name;
        }
        for (int i = 0; i < verticalCellsCount; i++)
        {
            tableToUpdate.VerticalHeader.transform.GetChild(i + 1).GetChild(1).GetComponent<TextMeshProUGUI>().text = i.ToString();
            //VerticalHeader.transform.GetChild(i + 1).GetChild(1).GetComponent<TextMeshProUGUI>().text = verticalHeaderList(i);
        }
    }

    void OnIterationDataUpdated(List<RingData> ringDatas, int dataType, bool isFirstInvokeForThisData, int iterationCount)
    {
        GameObject newIterationTable = Instantiate(IterationTablePrefab, CalculationTableObject.transform.GetChild(1).GetChild(dataType));

        if (isFirstInvokeForThisData)
        {
            iterationTableForDiffDatas[dataType] = new List<GameObject>();
        }

        iterationTableForDiffDatas[dataType].Add(newIterationTable);

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

        RectTransform newIterationTableRectTransform = newIterationTable.GetComponent<RectTransform>();

        int TableWidth = horizontalCellsCount * (int)cell.width;
        int TableHeight = verticalCellsCount * (int)cell.height + (int)cell.height;

        Vector2 vector = new Vector2(TableWidth * (iterationCount + 2), 0);
        Vector2 vectorForParent = new Vector2(TableWidth * iterationCount, TableHeight);
        CalculationTableObject.transform.GetChild(1).GetComponent<RectTransform>().sizeDelta = vectorForParent;
        CalculationTableObject.GetComponent<RectTransform>().sizeDelta = baseTableVector + vector;


        SetTablesChildrenSizes(newIterationTableRectTransform, new Vector2Int(TableWidth, TableHeight));

        CreateCellsInTable(verticalCellsCount, horizontalCellsCount, newIterationTable);
        PopulateCellsWithText(horizontalCellsCount, verticalCellsCount, allProperties, ringDatas, dataType);
        
        foreach(var ringData in ringDatas)
        {
            int i = ringData.ringIndex;
            //var iterationTableAddons = iterationTables.Last().transform.GetChild(2);
            var iterationTableAddons = iterationTableForDiffDatas[dataType].Last().transform.GetChild(2);
            var sumOfHeadloss = iterationTableAddons.GetChild(i).GetChild(1).GetComponent<TextMeshProUGUI>();

            AddToTableAdditionalIterationInfo(ringData, sumOfHeadloss);
            ChangeColor(ringData, sumOfHeadloss, horizontalCellsCount, verticalCellsCount, dataType);
        }

        //for some reason vertcal layout is not being displayed properly after creating new instances so I have to force rebuild it
        LayoutRebuilder.ForceRebuildLayoutImmediate(CalculationTableObject.transform.GetChild(1).GetComponent<RectTransform>()); 
    }
    void AddToTableAdditionalIterationInfo(RingData ringData, TMPro.TextMeshProUGUI sumOfHeadloss)
    {
        //var sumOfQuotient = iterationTableAddons.GetChild(i+2).GetChild(1).GetComponent<TextMeshProUGUI>();

        sumOfHeadloss.text = "HlSum: " + ringData.Iterations.Last().sumOfHeadloss.ToString("f2");
        //sumOfQuotient.text = "QuoSum: " + ringData.Iterations.Last().sumOfQuotients.ToString("f2");

    }
    
    void ChangeColor(RingData ringData, TMPro.TextMeshProUGUI sumOfHeadloss, int horizontalCellsCount, int verticalCellsCount, int dataType)
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
                iterationTableForDiffDatas[dataType].Last().transform.GetChild(1).GetChild(horizontalCellsCount * (i) + 2).GetChild(1).GetComponent<TextMeshProUGUI>().color = Color.green;
            }
            else
            {
                iterationTableForDiffDatas[dataType].Last().transform.GetChild(1).GetChild(horizontalCellsCount * (i) + 2).GetChild(1).GetComponent<TextMeshProUGUI>().color = Color.red;
            }

            if (ringData.Iterations.Last().velocityList[i] == false)
            {
                iterationTableForDiffDatas[dataType].Last().transform.GetChild(1).GetChild(horizontalCellsCount * (i) + 5).GetChild(1).GetComponent<TextMeshProUGUI>().color = Color.red;
            }
            else
            {
                iterationTableForDiffDatas[dataType].Last().transform.GetChild(1).GetChild(horizontalCellsCount * (i) + 5).GetChild(1).GetComponent<TextMeshProUGUI>().color = Color.green;
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

    void PopulateCellsWithText(int horizontalCellsCount, int verticalCellsCount, PropertyInfo[] allProperties, List<RingData> ringDatas, int dataType)
    {

        for (int i = 0; i < horizontalCellsCount; i++)
        {
            iterationTableForDiffDatas[dataType].Last().transform.GetChild(0).GetChild(i).GetChild(1).GetComponent<TextMeshProUGUI>().text = allProperties[i].Name;
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
                iterationTableForDiffDatas[dataType].Last().transform.GetChild(1).GetChild(cellIndex).GetChild(1).GetComponent<TextMeshProUGUI>().text = value.ToString("f2");
            }
        }
    }




    void UpdateCellText(BaseTable table, int verticalCellsCount, int horizontalCellsCount, DataVersion dataVersion, PropertyInfo[] properties)
    {
        int totalCells = verticalCellsCount * horizontalCellsCount;
        int childCount = table.CellsContainer.transform.childCount;

        Debug.Log($"Total cells expected: {totalCells}, Child count in CellsContainer: {childCount}");

        for (int i = 0; i < verticalCellsCount; i++)
        {
            for (int j = 0; j < horizontalCellsCount; j++)
            {
                int index = j + horizontalCellsCount * i;
                if (index >= childCount)
                {
                    Debug.LogError($"Index out of bounds: {index}, Child count: {childCount}");
                    continue;
                }

                object propertyValueObject = ReflectionHelper.GetPropertyValue(dataVersion, properties[j].Name);
                if (propertyValueObject is decimal[] propertyValueArray)
                {
                    decimal value = propertyValueArray[i];
                    table.CellsContainer.transform.GetChild(index).GetChild(1).GetComponent<TextMeshProUGUI>().text = value.ToString("f2");
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

    void DestroyCellsInTable(BaseTable baseTable)
    {
        foreach (Transform child in baseTable.HorizontalHeader.transform)
        {
            //child.GetChild(1).GetComponent<TextMeshPro>().text = "";
           // Destroy(child.gameObject);
        }
        foreach (Transform child in baseTable.VerticalHeader.transform)
        {
            //Destroy(child.gameObject);
        }
        foreach (Transform child in baseTable.CellsContainer.transform)
        {
           // Destroy(child.gameObject);
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

