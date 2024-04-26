using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class damage_calculator
    {
        [Test]
        public void useless_test50()
        {
            int finalDamage = DamageCalculator.CalculateDamage(amount:10, mitigationPercent:0.5f);

            Assert.AreEqual(expected: 5, actual: finalDamage);
        }
        [Test]
        public void useless_test80()
        {
            int finalDamage = DamageCalculator.CalculateDamage(amount: 10, mitigationPercent: 0.8f);

            Assert.AreEqual(expected: 2, actual: finalDamage);
        }
    }
}

