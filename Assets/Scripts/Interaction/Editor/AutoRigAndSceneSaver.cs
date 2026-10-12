#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AnimalCrossing.Interaction.Editor
{
    /// <summary>
    /// Executes on domain reload to automatically populate and save the complete PC VR Player Rig
    /// into both AnimalCrossingMarketSquare.unity and SampleScene.unity.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoRigAndSceneSaver
    {
        static AutoRigAndSceneSaver()
        {
            EditorApplication.delayCall += ExecuteAutoSetup;
        }

        private static void ExecuteAutoSetup()
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
