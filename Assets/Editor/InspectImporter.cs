using UnityEngine;
using UnityEditor;
using System.IO;

public class InspectImporter
{
    public static void Run()
    {
        string modelPath = "Assets/Models/KenneyCharacters/Model/characterMedium.fbx";
        string runPath = "Assets/Models/KenneyCharacters/Animations/run.fbx";
        
        ModelImporter modelImp = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        ModelImporter runImp = AssetImporter.GetAtPath(runPath) as ModelImporter;
        
        string report = "IMPORTER_REPORT:" + System.Environment.NewLine;
        if (modelImp != null)
        {
            report += "Model animationType: " + modelImp.animationType + System.Environment.NewLine;
            report += "Model avatar: " + (modelImp.sourceAvatar != null ? modelImp.sourceAvatar.name : "null") + System.Environment.NewLine;
        }
        if (runImp != null)
        {
            report += "Run animationType: " + runImp.animationType + System.Environment.NewLine;
            report += "Run avatar: " + (runImp.sourceAvatar != null ? runImp.sourceAvatar.name : "null") + System.Environment.NewLine;
        }
        
        File.WriteAllText("importer_report.txt", report);
    }
}
