using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class QTableManager
{
    public static void saveQTable(float[,,] qTable, string filename)
    {
        string qTableFilePath = Application.persistentDataPath + "/" + filename;

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(qTableFilePath, FileMode.Create);

        formatter.Serialize(stream, qTable);
        stream.Close();
    }

    public static float[,,] loadQTable(string filename)
    {
        string qTableFilePath = Application.persistentDataPath + "/" + filename;

        if (File.Exists(qTableFilePath))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(qTableFilePath, FileMode.Open);

            float[,,] qTable = formatter.Deserialize(stream) as float[,,];
            stream.Close();

            return qTable;
        }
        else
        {
            return null;
        }
    }
}

public static class ExportCSVData
{
    public static void exportToCSV(string filename, List<float> qvalues, List<int> steps)
    {
        StreamWriter sw = new StreamWriter(filename);
        sw.WriteLine("qvalue,steps");
        for (int i = 0; i < qvalues.Count; i++)
        {
            sw.WriteLine(qvalues[i] + "," + steps[i]);
        }
        sw.Close();
    }
}
