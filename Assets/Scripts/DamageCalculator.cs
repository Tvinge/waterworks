using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DamageCalculator : MonoBehaviour
{
    public static int CalculateDamage(int amount, float mitigationPercent)
    {
        return Convert.ToInt32(amount - amount * mitigationPercent);
    }

    public static int CalculateDamage(int amount, ICharacter character)
    {
        int totalArmor = character.Inventory.GetTotalArmor() + character.Level * 10;
        float multiplayer = 100f - totalArmor;
        multiplayer /= 100f;
        return Convert.ToInt32(amount * multiplayer);
    }
}
