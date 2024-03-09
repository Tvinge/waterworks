using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

class Program
{
    [SerializeField] AppLogic appLogic;
    [SerializeField] GameObject wodociag;
    void Main()
    {

        // Przyk³adowa tablica
        float[] myArray = appLogic.rozbioryNaWezlach;

        // Œcie¿ka do pliku CSV
        string filePath = "output.csv";

        // Wywo³aj funkcjê eksportuj¹c¹ tablicê do pliku CSV
        ExportArrayToCSV(myArray, filePath);

        Console.WriteLine("Tablica zosta³a pomyœlnie zapisana do pliku CSV.");
    }

    static void ExportArrayToCSV(float[] array, string filePath)
    {
        // Utwórz nowy obiekt StreamWriter do zapisu do pliku
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            // Zapisz nag³ówek (opcjonalny)
            writer.WriteLine("Index,Value");

            // Zapisz ka¿dy element tablicy w formacie "index, value"
            for (int i = 0; i < array.Length; i++)
            {
                writer.WriteLine($"{i},{array[i]}");
            }
        }
    }
}