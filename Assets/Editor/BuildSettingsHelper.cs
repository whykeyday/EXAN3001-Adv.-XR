using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

[InitializeOnLoad]
public class BuildSettingsHelper
{
    static BuildSettingsHelper()
    {
        EditorApplication.delayCall += EnsureScenesInBuild;
    }

    [MenuItem("Tools/Add Scenes to Build")]
    public static void EnsureScenesInBuild()
    {
        string[] scenesToAdd = new string[]
        {
            "Assets/Scenes/OceanScene.unity",
            "Assets/Scenes/TreeScene.unity",
            "Assets/Scenes/CatScene.unity"
        };
        
        var currentScenes = EditorBuildSettings.scenes.ToList();
        bool changed = false;

        foreach (var scenePath in scenesToAdd)
        {
            if (!currentScenes.Any(s => s.path == scenePath))
            {
                // Verify file exists before adding
                if (System.IO.File.Exists(scenePath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
                {
                    currentScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                    Debug.Log($"[BuildSettingsHelper] Added {scenePath} to Build Settings.");
                    changed = true;
                }
                else
                {
                    Debug.LogWarning($"[BuildSettingsHelper] Could not find scene at {scenePath}. Make sure it exists.");
                }
            }
        }

        if (changed)
        {
            EditorBuildSettings.scenes = currentScenes.ToArray();
        }
    }
}
