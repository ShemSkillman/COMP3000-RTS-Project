using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class MissionSaveLoad
{
    private const string saveFileName = "scenarios.save"; //the name of the file where the scenario's info will be saved

    //mark a scenario as completed/uncompleted in the save file
    public static void SaveScenario (string scenarioCode, bool completed)
    {
        //goal is to look for a line where the input scenario code is written and overwrite that by...
        //...creating a temporary file, storing all lines but the line that contains the scenario code in the temp file...
        //...then adding the line with the scenario code
        string tempFile = Path.GetTempFileName();
        string fileName = Application.persistentDataPath + "/" + saveFileName;

        //make sure that the save file exists or create it
        if (!File.Exists(fileName))
            File.Create(fileName).Close();

        List<string> linesToKeep = File.ReadLines(fileName).Where(line => line.Substring(0, line.IndexOf(':')) != scenarioCode).ToList();

        int completedInt = completed ? 1 : 0;
        linesToKeep.Add($"{scenarioCode}:{completedInt}");

        File.WriteAllLines(tempFile, linesToKeep);

        File.Delete(fileName);
        File.Move(tempFile, fileName);
    }

    //fetches the scenario save file for completed/uncompleted saved scenarios
    public static Dictionary<string, bool> LoadScenarios ()
    {
        Dictionary<string, bool> retDic = new Dictionary<string, bool>();

        string fileName = Application.persistentDataPath + "/" + saveFileName;

        //make sure the file exists
        if(File.Exists(fileName))
        {
            //each line represents a saved scenario state with 0 being uncompleted and 1 being completed
            foreach(string line in File.ReadLines(fileName))
            {
                string[] lineSplit = line.Split(':');
                retDic.Add(lineSplit[0], System.Int32.Parse(lineSplit[1]) == 1);
            }
        }

        return retDic;
    }

    /// <summary>
    /// Unlocks a scenario so that it becomes playable.
    /// </summary>
    /// <param name="scenarioCode">The unique code of the scenario to unlock.</param>
    public static void UnlockScenario (string scenarioCode)
    {
        SaveScenario(scenarioCode, true);
    }

    //clears the saved progress of scenarios
    public static void ClearSavedScenarios ()
    {
        string fileName = Application.persistentDataPath + "/" + saveFileName;

        //make sure the save file exists
        if(File.Exists(fileName))
        {
            List<string> lines = new List<string>();

            //each saved scenario will now be marked as uncompleted
            using (StreamReader sr = new StreamReader(fileName))
            {
                while (sr.EndOfStream == false)
                {
                    string nextLine = sr.ReadLine();
                    lines.Add(nextLine.Substring(0, nextLine.IndexOf(':')) + ":0");
                }
            }

            //clear the save file content and add the new saved scenarios info
            using (StreamWriter sw = new StreamWriter(File.Create(fileName)))
            {
                foreach (string line in lines)
                    sw.WriteLine(line);
            }
        }
    }
}
