using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NSubstitute;
using NUnit.Framework;

namespace Tests
{
    public class CalculationTestsForDefaultData : MonoBehaviour
    {
        CalculationManager calculationManager;
        
        [Test]
        public void calculateQhmax()
        {
            //IDataVersion defaultVersion = Substitute.For<IDataVersion>();
            DataVersion realDefaultVersion = DataVersion.CreateDefault();
            //defaultVersion.nodesRozbiory.Returns(realDefaultVersion.nodesRozbiory);
            //defaultVersion.pipesRozbiory.Returns(realDefaultVersion.pipesRozbiory);

            float value = AppLogic.CalculateQhmax(realDefaultVersion);
            Assert.AreEqual(expected: 282, actual: value);
        }

        [Test]
        public void calculateQzbiornika()
        {
            DataVersion realDefaultVersion = DataVersion.CreateDefault();

            float value = AppLogic.CalculateQzbiornika(realDefaultVersion);

            Assert.AreEqual(expected: 94, actual: value);
        }

        [Test]
        public void calculateOutflowOnPipe()
        {
            DataVersion realDefaultVersion = DataVersion.CreateDefault();
            int pipeId = 0;
            realDefaultVersion.pipesInflows[pipeId] = 188;

            float value = CalculationManager.CalculateOutflowOnPipe(realDefaultVersion, pipeId);

            Assert.AreEqual(expected: 188, actual: value);
        }

        [Test]
        public void addupInflows()
        {
            DataVersion realDefaultVersion = DataVersion.CreateDefault();
            int pipeIndex = 0;
            realDefaultVersion.doubleInflowsOnPipes[pipeIndex][0] = 100;
            realDefaultVersion.doubleInflowsOnPipes[pipeIndex][1] = 100;

            float[] expectedValues = new float[9];
            expectedValues[pipeIndex] = realDefaultVersion.doubleInflowsOnPipes[pipeIndex][0] + realDefaultVersion.doubleInflowsOnPipes[pipeIndex][1];

            float[] values = CalculationManager.AddupInflows(realDefaultVersion, pipeIndex);

            Assert.AreEqual(expected: expectedValues, actual: values);
        }






    }

}

