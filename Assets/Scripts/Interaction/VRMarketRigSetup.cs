// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VRMarketRigSetup.cs" company="VR Market Scene">
//   Copyright (c) 2026 Animal Crossing Stylized VR Village Project. All rights reserved.
// </copyright>
// <summary>
//   Master procedural PC VR Rig generator (Milestone 5 / F17, F18, F19).
//   Configures XR Origin, CharacterController locomotion, ContinuousMoveProvider,
//   SnapTurnProvider, TeleportationProvider, and Dual Near/Far Hand Interactors (Direct + Ray).
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace AnimalCrossing.Interaction
{
    public static class VRMarketRigSetup
    {
        public const string InputActionsAssetPath = "Assets/InputSystem_Actions.inputactions";

        /// <summary>
        /// Instantiates and configures a complete PC VR player rig in the scene.
        /// </summary>
        public static GameObject CreateOrConfigureRig(Transform parent = null, Vector3? spawnPos = null)
        {
            // Default position at (0, 0, -3) facing North (Quaternion.identity) per Milestone 5 requirements
            Vector3 pos = spawnPos ?? new Vector3(0f, 0f, -3.0f);

            // Check if existing XR Origin exists
            var existingOrigin = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
            if (existingOrigin != null)
            {
                existingOrigin.transform.position = pos;
                existingOrigin.transform.rotation = Quaternion.identity;
                ConfigureRigComponents(existingOrigin.gameObject);
                return existingOrigin.gameObject;
            }

            GameObject rigRoot = new GameObject("XR Origin (VR)");
            if (parent != null) rigRoot.transform.SetParent(parent, false);
            rigRoot.transform.position = pos;
            rigRoot.transform.rotation = Quaternion.identity;

            ConfigureRigComponents(rigRoot);

            Debug.Log("[VRMarketRigSetup] Complete PC VR Player Rig successfully configured.");
            return rigRoot;
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null)
            {
                comp = go.AddComponent<T>();
            }
            return comp;
        }

        public static void ConfigureRigComponents(GameObject rigRoot)
        {
            // 1. XR Origin
            var xrOrigin = GetOrAddComponent<XROrigin>(rigRoot);
            xrOrigin.Origin = rigRoot;
            xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            // 2. Camera Offset
            Transform cameraOffset = rigRoot.transform.Find("Camera Offset");
            if (cameraOffset == null)
            {
                var camOffsetGo = new GameObject("Camera Offset");
                camOffsetGo.transform.SetParent(rigRoot.transform, false);
                cameraOffset = camOffsetGo.transform;
            }
            cameraOffset.localPosition = Vector3.zero;
            cameraOffset.localRotation = Quaternion.identity;
            xrOrigin.CameraFloorOffsetObject = cameraOffset.gameObject;

            // 3. Main Camera (HMD)
            Transform mainCam = cameraOffset.Find("Main Camera");
            GameObject camGo;
            if (mainCam == null)
            {
                camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                camGo.transform.SetParent(cameraOffset, false);
                mainCam = camGo.transform;
            }
            else
            {
                camGo = mainCam.gameObject;
                camGo.tag = "MainCamera";
            }
            camGo.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            camGo.transform.localRotation = Quaternion.identity;

            Camera cam = GetOrAddComponent<Camera>(camGo);
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 1000f;

            if (camGo.GetComponent<AudioListener>() == null)
            {
                camGo.AddComponent<AudioListener>();
            }

            var driver = GetOrAddComponent<TrackedPoseDriver>(camGo);
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;

            xrOrigin.Camera = cam;

            // 4. CharacterController for physical body
            // Explicitly attach CharacterController BEFORE configuring or accessing it!
            var cc = GetOrAddComponent<CharacterController>(rigRoot);
            cc.height = 1.8f;
            cc.radius = 0.30f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.30f;
            cc.skinWidth = 0.02f;
            cc.minMoveDistance = 0.001f;
            cc.slopeLimit = 45.0f;

            // 5. Locomotion Mediator & Body Transformer
            var bodyTransformer = GetOrAddComponent<XRBodyTransformer>(rigRoot);
            bodyTransformer.xrOrigin = xrOrigin;

            var mediator = GetOrAddComponent<LocomotionMediator>(rigRoot);
            if (Application.isPlaying)
            {
                mediator.xrOrigin = xrOrigin;
            }

            // 6. Locomotion Providers
            var moveProvider = GetOrAddComponent<ContinuousMoveProvider>(rigRoot);
            moveProvider.moveSpeed = 2.4f;
            moveProvider.enableFly = false;
            moveProvider.mediator = mediator;

            var turnProvider = GetOrAddComponent<SnapTurnProvider>(rigRoot);
            turnProvider.turnAmount = 45.0f;
            turnProvider.debounceTime = 0.25f;
            turnProvider.mediator = mediator;

            var teleportProvider = GetOrAddComponent<TeleportationProvider>(rigRoot);
            teleportProvider.mediator = mediator;

            // PC Fallback Movement (WASD + Mouse Look in Game view without VR headset)
            var pcCtrl = GetOrAddComponent<AnimalCrossing.Locomotion.SimplePCController>(rigRoot);
            pcCtrl.moveSpeed = 2.4f;

            // 7. Controllers / Hands Setup
            SetupHand(cameraOffset, "Left Hand", isLeft: true);
            SetupHand(cameraOffset, "Right Hand", isLeft: false);
        }

        private static GameObject SetupHand(Transform cameraOffset, string handName, bool isLeft)
        {
            Transform handTrans = cameraOffset.Find(handName);
            GameObject handGo;
            if (handTrans == null)
            {
                handGo = new GameObject(handName);
                handGo.transform.SetParent(cameraOffset, false);
                handGo.transform.localPosition = new Vector3(isLeft ? -0.20f : 0.20f, 1.2f, 0.35f);
            }
            else
            {
                handGo = handTrans.gameObject;
            }

            // Dual Near/Far Interaction: Direct Interactor + Ray Interactor
            Transform directChild = handGo.transform.Find("Direct Interactor");
            GameObject directGo = directChild != null ? directChild.gameObject : new GameObject("Direct Interactor");
            directGo.transform.SetParent(handGo.transform, false);

            var directCol = GetOrAddComponent<SphereCollider>(directGo);
            directCol.radius = 0.08f;
            directCol.isTrigger = true;

            var directInteractor = GetOrAddComponent<XRDirectInteractor>(directGo);

            Transform rayChild = handGo.transform.Find("Ray Interactor");
            GameObject rayGo = rayChild != null ? rayChild.gameObject : new GameObject("Ray Interactor");
            rayGo.transform.SetParent(handGo.transform, false);

            var rayInteractor = GetOrAddComponent<XRRayInteractor>(rayGo);
            rayInteractor.maxRaycastDistance = 15.0f;

            // Interaction Group to prioritize Direct over Ray
            var group = GetOrAddComponent<XRInteractionGroup>(handGo);
            group.startingGroupMembers = new List<UnityEngine.Object> { directInteractor, rayInteractor };

            return handGo;
        }

        /// <summary>
        /// Ensures TeleportationArea is attached and configured on plaza pavement/paths.
        /// </summary>
        public static void SetupTeleportationAreas()
        {
            // Remove TeleportationArea from sub-colliders so Ground is the primary 25m x 25m TeleportationArea (F17-05)
            var existingAreas = UnityEngine.Object.FindObjectsByType<TeleportationArea>(FindObjectsInactive.Include);
            foreach (var a in existingAreas)
            {
                if (a.gameObject.name != "Ground")
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(a);
                    else UnityEngine.Object.DestroyImmediate(a);
                }
            }

            var groundGo = GameObject.Find("Ground");
            if (groundGo != null)
            {
                var area = GetOrAddComponent<TeleportationArea>(groundGo);
                area.teleportTrigger = BaseTeleportationInteractable.TeleportTrigger.OnDeactivated;
            }
        }

        public static bool VerifyRigInCurrentScene(out string error)
        {
            var origin = UnityEngine.Object.FindAnyObjectByType<XROrigin>();
            if (origin == null)
            {
                error = "XROrigin not found in scene.";
                return false;
            }
            var cc = origin.GetComponent<CharacterController>();
            if (cc == null)
            {
                error = "CharacterController not found on XROrigin.";
                return false;
            }
            if (Mathf.Abs(cc.height - 1.8f) > 0.05f)
            {
                error = $"CharacterController height is {cc.height}, expected ~1.8m.";
                return false;
            }
            var move = origin.GetComponent<ContinuousMoveProvider>();
            if (move == null)
            {
                error = "ContinuousMoveProvider not found on XROrigin.";
                return false;
            }
            var turn = origin.GetComponent<SnapTurnProvider>();
            if (turn == null)
            {
                error = "SnapTurnProvider not found on XROrigin.";
                return false;
            }
            var teleport = origin.GetComponent<TeleportationProvider>();
            if (teleport == null)
            {
                error = "TeleportationProvider not found on XROrigin.";
                return false;
            }
            var directs = UnityEngine.Object.FindObjectsByType<XRDirectInteractor>();
            if (directs.Length < 2)
            {
                error = $"Expected at least 2 XRDirectInteractors, found {directs.Length}.";
                return false;
            }
            var rays = UnityEngine.Object.FindObjectsByType<XRRayInteractor>();
            if (rays.Length < 2)
            {
                error = $"Expected at least 2 XRRayInteractors, found {rays.Length}.";
                return false;
            }
            var teleportArea = UnityEngine.Object.FindAnyObjectByType<TeleportationArea>();
            if (teleportArea == null)
            {
                error = "TeleportationArea not found in scene.";
                return false;
            }
            error = null;
            return true;
        }

#if UNITY_EDITOR
        [MenuItem("AnimalCrossing/Setup PC VR Rig", priority = 30)]
        public static void MenuItem_SetupRig()
        {
            var rig = CreateOrConfigureRig();
            SetupTeleportationAreas();
            Selection.activeGameObject = rig;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[VRMarketRigSetup] Setup PC VR Rig complete and active scene saved.");
        }

        [MenuItem("AnimalCrossing/Setup PC VR Player Rig", priority = 31)]
        public static void MenuItem_SetupPlayerRig()
        {
            MenuItem_SetupRig();
        }

        [MenuItem("AnimalCrossing/Setup and Save Rig in All Scenes", priority = 32)]
        public static void SetupAndSaveBothScenes()
        {
            string[] scenePaths = new string[]
            {
                "Assets/Scenes/SampleScene.unity",
                "Assets/Scenes/AnimalCrossingMarketSquare.unity"
            };

            foreach (var path in scenePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    CreateOrConfigureRig();
                    if (GameObject.Find("TownSquarePlaza") == null)
                    {
                        var plazaGo = new GameObject("TownSquarePlaza");
                        plazaGo.AddComponent<AnimalCrossing.Environment.TownSquarePlazaBuilder>().BuildPlaza();
                    }
                    if (GameObject.Find("MarketStalls") == null)
                    {
                        AnimalCrossing.Environment.MarketStallBuilder.BuildAllStalls();
                    }
                    if (GameObject.Find("ShoppingBasket") == null)
                    {
                        ShoppingBasketBuilder.BuildShoppingBasket();
                    }
                    if (UnityEngine.Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsInactive.Include).Length < 11)
                    {
                        MarketPropsBuilder.BuildAllProps();
                    }
                    SetupTeleportationAreas();
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log($"[VRMarketRigSetup] Saved configured VR Rig and props into {path}");
                }
            }
        }

        public static void SetupAndSaveBothScenesBatch()
        {
            SetupAndSaveBothScenes();
            Debug.Log("[VRMarketRigSetup] Batch scene setup and save complete.");
            EditorApplication.Exit(0);
        }
#endif
    }
}
