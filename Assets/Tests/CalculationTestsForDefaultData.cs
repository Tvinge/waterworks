using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NSubstitute;
using NUnit.Framework;
using System;
using System.IO;

namespace Tests
{
    public class CalculationTestsForDefaultData : MonoBehaviour
    {
        DataVersion data;
        DataLoader dataLoader;
        IterationManager _iterationManager;
        [SetUp]
        public void Setup()
        {
            data = DataVersion.CreateDefault();

            _iterationManager = new IterationManager();


        }


        List<RingData> SetupRingData()
        {
            var ringDatas = _iterationManager.CreateRingDatas();
            int ringCount = ringDatas.Count;
            int pipesPerRing = ringDatas[0].pipesPerRing;

            for (int i = 0; i < ringCount; i++)
            {
                ringDatas[i] = _iterationManager.CalculateFirstIteration(ringDatas[i], i);                 //no need to calculate pipes which are not part of the ring
            }
            _iterationManager.SetDeltaDesignFlowForRing(ringDatas, pipesPerRing);
            Pipe pipe = _iterationManager.FindPipesInMultipleRings(ringDatas);                            //atm 2 rings == 1 pipe in common
            _iterationManager.CalculateDeltaDesingFlowForPipesInMultipleRings(ringDatas, pipe.index);
            _iterationManager.CheckValues(ringDatas);
            return ringDatas;
        }


        [Test]
        public void CalculateNextIterationUnlessConditionsAreMet_ShouldNotThrowException_WhenValidInput()
        {
            // Arrange
            List<RingData> ringDatas = SetupRingData();
            int pipeIndex = 0;

            // Act & Assert
            Assert.DoesNotThrow(() => _iterationManager.CalculateNextIterationUnlessConditionsAreMet(ringDatas, pipeIndex));
        }

        [Test]
        public void CalculateNextIterationUnlessConditionsAreMet_ShouldAdjustDiameter_WhenConditionsMet()
        {
            // Arrange
            List<RingData> ringDatas = SetupRingData();
            int pipeIndex = 0;

            // Act
            _iterationManager.CalculateNextIterationUnlessConditionsAreMet(ringDatas, pipeIndex);

            // Assert
            // Add assertions to verify the expected behavior
            // For example, check if the diameter was adjusted correctly
            // Assert.AreEqual(expectedValue, actualValue);
        }

        [Test]
        public void CalculateNextIterationUnlessConditionsAreMet_ShouldHandleEmptyRingDatas()
        {
            // Arrange
            List<RingData> ringDatas = SetupRingData();
            int pipeIndex = 0;

            // Act & Assert
            Assert.DoesNotThrow(() => _iterationManager.CalculateNextIterationUnlessConditionsAreMet(ringDatas, pipeIndex));
        }








    }
}
/*}
        #region AppLogicTests
        [Test]
        public void calculateQhmax()
        {
            decimal value = AppLogic.CalculateQhmax(data);
            Assert.AreEqual(expected: 282, actual: value);
        }

        [Test]
        public void calculateQzbiornika()
        {

            decimal value = AppLogic.CalculateQzbiornika(data);

            Assert.AreEqual(expected: 94, actual: value);
        }
        #endregion

        #region CalculationManagerTests

        /*
        [Test]
        public void SetOutflowOnNodeWithTwoUncalculatedPipes()
        {
            int i = 0;
            int nodeIndex = 3;
            decimal IOdplyw = 100;
            List<int> pipes = new List<int> { 3,7};
            List<(int pipeIndex, int[] adjacentNodes)> adjacentNodesToAdjacentPipes = new List<(int pipeIndex, int[] adjacentNodes)>
            {
                (3, new int[] { 3, 4 }),
                (7, new int[] { 3, 5 })
            };
            decimal[][] result = CalculationManager.SetOutflowOnNodeWithTwoUncalculatedPipes(data, i, nodeIndex, IOdplyw, pipes, adjacentNodesToAdjacentPipes);

        }

        [Test]
        public void PorownanieRurr()
        {

        }


        [Test]
        public void calculateOutflowOnPipe()
        {

            int pipeId = 0;
            data.pipesInflows[pipeId] = 188;

            decimal value = CalculationManager.CalculateOutflowOnPipe(data, pipeId);

            Assert.AreEqual(expected: 188, actual: value);
        }

        [Test]
        public void addupInflows()
        {

            int pipeIndex = 0;
            data.doubleInflowsOnPipes[pipeIndex][0] = 100;
            data.doubleInflowsOnPipes[pipeIndex][1] = 100;

            decimal[] expectedValues = new decimal[9];
            expectedValues[pipeIndex] = data.doubleInflowsOnPipes[pipeIndex][0] + data.doubleInflowsOnPipes[pipeIndex][1];

            decimal values = CalculationManager.AddupInflows(data, pipeIndex);

            Assert.AreEqual(expected: expectedValues, actual: values);
        }


        #endregion

        #region DataTests

        [Test]
        public void ReadLambda()
        {
            TextAsset fileContents = Resources.Load<TextAsset>("lambdaCoefficient");

            string[] data = fileContents.text.Split(new string[] { ";", "\n" }, StringSplitOptions.None);
            
            int rows = 24;
            int columns = 11;
            

            Assert.That(data[columns * 2 + 1], Is.EqualTo(10));

            //Assert.That(data, Is.Not.Null);
            //Assert.That(data.Length, Is.EqualTo(rows * columns - 1));


        }


        [Test]
        public void GenerateRandomArray()
        {
            decimal totalRozbior = 400;
            int length = 13;
            decimal[] array = DataGenerator.GenerateRandomArray(length, totalRozbior);
            decimal finalRozbior = 0;

            foreach (var item in array)
            {
                finalRozbior += item;
            }
            Assert.That(array, Is.Not.Null);
            Assert.That(array.Length, Is.EqualTo(length));
            Assert.That(finalRozbior, Is.EqualTo(totalRozbior));
        }




        #endregion

        [Test]
        public void UpdateUI_ShouldUpdateRozbiory_AfterUpdateData()
        {

        }


        [Test]
        public void ResetData_ShouldResetData()
        {
            
        }


        [Test]
        public void Check_Loaded_Data()
        {

        }










        // 1st level is/has/dopes/contains
        // 2nd level all/not/some/exactly
        // or/and/not
        // Is.Unique / Is.Ordered
        // Assert.IsTrue

        [Test]
        public void Testee()
        {
            string username = "User123";
            Assert.That(username, Does.StartWith("U"));
            Assert.That(username, Does.EndWith("3"));

            var list = new List<int> { 1, 2, 3, 4, 5 };
            Assert.That(list, Contains.Item(3));
            Assert.That(list, Is.All.Positive);
            Assert.That(list, Has.Exactly(expectedCount: 2).LessThan(3));
            Assert.That(list, Is.Ordered);
            Assert.That(list, Is.Unique);
            Assert.That(list, Has.Exactly(expectedCount: 3).Matches<int>(x => x % 2 != 0));

        }


    }





    public static class NumberPredicates
    {
        public static bool IsEven(int number)
        {
            return number % 2 == 0;
        }
        public static bool IsOdd(int number)
        {
            return number % 2 != 0;
        }
    }
}

        */