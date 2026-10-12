#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace AnimalCrossing.Interaction.Editor
{
    /// <summary>
    /// Explicit one-click batch builder & scene synchronizer.
    /// (Replaces automatic [InitializeOnLoad] scene switching to ensure instant C# domain reloads).
    /// </summary>
    public static class AutoRigAndSceneSaver
    {
        [MenuItem("AnimalCrossing/Rebuild and Save All Scenes", priority = 1)]
        public static void ExecuteAutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying) return;
            try
            {
                VRMarketRigSetup.SetupAndSaveBothScenes();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AutoRigAndSceneSaver] Failed to setup and save scenes: {ex}");
            }
        }
    }
}
#endif
