using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OfficeOpenXml;
using System.IO;

public enum InputDataSet
{
    RozbioryWezlow,
    RozbioryOdcinkow,
    WysokosciWezlow,
    DlugosciOdcinkow,
    WysokoscZabudowy,
    ZasilanieZPompowni,
    Wspolczynnik
}

public class DataStorageVariants
{


    public float[] RozbioryWezlow { get; private set; }
    public float[] RozbioryOdcinkow { get; private set; }
    public float[] WysokosciWezlow { get; private set; }
    public float[] DlugosciOdcinkow { get; private set; }
    public float[] WysokoscZabudowy { get; private set; }
    public float ZasilanieZPompowni { get; private set; }
    public float Wspolczynnik { get; private set; }



    public DataStorageVariants(float[] rozbioryWezlow, float[] rozbioryOdcinkow, float[] wysokosciWezlow,
                        float[] dlugosciOdcinkow, float[] wysokoscZabudowy, float zasilanieZPompowni,
                        float wspolczynnik)
    {
        RozbioryWezlow = rozbioryWezlow;
        RozbioryOdcinkow = rozbioryOdcinkow;
        WysokosciWezlow = wysokosciWezlow;
        DlugosciOdcinkow = dlugosciOdcinkow;
        WysokoscZabudowy = wysokoscZabudowy;
        ZasilanieZPompowni = zasilanieZPompowni;
        Wspolczynnik = wspolczynnik;
    }

    public static DataStorageVariants CreateDataSet1()
    {
        return new DataStorageVariants(
            new float[] { 0f, 17f, 12f, 23f, 26f, 29f, 30f, 0f },
            new float[] { 0f, 21f, 25f, 11f, 32f, 15f, 26f, 15f, 0f },
            new float[] { 145f, 147f, 146f, 151f, 154f, 159f, 168f, 192f },
            new float[] { 150f, 400f, 350f, 320f, 290f, 300f, 315f, 290f, 250f },
            new float[] { 0f, 20f, 25f, 15f, 20f, 15f, 15f, 15f, 0f },
            188f,
            1.75f
        );
    }
    /*
    public static DataStorageVariants CreateDataSet2()
    {
        return new DataStorageVariants(
            new float[] { 0f, 17f, 12f, 23f, 26f, 29f, 30f, 0f },
            new float[] { 0f, 21f, 25f, 11f, 32f, 15f, 26f, 15f, 0f },
            new float[] { 145f, 147f, 146f, 151f, 154f, 159f, 168f, 192f },
            new float[] { 150f, 400f, 350f, 320f, 290f, 300f, 315f, 290f, 250f },
            new float[] { 0f, 20f, 25f, 15f, 20f, 15f, 15f, 15f, 0f },
            188f,
            1.75f
        );
    }*/

}
