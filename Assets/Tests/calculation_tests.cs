using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NSubstitute;
using NUnit.Framework;

namespace Tests
{
    public class calculation_tests : MonoBehaviour
    {
        CalculationManager calculationManager;
        [Test]
        public void addup_inflows()
        {
            int index = 3;
            //float addedupInflow = calculationManager.AddUpInFlows(index);

            int finalDamage = DamageCalculator.CalculateDamage(amount: 10, mitigationPercent: 0.5f);

            Assert.AreEqual(expected: 5, actual: finalDamage);
        }

        [Test]
        public void dataloader_test()
        {
            DataLoader loader = new DataLoader();
            //DataLoader.DataSet data = new DataLoader.DataSet();

            DataLoader.DataSetList myDataSetList = new DataLoader.DataSetList();
            myDataSetList.dataSet = new DataLoader.DataSet[loader.setsOfData];


            for (int i = 0; i < loader.setsOfData; i++)
            {
                myDataSetList.dataSet[i] = new DataLoader.DataSet();
            }
            var m = loader.ReadPipeData();
            
            int[] nodeIndexes = new int[8];
            //int[] nodeIndexes = data.nodeID;
            int[] pipeIndexes = new int[9];
            //int[] pipeIndexes = data.pipeID;


            Assert.AreEqual(expected: pipeIndexes, actual: m);
        }

    }
    /*
    public class applogic_tests : MonoBehaviour {

        [Test]
        public void oblicz_Qhmax()
        {

            float value = 0;



            Assert.AreEqual(expected: 
        }


        [Test]
        public void oblicz_Q_zbiornika()
        {

        }
    }*/

}

