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

        // Przyk�adowa tablica
        float[] myArray = appLogic.rozbioryNaWezlach;

        // �cie�ka do pliku CSV
        string filePath = "output.csv";

        // Wywo�aj funkcj� eksportuj�c� tablic� do pliku CSV
        ExportArrayToCSV(myArray, filePath);

        Console.WriteLine("Tablica zosta�a pomy�lnie zapisana do pliku CSV.");
    }

    static void ExportArrayToCSV(float[] array, string filePath)
    {
        // Utw�rz nowy obiekt StreamWriter do zapisu do pliku
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            // Zapisz nag��wek (opcjonalny)
            writer.WriteLine("Index,Value");

            // Zapisz ka�dy element tablicy w formacie "index, value"
            for (int i = 0; i < array.Length; i++)
            {
                writer.WriteLine($"{i},{array[i]}");
            }
        }
    }
}