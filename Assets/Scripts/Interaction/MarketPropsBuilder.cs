// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MarketPropsBuilder.cs" company="VR Market Scene">
//   Copyright (c) 2026 Animal Crossing Stylized VR Village Project. All rights reserved.
// </copyright>
// <summary>
//   Master procedural generator for the 11 Animal Crossing grabbable items (Milestone 4 / F12, F13, F14).
//   Constructs items with 100% primitive compound colliders (Box, Sphere, Capsule; strictly zero dynamic MeshCollider),
//   ContinuousDynamic interpolation rigidbodies, and ergonomic child AttachTransforms.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using AnimalCrossing.TechnicalArt;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnimalCrossing.Interaction
{
    /// <summary>
    /// Procedural generator for all 11 grabbable market goods.
    /// Strictly complies with features F12, F13, F14, T3_10, and Scenarios 1-3.
    /// </summary>
    public static class MarketPropsBuilder
    {
        public const string OpaqueMatPath = "Assets/Materials/M_AnimalCrossing_Opaque.mat";
        public const string LiquidGlassMatPath = "Assets/Materials/M_AnimalCrossing_LiquidGlass.mat";

        private static Material _cachedOpaqueMat;
        public static Material OpaqueMaterial
        {
            get
            {
                if (_cachedOpaqueMat == null)
                {
#if UNITY_EDITOR
                    _cachedOpaqueMat = AssetDatabase.LoadAssetAtPath<Material>(OpaqueMatPath);
#endif
                }
                return _cachedOpaqueMat;
            }
            set => _cachedOpaqueMat = value;
        }

        private static Material _cachedLiquidGlassMat;
        public static Material LiquidGlassMaterial
        {
            get
            {
                if (_cachedLiquidGlassMat == null)
                {
#if UNITY_EDITOR
                    _cachedLiquidGlassMat = AssetDatabase.LoadAssetAtPath<Material>(LiquidGlassMatPath);
#endif
                }
                return _cachedLiquidGlassMat;
            }
            set => _cachedLiquidGlassMat = value;
        }

        // =========================================================================
        // Master Assembly API: BuildAllProps
        // =========================================================================

        public static GameObject BuildAllProps(Transform parent = null)
        {
            // Destroy any existing grabbable props on stalls to maintain idempotency
            string[] propPrefixes = { "Apple", "Orange", "Peach", "Turnip", "CoffeeMug", "Donut", "Teapot", "SlicedBread", "WateringCan", "PottedSapling", "SeedPacket" };
            var allGrabs = UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include);
            foreach (var grab in allGrabs)
            {
                if (grab == null) continue;
                string gName = grab.gameObject.name;
                if (gName.Contains("ShoppingBasket") || gName.Contains("Origin") || gName.Contains("Controller")) continue;
                foreach (var prefix in propPrefixes)
                {
                    if (gName.StartsWith(prefix))
                    {
                        if (Application.isPlaying)
                            UnityEngine.Object.Destroy(grab.gameObject);
                        else
                            UnityEngine.Object.DestroyImmediate(grab.gameObject);
                        break;
                    }
                }
            }

            GameObject propsGroup = new GameObject("MarketProps");
            if (parent != null)
                propsGroup.transform.SetParent(parent, false);

            propsGroup.transform.localPosition = Vector3.zero;
            propsGroup.transform.localRotation = Quaternion.identity;

            // Locate existing stalls for proper resting positions
            GameObject produceStall = GameObject.Find("ProduceStall");
            GameObject cafeStall = GameObject.Find("CafeStall");
            GameObject craftingStall = GameObject.Find("CraftingStall");

            // 1. Produce Stall Props (Apples, Oranges, Peaches, Turnips)
            Transform prodParent = produceStall != null ? produceStall.transform : propsGroup.transform;
            // Apples: Fruit tray trio + crate display
            GameObject apple = BuildApple(new Vector3(-0.65f, 0.951f, -0.15f), Quaternion.identity, prodParent, "Apple");
            BuildApple(new Vector3(-0.50f, 0.951f, -0.15f), Quaternion.Euler(0f, 15f, 0f), prodParent, "Apple_02");
            BuildApple(new Vector3(-0.58f, 0.951f, -0.03f), Quaternion.Euler(0f, -25f, 0f), prodParent, "Apple_03");
            BuildApple(new Vector3(-0.60f, 1.090f, 0.00f), Quaternion.identity, prodParent, "Apple_04");

            // Oranges: Tier 2 middle display crate trio
            GameObject orange = BuildOrange(new Vector3(-0.25f, 1.090f, 0.00f), Quaternion.identity, prodParent, "Orange");
            BuildOrange(new Vector3(-0.12f, 1.090f, 0.00f), Quaternion.Euler(0f, 30f, 0f), prodParent, "Orange_02");
            BuildOrange(new Vector3(-0.185f, 1.090f, 0.11f), Quaternion.Euler(0f, -40f, 0f), prodParent, "Orange_03");

            // Peaches: Tier 2 middle display crate trio
            GameObject peach = BuildPeach(new Vector3(0.15f, 1.090f, 0.00f), Quaternion.identity, prodParent, "Peach");
            BuildPeach(new Vector3(0.30f, 1.090f, 0.00f), Quaternion.Euler(0f, -20f, 0f), prodParent, "Peach_02");
            BuildPeach(new Vector3(0.225f, 1.090f, 0.13f), Quaternion.Euler(0f, 45f, 0f), prodParent, "Peach_03");

            // Turnips: Scale turnip + counter deck harvest bunch
            GameObject turnip = BuildTurnip(new Vector3(0.65f, 0.951f, -0.15f), Quaternion.Euler(0f, 45f, 0f), prodParent, "Turnip");
            BuildTurnip(new Vector3(0.46f, 0.951f, -0.15f), Quaternion.Euler(0f, 35f, 0f), prodParent, "Turnip_02");
            BuildTurnip(new Vector3(0.55f, 0.951f, 0.02f), Quaternion.Euler(0f, -30f, 0f), prodParent, "Turnip_03");

            // 2. Café Stall Props (Donuts, Bread, Teapot, Coffee Mugs)
            Transform cafeParent = cafeStall != null ? cafeStall.transform : propsGroup.transform;
            // Donuts: 4 gourmet flavors in a 2x2 grid inside pastry showcase
            GameObject donut = BuildDonut(new Vector3(-0.70f, 0.984f, -0.38f), Quaternion.identity, cafeParent, "Donut", Swatches.DonutGlazeStrawberry);
            BuildDonut(new Vector3(-0.50f, 0.984f, -0.38f), Quaternion.Euler(0f, 15f, 0f), cafeParent, "Donut_Choco", Swatches.ChocolateFrostingCocoa);
            BuildDonut(new Vector3(-0.70f, 0.984f, -0.22f), Quaternion.Euler(0f, -10f, 0f), cafeParent, "Donut_Matcha", Swatches.MatchaGlazeGreen);
            BuildDonut(new Vector3(-0.50f, 0.984f, -0.22f), Quaternion.Euler(0f, 25f, 0f), cafeParent, "Donut_Vanilla", Swatches.VanillaFondantCream);

            // Teapot
            GameObject teapot = BuildTeapot(new Vector3(-0.25f, 0.952f, -0.05f), Quaternion.Euler(0f, -30f, 0f), cafeParent, "Teapot");

            // Sliced Bread: Artisanal sliced loaves on rear counter deck
            GameObject slicedBread = BuildSlicedBread(new Vector3(0.15f, 0.953f, 0.08f), Quaternion.identity, cafeParent, "SlicedBread");
            BuildSlicedBread(new Vector3(0.15f, 0.953f, -0.03f), Quaternion.Euler(0f, -10f, 0f), cafeParent, "SlicedBread_02");
            BuildSlicedBread(new Vector3(0.28f, 0.953f, 0.02f), Quaternion.Euler(0f, 20f, 0f), cafeParent, "SlicedBread_03");

            // Coffee Mugs: Mint ceramic on saucer + Ivory porcelain on counter
            GameObject coffeeMug = BuildCoffeeMug(new Vector3(0.50f, 0.953f, -0.28f), Quaternion.identity, cafeParent, "CoffeeMug", Swatches.DinerMintSaucer, Swatches.CoffeeDarkEspresso);
            BuildCoffeeMug(new Vector3(0.32f, 0.953f, -0.28f), Quaternion.Euler(0f, 35f, 0f), cafeParent, "CoffeeMug_02", Swatches.PorcelainIvoryMug, Swatches.CafeAuLaitCoffee);

            // Pre-socket Coffee Mug onto CoffeeSaucerSocket if present (Scenario 3 contract)
            var saucerSocketGo = GameObject.Find("CoffeeSaucerSocket");
            if (saucerSocketGo != null && coffeeMug != null)
            {
                var socket = saucerSocketGo.GetComponent<XRSocketInteractor>();
                var mugInteractable = coffeeMug.GetComponent<XRGrabInteractable>();
                if (socket != null && mugInteractable != null)
                {
                    socket.startingSelectedInteractable = mugInteractable;
                }
            }

            // 3. Crafting Stall Props (Watering Can, Potted Saplings, Seed Packets)
            Transform craftParent = craftingStall != null ? craftingStall.transform : propsGroup.transform;
            // Watering Can
            GameObject wateringCan = BuildWateringCan(new Vector3(0.55f, 0.920f, 0.00f), Quaternion.identity, craftParent, "WateringCan");

            // Potted Saplings: Nursery trio on potting mat
            GameObject pottedSapling = BuildPottedSapling(new Vector3(-0.30f, 0.931f, -0.15f), Quaternion.identity, craftParent, "PottedSapling");
            BuildPottedSapling(new Vector3(-0.48f, 0.931f, -0.15f), Quaternion.Euler(0f, 25f, 0f), craftParent, "PottedSapling_02");
            BuildPottedSapling(new Vector3(-0.39f, 0.931f, 0.05f), Quaternion.Euler(0f, -40f, 0f), craftParent, "PottedSapling_03");

            // Seed Packets: Tidy row of 3 seed varieties on workbench (clear of potting mat)
            GameObject seedPacket = BuildSeedPacket(new Vector3(0.08f, 0.920f, -0.10f), Quaternion.identity, craftParent, "SeedPacket", Swatches.SeedPacketPrintYellow);
            BuildSeedPacket(new Vector3(0.20f, 0.920f, -0.10f), Quaternion.Euler(0f, 10f, 0f), craftParent, "SeedPacket_Tomato", Swatches.SeedPacketPrintRed);
            BuildSeedPacket(new Vector3(0.32f, 0.920f, -0.10f), Quaternion.Euler(0f, -10f, 0f), craftParent, "SeedPacket_Herbs", Swatches.SeedPacketPrintGreen);

            // Clean up empty propsGroup if props were parented to stalls
            if (produceStall != null && propsGroup != null && propsGroup.transform.childCount == 0)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(propsGroup);
                else
                    UnityEngine.Object.DestroyImmediate(propsGroup);
                propsGroup = apple;
            }

            Debug.Log("[MarketPropsBuilder] Realistic market inventory (30 props across 3 stalls) successfully generated.");
            return propsGroup;
        }

        // =========================================================================
        // Physics & Interaction Root Setup
        // =========================================================================

        private static (Rigidbody, XRGrabInteractable, Transform) SetupGrabbableRoot(
            GameObject go, float mass, float drag = 0.5f, float angularDrag = 0.8f)
        {
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = Mathf.Clamp(mass, 0.10f, 5.00f);
            rb.linearDamping = drag;
            rb.angularDamping = angularDrag;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.useGravity = true;

            var grab = go.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.throwOnDetach = true;
            grab.throwVelocityScale = 1.5f;
            grab.throwAngularVelocityScale = 1.0f;

            GameObject attachGo = new GameObject("AttachTransform");
            attachGo.transform.SetParent(go.transform, false);
            grab.attachTransform = attachGo.transform;

            return (rb, grab, attachGo.transform);
        }

        // =========================================================================
        // 1. Apple (Produce Stall)
        // =========================================================================

        public static GameObject BuildApple(Vector3 pos, Quaternion rot, Transform parent = null, string name = "Apple")
        {
            GameObject root = new GameObject(name);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = rot;

            SetupGrabbableRoot(root, 0.18f);

            // SphereCollider: radius 0.065m -> diameter 0.13m (F12-C01: 0.08m to 0.22m)
            var sphereCol = root.AddComponent<SphereCollider>();
            sphereCol.center = new Vector3(0f, 0.065f, 0f);
            sphereCol.radius = 0.065f;

            // Visual Hierarchy
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            var mf = visual.AddComponent<MeshFilter>();
            var mr = visual.AddComponent<MeshRenderer>();
            mr.sharedMaterial = OpaqueMaterial;

            var buffer = new ProceduralMeshBuilder.MeshBuffer();
            Vector2 uvBody = ProceduralMeshBuilder.GetSwatchUV(Swatches.FruitAppleRed.Col, Swatches.FruitAppleRed.Row);
            Vector2 uvStem = ProceduralMeshBuilder.GetSwatchUV(Swatches.FruitStemWoodBrown.Col, Swatches.FruitStemWoodBrown.Row);
            Vector2 uvLeaf = ProceduralMeshBuilder.GetSwatchUV(Swatches.TurnipLeafGreen.Col, Swatches.TurnipLeafGreen.Row);

            AddSphereToBuffer(buffer, new Vector3(0f, 0.065f, 0f), 0.065f, 10, 14, uvBody);
            AddCylinderToBuffer(buffer, new Vector3(0f, 0.130f, 0f), 0.005f, 0.035f, 6, uvStem);
            AddLeafQuadToBuffer(buffer, new Vector3(0.015f, 0.145f, 0f), 0.025f, 0.012f, uvLeaf);

            mf.sharedMesh = buffer.Build($"{name}_Mesh");

            // Attach Transform (Palm Grasp)
            var grab = root.GetComponent<XRGrabInteractable>();
            grab.attachTransform.localPosition = new Vector3(0f, 0.060f, -0.040f);
            grab.attachTransform.localRotation = Quaternion.identity;

            return root;
        }

        // =========================================================================
        // 2. Orange (Produce Stall)
        // =========================================================================

        public static GameObject BuildOrange(Vector3 pos, Quaternion rot, Transform parent = null, string name = "Orange")
        {
            GameObject root = new GameObject(name);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = rot;

            SetupGrabbableRoot(root, 0.20f);

            // SphereCollider: radius 0.062m -> diameter 0.124m (F12-C01)
            var sphereCol = root.AddComponent<SphereCollider>();
            sphereCol.center = new Vector3(0f, 0.062f, 0f);
            sphereCol.radius = 0.062f;

            // Visual Hierarchy
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            var mf = visual.AddComponent<MeshFilter>();
            var mr = visual.AddComponent<MeshRenderer>();
            mr.sharedMaterial = OpaqueMaterial;

            var buffer = new ProceduralMeshBuilder.MeshBuffer();
            Vector2 uvBody = ProceduralMeshBuilder.GetSwatchUV(Swatches.FruitOrangeCitrus.Col, Swatches.FruitOrangeCitrus.Row);
            Vector2 uvNavel = ProceduralMeshBuilder.GetSwatchUV(Swatches.FoliageShrubOlive.Col, Swatches.FoliageShrubOlive.Row);
            Vector2 uvLeaf = ProceduralMeshBuilder.GetSwatchUV(Swatches.FoliageHedgeJade.Col, Swatches.FoliageHedgeJade.Row);

            AddSphereToBuffer(buffer, new Vector3(0f, 0.062f, 0f), 0.062f, 10, 14, uvBody);
            AddSphereToBuffer(buffer, new Vector3(0f, 0.123f, 0f), 0.009f, 6, 8, uvNavel);
            AddLeafQuadToBuffer(buffer, new Vector3(0.012f, 0.126f, 0f), 0.022f, 0.010f, uvLeaf);

            mf.sharedMesh = buffer.Build($"{name}_Mesh");

            var grab = root.GetComponent<XRGrabInteractable>();
            grab.attachTransform.localPosition = new Vector3(0f, 0.058f, -0.040f);
            grab.attachTransform.localRotation = Quaternion.identity;

            return root;
        }

        // =========================================================================
        // 3. Peach (Produce Stall)
        // =========================================================================

        public static GameObject BuildPeach(Vector3 pos, Quaternion rot, Transform parent = null, string name = "Peach")
        {
            GameObject root = new GameObject(name);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = rot;

            SetupGrabbableRoot(root, 0.17f);

            // Compound Colliders: 2 Spheres forming cleft lobes (combined diameter 0.146m)
            var sphere1 = root.AddComponent<SphereCollider>();
            sphere1.center = new Vector3(-0.018f, 0.062f, 0f);
            sphere1.radius = 0.055f;

            var sphere2 = root.AddComponent<SphereCollider>();
            sphere2.center = new Vector3(0.018f, 0.062f, 0f);
            sphere2.radius = 0.055f;

            // Visual Hierarchy
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            var mf = visual.AddComponent<MeshFilter>();
            var mr = visual.AddComponent<MeshRenderer>();
            mr.sharedMaterial = OpaqueMaterial;

            var buffer = new ProceduralMeshBuilder.MeshBuffer();
            Vector2 uvBody = ProceduralMeshBuilder.GetSwatchUV(Swatches.FruitPeachBlushPink.Col, Swatches.FruitPeachBlushPink.Row);
            Vector2 uvStem = ProceduralMeshBuilder.GetSwatchUV(Swatches.FruitStemWoodBrown.Col, Swatches.FruitStemWoodBrown.Row);
            Vector2 uvLeaf = ProceduralMeshBuilder.GetSwatchUV(Swatches.FoliageHedgeLime.Col, Swatches.FoliageHedgeLime.Row);

            AddSphereToBuffer(buffer, new Vector3(-0.018f, 0.062f, 0f), 0.055f, 10, 12, uvBody);
            AddSphereToBuffer(buffer, new Vector3(0.018f, 0.062f, 0f), 0.055f, 10, 12, uvBody);
            AddCylinderToBuffer(buffer, new Vector3(0f, 0.115f, 0f), 0.005f, 0.030f, 6, uvStem);
            AddLeafQuadToBuffer(buffer, new Vector3(0.014f, 0.125f, 0f), 0.024f, 0.011f, uvLeaf);

            mf.sharedMesh = buffer.Build($"{name}_Mesh");

            var grab = root.GetComponent<XRGrabInteractable>();
            grab.attachTransform.localPosition = new Vector3(0f, 0.060f, -0.040f);
            grab.attachTransform.localRotation = Quaternion.identity;

            return root;
        }

        // =========================================================================
        // 4. Turnip (Produce Stall)
        // =========================================================================

        public static GameObject BuildTurnip(Vector3 pos, Quaternion rot, Transform parent = null, string name = "Turnip")
        {
            GameObject root = new GameObject(name);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = rot;

            SetupGrabbableRoot(root, 0.35f, 0.6f, 1.0f);

            // Compound Primitive Colliders: 1 Sphere + 2 Capsules (F12-C03, F13-C03)
            var bulbSphere = root.AddComponent<SphereCollider>();
            bulbSphere.center = new Vector3(0f, 0.090f, 0f);
            bulbSphere.radius = 0.068f;

            var rootCapsule = root.AddComponent<CapsuleCollider>();
            rootCapsule.center = new Vector3(0f, 0.045f, 0f);
            rootCapsule.radius = 0.035f;
            rootCapsule.height = 0.090f; // H >= 2R + 0.002m

            var foliageCapsule = root.AddComponent<CapsuleCollider>();
            foliageCapsule.center = new Vector3(0f, 0.170f, 0f);
            foliageCapsule.radius = 0.030f;
            foliageCapsule.height = 0.110f; // H >= 2R + 0.002m

            // Visual Hierarchy
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            var mf = visual.AddComponent<MeshFilter>();
            var mr = visual.AddComponent<MeshRenderer>();
            mr.sharedMaterial = OpaqueMaterial;

            var buffer = new ProceduralMeshBuilder.MeshBuffer();
            Vector2 uvBody = ProceduralMeshBuilder.GetSwatchUV(Swatches.TurnipCreamBody.Col, Swatches.TurnipCreamBody.Row);
            Vector2 uvTop = ProceduralMeshBuilder.GetSwatchUV(Swatches.TurnipVioletTop.Col, Swatches.TurnipVioletTop.Row);
            Vector2 uvLeaf = ProceduralMeshBuilder.GetSwatchUV(Swatches.TurnipLeafGreen.Col, Swatches.TurnipLeafGreen.Row);
            Vector2 uvTip = ProceduralMeshBuilder.GetSwatchUV(Swatches.RootTipCharcoal.Col, Swatches.RootTipCharcoal.Row);

            // Root bulb & top crown
            AddSphereToBuffer(buffer, new Vector3(0f, 0.080f, 0f), 0.068f, 10, 14, uvBody);
            AddSphereToBuffer(buffer, new Vector3(0f, 0.120f, 0f), 0.050f, 8, 12, uvTop);
            AddConeToBuffer(buffer, new Vector3(0f, 0.020f, 0f), 0.035f, 0.004f, 0.045f, 8, uvTip);

            // 3 Leafy Green Stalks
            AddLeafStalk(buffer, new Vector3(-0.015f, 0.150f, 0f), Quaternion.Euler(0f, 0f, 15f), 0.10f, uvLeaf);
            AddLeafStalk(buffer, new Vector3(0.015f, 0.150f, 0f), Quaternion.Euler(0f, 0f, -15f), 0.10f, uvLeaf);
            AddLeafStalk(buffer, new Vector3(0f, 0.150f, 0.015f), Quaternion.Euler(15f, 0f, 0f), 0.11f, uvLeaf);

            mf.sharedMesh = buffer.Build($"{name}_Mesh");

            var grab = root.GetComponent<XRGrabInteractable>();
            grab.attachTransform.localPosition = new Vector3(0f, 0.120f, -0.030f);
            grab.attachTransform.localRotation = Quaternion.Euler(15f, 0f, 0f);

            return root;
        }

        // =========================================================================
        // 5. Coffee Mug (Café Stall)
        // =========================================================================

        public static GameObject BuildCoffeeMug(Vector3 pos, Quaternion rot, Transform parent = null, string name = "CoffeeMug", SwatchCoord? ceramicSwatch = null, SwatchCoord? beverageSwatch = null)
        {
            GameObject root = new GameObject(name);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = rot;

            SetupGrabbableRoot(root, 0.28f);

            // Compound Primitive Colliders: 1 Capsule (Cup) + 1 Box (Handle)
            var cupCap = root.AddComponent<CapsuleCollider>();
            cupCap.center = new Vector3(0f, 0.048f, 0f);
            cupCap.radius = 0.046f;
            cupCap.height = 0.096f; // H >= 2R + 0.002m

            var handleBox = root.AddComponent<BoxCollider>();
            handleBox.center = new Vector3(0.062f, 0.048f, 0f);
            handleBox.size = new Vector3(0.024f, 0.055f, 0.018f); // all > 0.001m

            // Visual Hierarchy (Cup: Opaque, Liquid: LiquidGlass)
            GameObject cupVisual = new GameObject("CupVisual");
            cupVisual.transform.SetParent(root.transform, false);
            var mfCup = cupVisual.AddComponent<MeshFilter>();
            var mrCup = cupVisual.AddComponent<MeshRenderer>();
            mrCup.sharedMaterial = OpaqueMaterial;

            var bufferCup = new ProceduralMeshBuilder.MeshBuffer();
            SwatchCoord ceramic = ceramicSwatch ?? Swatches.DinerMintSaucer;
            Vector2 uvMug = ProceduralMeshBuilder.GetSwatchUV(ceramic.Col, ceramic.Row);
            AddHollowCylinderToBuffer(bufferCup, new Vector3(0f, 0.048f, 0f), 0.046f, 0.040f, 0.096f, 16, uvMug);
            AddTorusHandleToBuffer(bufferCup, new Vector3(0.048f, 0.048f, 0f), 0.025f, 0.008f, 12, 8, uvMug);
            mfCup.sharedMesh = bufferCup.Build($"{name}_Body");

            // Coffee Liquid Disc
            GameObject liquidVisual = new GameObject("LiquidVisual");
            liquidVisual.transform.SetParent(root.transform, false);
            var mfLiquid = liquidVisual.AddComponent<MeshFilter>();
            var mrLiquid = liquidVisual.AddComponent<MeshRenderer>();
            mrLiquid.sharedMaterial = LiquidGlassMaterial;

            var bufferLiquid = new ProceduralMeshBuilder.MeshBuffer();
            SwatchCoord beverage = beverageSwatch ?? Swatches.CoffeeDarkEspresso;
            Vector2 uvCoffee = ProceduralMeshBuilder.GetSwatchUV(beverage.Col, beverage.Row);
            AddDiscToBuffer(bufferLiquid, new Vector3(0f, 0.082f, 0f), 0.039f, 16, uvCoffee);
            mfLiquid.sharedMesh = bufferLiquid.Build($"{name}_Liquid");

            var grab = root.GetComponent<XRGrabInteractable>();
            grab.attachTransform.localPosition = new Vector3(0.062f, 0.048f, 0f);
            grab.attachTransform.localRotation = Quaternion.Euler(0f, -90f, 0f);

            return root;
        }

        // =========================================================================
        // 6. Donut (Café Stall)
        // =========================================================================

        public static GameObject BuildDonut(Vector3 pos, Quaternion rot, Transform parent = null, string name = "Donut", SwatchCoord? glazeSwatch = null)
        {
            GameObject root = new GameObject(name);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = rot;

            SetupGrabbableRoot(root, 0.12f);

            // 4 Compound CapsuleColliders enclosing torus ring
            float ringR = 0.052f;
            float capR = 0.020f;
            float capH = 0.075f;

            var colN = root.AddComponent<CapsuleCollider>();
            colN.center = new Vector3(0f, 0.022f, ringR);
            colN.radius = capR; colN.height = capH; colN.direction = 0; // X-axis

            var colS = root.AddComponent<CapsuleCollider>();
            colS.center = new Vector3(0f, 0.022f, -ringR);
            colS.radius = capR; colS.height = capH; colS.direction = 0;

            var colE = root.AddComponent<CapsuleCollider>();
            colE.center = new Vector3(ringR, 0.022f, 0f);
            colE.radius = capR; colE.height = capH; colE.direction = 2; // Z-axis

            var colW = root.AddComponent<CapsuleCollider>();
            colW.center = new Vector3(-ringR, 0.022f, 0f);
            colW.radius = capR; colW.height = capH; colW.direction = 2;

            // Visual Hierarchy
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var mf = visual.AddComponent<MeshFilter>();
            var mr = visual.AddComponent<MeshRenderer>();
            mr.sharedMaterial = OpaqueMaterial;

            var buffer = new ProceduralMeshBuilder.MeshBuffer();
            Vector2 uvDough = ProceduralMeshBuilder.GetSwatchUV(Swatches.DonutFriedGoldenTan.Col, Swatches.DonutFriedGoldenTan.Row);
            SwatchCoord glaze = glazeSwatch ?? Swatches.DonutGlazeStrawberry;
            Vector2 uvGlaze = ProceduralMeshBuilder.GetSwatchUV(glaze.Col, glaze.Row);

            AddTorusToBuffer(buffer, new Vector3(0f, 0.022f, 0f), ringR, 0.022f, 16, 10, uvDough);
            AddTorusTopHalfToBuffer(buffer, new Vector3(0f, 0.023f, 0f), ringR, 0.023f, 16, 8, uvGlaze);

            mf.sharedMesh = buffer.Build($"{name}_Mesh");

            var grab = root.GetComponent<XRGrabInteractable>();
            grab.attachTransform.localPosition = new Vector3(0f, 0.022f, -0.052f);
            grab.attachTransform.localRotation = Quaternion.Euler(-10f, 0f, 0f);

            return root;
        }

        // =========================================================================
        // 7. Teapot (Café Stall)
        // =========================================================================

        public static GameObject BuildTeapot(Vector3 pos, Quaternion rot, Transform parent = null, string name = "Teapot")
        {
            GameObject root = new GameObject(name);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = rot;

            SetupGrabbableRoot(root, 0.65f);

            // Compound Colliders: 2 Capsules + 2 Boxes (height ~ 0.20m, F12-C02: 0.15m to 0.35m)
            var footBox = root.AddComponent<BoxCollider>();
            footBox.center = new Vector3(0f, 0.015f, 0f);
            footBox.size = new Vector3(0.100f, 0.030f, 0.100f);

            var bellyCap = root.AddComponent<CapsuleCollider>();
            bellyCap.center = new Vector3(0f, 0.100f, 0f);
            bellyCap.radius = 0.078f;
            bellyCap.height = 0.165f; // H >= 2R + 0.002m

            var spoutCap = root.AddComponent<CapsuleCollider>();
            spoutCap.center = new Vector3(0f, 0.115f, 0.095f);
            spoutCap.radius = 0.018f;
            spoutCap.height = 0.080f;

            var handleBox = root.AddComponent<BoxCollider>();
            handleBox.center = new Vector3(0f, 0.110f, -0.095f);
            handleBox.size = new Vector3(0.024f, 0.090f, 0.035f);

            // Visual Hierarchy
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var mf = visual.AddComponent<MeshFilter>();
            var mr = visual.AddComponent<MeshRenderer>();
            mr.sharedMaterial = OpaqueMaterial;

            var buffer = new ProceduralMeshBuilder.MeshBuffer();
            Vector2 uvTeal = ProceduralMeshBuilder.GetSwatchUV(Swatches.TeapotRetroTeal.Col, Swatches.TeapotRetroTeal.Row);
            Vector2 uvGold = ProceduralMeshBuilder.GetSwatchUV(Swatches.BellBrassGold.Col, Swatches.BellBrassGold.Row);

            AddSphereToBuffer(buffer, new Vector3(0f, 0.100f, 0f), 0.080f, 10, 16, uvTeal);
            AddCylinderToBuffer(buffer, new Vector3(0f, 0.015f, 0f), 0.055f, 0.030f, 12, uvTeal); // Foot
            AddSphereToBuffer(buffer, new Vector3(0f, 0.185f, 0f), 0.016f, 6, 8, uvGold); // Lid knob
            // Spout
            AddCylinderAngledToBuffer(buffer, new Vector3(0f, 0.095f, 0.055f), new Vector3(0f, 0.145f, 0.115f), 0.016f, 0.012f, 8, uvTeal);
            // Handle Arch
            AddTorusHandleToBuffer(buffer, new Vector3(0f, 0.110f, -0.080f), 0.040f, 0.010f, 10, 6, uvTeal, true);

            mf.sharedMesh = buffer.Build($"{name}_Mesh");

            var grab = root.GetComponent<XRGrabInteractable>();
            grab.attachTransform.localPosition = new Vector3(0f, 0.120f, -0.095f);
            grab.attachTransform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            return root;
        }

        // =========================================================================
        // 8. Sliced Bread (Café Stall)
        // =========================================================================

        public static GameObject BuildSlicedBread(Vector3 pos, Quaternion rot, Transform parent = null, string name = "SlicedBread")
        {
            GameObject root = new GameObject(name);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = rot;

            SetupGrabbableRoot(root, 0.15f);

            // Compound Colliders: 1 Box (base) + 1 Capsule (arched crown)
            var baseBox = root.AddComponent<BoxCollider>();
            baseBox.center = new Vector3(0f, 0.040f, 0f);
            baseBox.size = new Vector3(0.110f, 0.080f, 0.032f);

            var crownCap = root.AddComponent<CapsuleCollider>();
            crownCap.center = new Vector3(0f, 0.080f, 0f);
            crownCap.radius = 0.020f;
            crownCap.height = 0.110f;
            crownCap.direction = 0; // along X

            // Visual Hierarchy
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var mf = visual.AddComponent<MeshFilter>();
            var mr = visual.AddComponent<MeshRenderer>();
            mr.sharedMaterial = OpaqueMaterial;

            var buffer = new ProceduralMeshBuilder.MeshBuffer();
            Vector2 uvCrumb = ProceduralMeshBuilder.GetSwatchUV(Swatches.BreadCrumbSoft.Col, Swatches.BreadCrumbSoft.Row);
            Vector2 uvCrust = ProceduralMeshBuilder.GetSwatchUV(Swatches.BreadCrustGolden.Col, Swatches.BreadCrustGolden.Row);

            AddBoxToBuffer(buffer, new Vector3(0f, 0.040f, 0f), new Vector3(0.110f, 0.080f, 0.032f), uvCrumb, uvCrust);
            AddHalfCylinderToBuffer(buffer, new Vector3(0f, 0.080f, 0f), 0.055f, 0.032f, 10, uvCrumb, uvCrust);

            mf.sharedMesh = buffer.Build($"{name}_Mesh");

            var grab = root.GetComponent<XRGrabInteractable>();
            grab.attachTransform.localPosition = new Vector3(0f, 0.060f, -0.018f);
            grab.attachTransform.localRotation = Quaternion.identity;

            return root;
        }

        // =========================================================================
        // 9. Watering Can (Crafting Stall)
        // =========================================================================

        public static GameObject BuildWateringCan(Vector3 pos, Quaternion rot, Transform parent = null, string name = "WateringCan")
        {
            GameObject root = new GameObject(name);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = rot;

            SetupGrabbableRoot(root, 0.85f);

            // Compound Colliders: 2 Capsules + 1 Box
            var bodyCap = root.AddComponent<CapsuleCollider>();
            bodyCap.center = new Vector3(0f, 0.075f, 0f);
            bodyCap.radius = 0.070f;
            bodyCap.height = 0.150f; // H >= 2R + 0.002m

            var spoutCap = root.AddComponent<CapsuleCollider>();
            spoutCap.center = new Vector3(0f, 0.115f, 0.115f);
            spoutCap.radius = 0.022f;
            spoutCap.height = 0.140f;

            var handleBox = root.AddComponent<BoxCollider>();
            handleBox.center = new Vector3(0f, 0.150f, -0.050f);
            handleBox.size = new Vector3(0.028f, 0.080f, 0.120f);

            // Visual Hierarchy
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var mf = visual.AddComponent<MeshFilter>();
            var mr = visual.AddComponent<MeshRenderer>();
            mr.sharedMaterial = OpaqueMaterial;

            var buffer = new ProceduralMeshBuilder.MeshBuffer();
            Vector2 uvPail = ProceduralMeshBuilder.GetSwatchUV(Swatches.GalvanizedPailGrey.Col, Swatches.GalvanizedPailGrey.Row);
            Vector2 uvBrass = ProceduralMeshBuilder.GetSwatchUV(Swatches.BellBrassGold.Col, Swatches.BellBrassGold.Row);
            Vector2 uvWood = ProceduralMeshBuilder.GetSwatchUV(Swatches.PailPineWood.Col, Swatches.PailPineWood.Row);

            AddCylinderToBuffer(buffer, new Vector3(0f, 0.075f, 0f), 0.070f, 0.150f, 14, uvPail);
            // Angled Spout & Rosette Head
            AddCylinderAngledToBuffer(buffer, new Vector3(0f, 0.050f, 0.060f), new Vector3(0f, 0.160f, 0.160f), 0.018f, 0.014f, 8, uvPail);
            AddSphereToBuffer(buffer, new Vector3(0f, 0.165f, 0.165f), 0.024f, 8, 10, uvBrass);
            // Arch Handle
            AddTorusHandleToBuffer(buffer, new Vector3(0f, 0.160f, -0.030f), 0.060f, 0.012f, 12, 6, uvWood, true);

            mf.sharedMesh = buffer.Build($"{name}_Mesh");

            var grab = root.GetComponent<XRGrabInteractable>();
            grab.attachTransform.localPosition = new Vector3(0f, 0.165f, -0.040f);
            grab.attachTransform.localRotation = Quaternion.identity; // Spout points forward along grip axis (F12-C04)

            return root;
        }

        // =========================================================================
        // 10. Potted Sapling (Crafting Stall)
        // =========================================================================

        public static GameObject BuildPottedSapling(Vector3 pos, Quaternion rot, Transform parent = null, string name = "PottedSapling")
        {
            GameObject root = new GameObject(name);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = rot;

            SetupGrabbableRoot(root, 0.75f);

            // Compound Colliders: 2 Capsules (Pot + Shoot)
            var potCap = root.AddComponent<CapsuleCollider>();
            potCap.center = new Vector3(0f, 0.060f, 0f);
            potCap.radius = 0.058f;
            potCap.height = 0.120f; // H >= 2R + 0.002m

            var shootCap = root.AddComponent<CapsuleCollider>();
            shootCap.center = new Vector3(0f, 0.170f, 0f);
            shootCap.radius = 0.040f;
            shootCap.height = 0.110f;

            // Visual Hierarchy
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var mf = visual.AddComponent<MeshFilter>();
            var mr = visual.AddComponent<MeshRenderer>();
            mr.sharedMaterial = OpaqueMaterial;

            var buffer = new ProceduralMeshBuilder.MeshBuffer();
            Vector2 uvPot = ProceduralMeshBuilder.GetSwatchUV(Swatches.FlowerPotTerracotta.Col, Swatches.FlowerPotTerracotta.Row);
            Vector2 uvSoil = ProceduralMeshBuilder.GetSwatchUV(Swatches.FlowerPotSoil.Col, Swatches.FlowerPotSoil.Row);
            Vector2 uvStem = ProceduralMeshBuilder.GetSwatchUV(Swatches.FruitStemWoodBrown.Col, Swatches.FruitStemWoodBrown.Row);
            Vector2 uvShoot = ProceduralMeshBuilder.GetSwatchUV(Swatches.PottedSaplingGreenShoot.Col, Swatches.PottedSaplingGreenShoot.Row);

            // Terracotta pot & rim collar
            AddCylinderToBuffer(buffer, new Vector3(0f, 0.055f, 0f), 0.056f, 0.110f, 12, uvPot);
            AddCylinderToBuffer(buffer, new Vector3(0f, 0.105f, 0f), 0.062f, 0.016f, 12, uvPot);
            AddDiscToBuffer(buffer, new Vector3(0f, 0.106f, 0f), 0.053f, 12, uvSoil);

            // Sapling stem & leafy crown spheres
            AddCylinderToBuffer(buffer, new Vector3(0f, 0.145f, 0f), 0.006f, 0.080f, 6, uvStem);
            AddSphereToBuffer(buffer, new Vector3(0f, 0.180f, 0f), 0.035f, 8, 10, uvShoot);
            AddSphereToBuffer(buffer, new Vector3(-0.020f, 0.165f, 0.010f), 0.025f, 6, 8, uvShoot);
            AddSphereToBuffer(buffer, new Vector3(0.020f, 0.165f, -0.010f), 0.025f, 6, 8, uvShoot);

            mf.sharedMesh = buffer.Build($"{name}_Mesh");

            var grab = root.GetComponent<XRGrabInteractable>();
            grab.attachTransform.localPosition = new Vector3(0f, 0.075f, -0.050f);
            grab.attachTransform.localRotation = Quaternion.identity;

            return root;
        }

        // =========================================================================
        // 11. Seed Packet (Crafting Stall)
        // =========================================================================

        public static GameObject BuildSeedPacket(Vector3 pos, Quaternion rot, Transform parent = null, string name = "SeedPacket", SwatchCoord? badgeSwatch = null)
        {
            GameObject root = new GameObject(name);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = rot;

            SetupGrabbableRoot(root, 0.10f, 0.8f, 1.2f);

            // BoxCollider: (0.090, 0.130, 0.016) -> volume = 1.87e-4 m^3 > 1e-6 m^3 (F12-C05, F13-C01)
            var boxCol = root.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0f, 0.065f, 0f);
            boxCol.size = new Vector3(0.090f, 0.130f, 0.016f);

            // Visual Hierarchy
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var mf = visual.AddComponent<MeshFilter>();
            var mr = visual.AddComponent<MeshRenderer>();
            mr.sharedMaterial = OpaqueMaterial;

            var buffer = new ProceduralMeshBuilder.MeshBuffer();
            Vector2 uvKraft = ProceduralMeshBuilder.GetSwatchUV(Swatches.SeedPacketKraftPaper.Col, Swatches.SeedPacketKraftPaper.Row);
            SwatchCoord badge = badgeSwatch ?? Swatches.SeedPacketPrintYellow;
            Vector2 uvPrint = ProceduralMeshBuilder.GetSwatchUV(badge.Col, badge.Row);

            AddBoxToBuffer(buffer, new Vector3(0f, 0.065f, 0f), new Vector3(0.090f, 0.130f, 0.016f), uvKraft);
            // Badge Print Inset Quad on Front (+Z)
            buffer.AddQuad(
                new Vector3(-0.030f, 0.040f, 0.0081f),
                new Vector3(0.030f, 0.040f, 0.0081f),
                new Vector3(0.030f, 0.090f, 0.0081f),
                new Vector3(-0.030f, 0.090f, 0.0081f),
                Vector3.forward, uvPrint);

            mf.sharedMesh = buffer.Build($"{name}_Mesh");

            var grab = root.GetComponent<XRGrabInteractable>();
            grab.attachTransform.localPosition = new Vector3(0f, 0.065f, -0.010f);
            grab.attachTransform.localRotation = Quaternion.identity;

            return root;
        }

        // =========================================================================
        // Geometric Procedural Helper Routines
        // =========================================================================

        private static void AddSphereToBuffer(ProceduralMeshBuilder.MeshBuffer buffer, Vector3 center, float radius, int latSeg, int lonSeg, Vector2 uv)
        {
            for (int lat = 0; lat < latSeg; lat++)
            {
                float theta0 = (lat / (float)latSeg) * Mathf.PI;
                float theta1 = ((lat + 1) / (float)latSeg) * Mathf.PI;
                float y0 = Mathf.Cos(theta0) * radius;
                float r0 = Mathf.Sin(theta0) * radius;
                float y1 = Mathf.Cos(theta1) * radius;
                float r1 = Mathf.Sin(theta1) * radius;

                for (int lon = 0; lon < lonSeg; lon++)
                {
                    float phi0 = (lon / (float)lonSeg) * Mathf.PI * 2f;
                    float phi1 = ((lon + 1) / (float)lonSeg) * Mathf.PI * 2f;

                    Vector3 v00 = center + new Vector3(Mathf.Cos(phi0) * r0, y0, Mathf.Sin(phi0) * r0);
                    Vector3 v01 = center + new Vector3(Mathf.Cos(phi1) * r0, y0, Mathf.Sin(phi1) * r0);
                    Vector3 v11 = center + new Vector3(Mathf.Cos(phi1) * r1, y1, Mathf.Sin(phi1) * r1);
                    Vector3 v10 = center + new Vector3(Mathf.Cos(phi0) * r1, y1, Mathf.Sin(phi0) * r1);

                    Vector3 n00 = (v00 - center).normalized;
                    Vector3 n01 = (v01 - center).normalized;
                    Vector3 n11 = (v11 - center).normalized;
                    Vector3 n10 = (v10 - center).normalized;

                    if (lat == 0)
                        buffer.AddTriangle(v00, v11, v10, n00, n11, n10, uv);
                    else if (lat == latSeg - 1)
                        buffer.AddTriangle(v00, v01, v10, n00, n01, n10, uv);
                    else
                        buffer.AddQuad(v00, v01, v11, v10, n00, n01, n11, n10, uv);
                }
            }
        }

        private static void AddCylinderToBuffer(ProceduralMeshBuilder.MeshBuffer buffer, Vector3 center, float radius, float height, int seg, Vector2 uv)
        {
            float halfH = height * 0.5f;
            Vector3 topCenter = center + Vector3.up * halfH;
            Vector3 botCenter = center - Vector3.up * halfH;

            for (int i = 0; i < seg; i++)
            {
                float a0 = (i / (float)seg) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)seg) * Mathf.PI * 2f;

                Vector3 p0 = new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);

                Vector3 n0 = p0.normalized;
                Vector3 n1 = p1.normalized;

                // Top Cap
                buffer.AddTriangle(topCenter, topCenter + p0, topCenter + p1, Vector3.up, Vector3.up, Vector3.up, uv);
                // Side Barrel
                buffer.AddQuad(topCenter + p0, topCenter + p1, botCenter + p1, botCenter + p0, n0, n1, n1, n0, uv);
                // Bottom Cap
                buffer.AddTriangle(botCenter, botCenter + p1, botCenter + p0, Vector3.down, Vector3.down, Vector3.down, uv);
            }
        }

        private static void AddHollowCylinderToBuffer(ProceduralMeshBuilder.MeshBuffer buffer, Vector3 center, float rOuter, float rInner, float height, int seg, Vector2 uv)
        {
            float halfH = height * 0.5f;
            Vector3 topY = center + Vector3.up * halfH;
            Vector3 botY = center - Vector3.up * halfH;

            for (int i = 0; i < seg; i++)
            {
                float a0 = (i / (float)seg) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)seg) * Mathf.PI * 2f;

                Vector3 po0 = new Vector3(Mathf.Cos(a0) * rOuter, 0f, Mathf.Sin(a0) * rOuter);
                Vector3 po1 = new Vector3(Mathf.Cos(a1) * rOuter, 0f, Mathf.Sin(a1) * rOuter);
                Vector3 pi0 = new Vector3(Mathf.Cos(a0) * rInner, 0f, Mathf.Sin(a0) * rInner);
                Vector3 pi1 = new Vector3(Mathf.Cos(a1) * rInner, 0f, Mathf.Sin(a1) * rInner);

                // Outer Wall
                buffer.AddQuad(topY + po0, topY + po1, botY + po1, botY + po0, po0.normalized, po1.normalized, po1.normalized, po0.normalized, uv);
                // Inner Wall
                buffer.AddQuad(topY + pi1, topY + pi0, botY + pi0, botY + pi1, -pi1.normalized, -pi0.normalized, -pi0.normalized, -pi1.normalized, uv);
                // Top Rim
                buffer.AddQuad(topY + pi0, topY + pi1, topY + po1, topY + po0, Vector3.up, uv);
                // Bottom Cap
                buffer.AddQuad(botY + po0, botY + po1, botY + pi1, botY + pi0, Vector3.down, uv);
            }
        }

        private static void AddDiscToBuffer(ProceduralMeshBuilder.MeshBuffer buffer, Vector3 center, float radius, int seg, Vector2 uv)
        {
            for (int i = 0; i < seg; i++)
            {
                float a0 = (i / (float)seg) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)seg) * Mathf.PI * 2f;

                Vector3 p0 = center + new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
                Vector3 p1 = center + new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);

                buffer.AddTriangle(center, p0, p1, Vector3.up, Vector3.up, Vector3.up, uv);
            }
        }

        private static void AddConeToBuffer(ProceduralMeshBuilder.MeshBuffer buffer, Vector3 center, float rBase, float rTip, float height, int seg, Vector2 uv)
        {
            float halfH = height * 0.5f;
            Vector3 topY = center + Vector3.up * halfH;
            Vector3 botY = center - Vector3.up * halfH;

            for (int i = 0; i < seg; i++)
            {
                float a0 = (i / (float)seg) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)seg) * Mathf.PI * 2f;

                Vector3 pb0 = new Vector3(Mathf.Cos(a0) * rBase, 0f, Mathf.Sin(a0) * rBase);
                Vector3 pb1 = new Vector3(Mathf.Cos(a1) * rBase, 0f, Mathf.Sin(a1) * rBase);
                Vector3 pt0 = new Vector3(Mathf.Cos(a0) * rTip, 0f, Mathf.Sin(a0) * rTip);
                Vector3 pt1 = new Vector3(Mathf.Cos(a1) * rTip, 0f, Mathf.Sin(a1) * rTip);

                buffer.AddQuad(topY + pt0, topY + pt1, botY + pb1, botY + pb0, pb0.normalized, pb1.normalized, pb1.normalized, pb0.normalized, uv);
            }
        }

        private static void AddTorusToBuffer(ProceduralMeshBuilder.MeshBuffer buffer, Vector3 center, float majorR, float minorR, int majorSeg, int minorSeg, Vector2 uv)
        {
            for (int i = 0; i < majorSeg; i++)
            {
                float u0 = (i / (float)majorSeg) * Mathf.PI * 2f;
                float u1 = ((i + 1) / (float)majorSeg) * Mathf.PI * 2f;

                Vector3 c0 = center + new Vector3(Mathf.Cos(u0) * majorR, 0f, Mathf.Sin(u0) * majorR);
                Vector3 c1 = center + new Vector3(Mathf.Cos(u1) * majorR, 0f, Mathf.Sin(u1) * majorR);

                Vector3 d0 = (c0 - center).normalized;
                Vector3 d1 = (c1 - center).normalized;

                for (int j = 0; j < minorSeg; j++)
                {
                    float v0 = (j / (float)minorSeg) * Mathf.PI * 2f;
                    float v1 = ((j + 1) / (float)minorSeg) * Mathf.PI * 2f;

                    Vector3 n00 = (d0 * Mathf.Cos(v0) + Vector3.up * Mathf.Sin(v0)).normalized;
                    Vector3 n01 = (d0 * Mathf.Cos(v1) + Vector3.up * Mathf.Sin(v1)).normalized;
                    Vector3 n11 = (d1 * Mathf.Cos(v1) + Vector3.up * Mathf.Sin(v1)).normalized;
                    Vector3 n10 = (d1 * Mathf.Cos(v0) + Vector3.up * Mathf.Sin(v0)).normalized;

                    buffer.AddQuad(c0 + n00 * minorR, c0 + n01 * minorR, c1 + n11 * minorR, c1 + n10 * minorR, n00, n01, n11, n10, uv);
                }
            }
        }

        private static void AddTorusTopHalfToBuffer(ProceduralMeshBuilder.MeshBuffer buffer, Vector3 center, float majorR, float minorR, int majorSeg, int minorSeg, Vector2 uv)
        {
            for (int i = 0; i < majorSeg; i++)
            {
                float u0 = (i / (float)majorSeg) * Mathf.PI * 2f;
                float u1 = ((i + 1) / (float)majorSeg) * Mathf.PI * 2f;

                Vector3 c0 = center + new Vector3(Mathf.Cos(u0) * majorR, 0f, Mathf.Sin(u0) * majorR);
                Vector3 c1 = center + new Vector3(Mathf.Cos(u1) * majorR, 0f, Mathf.Sin(u1) * majorR);

                Vector3 d0 = (c0 - center).normalized;
                Vector3 d1 = (c1 - center).normalized;

                for (int j = 0; j < minorSeg; j++)
                {
                    float v0 = (j / (float)minorSeg) * Mathf.PI; // top half (0 to PI)
                    float v1 = ((j + 1) / (float)minorSeg) * Mathf.PI;

                    Vector3 n00 = (d0 * Mathf.Cos(v0) + Vector3.up * Mathf.Sin(v0)).normalized;
                    Vector3 n01 = (d0 * Mathf.Cos(v1) + Vector3.up * Mathf.Sin(v1)).normalized;
                    Vector3 n11 = (d1 * Mathf.Cos(v1) + Vector3.up * Mathf.Sin(v1)).normalized;
                    Vector3 n10 = (d1 * Mathf.Cos(v0) + Vector3.up * Mathf.Sin(v0)).normalized;

                    buffer.AddQuad(c0 + n00 * minorR, c0 + n01 * minorR, c1 + n11 * minorR, c1 + n10 * minorR, n00, n01, n11, n10, uv);
                }
            }
        }

        private static void AddTorusHandleToBuffer(ProceduralMeshBuilder.MeshBuffer buffer, Vector3 center, float majorR, float minorR, int segArc, int segTube, Vector2 uv, bool vertical = false)
        {
            for (int i = 0; i < segArc; i++)
            {
                float t0 = (i / (float)segArc) * Mathf.PI;
                float t1 = ((i + 1) / (float)segArc) * Mathf.PI;

                Vector3 p0 = vertical
                    ? center + new Vector3(0f, Mathf.Sin(t0) * majorR, Mathf.Cos(t0) * majorR)
                    : center + new Vector3(Mathf.Cos(t0) * majorR, Mathf.Sin(t0) * majorR, 0f);
                Vector3 p1 = vertical
                    ? center + new Vector3(0f, Mathf.Sin(t1) * majorR, Mathf.Cos(t1) * majorR)
                    : center + new Vector3(Mathf.Cos(t1) * majorR, Mathf.Sin(t1) * majorR, 0f);

                Vector3 dir = (p1 - p0).normalized;
                Vector3 side = vertical ? Vector3.right : Vector3.forward;
                Vector3 up = Vector3.Cross(dir, side).normalized;

                for (int j = 0; j < segTube; j++)
                {
                    float u0 = (j / (float)segTube) * Mathf.PI * 2f;
                    float u1 = ((j + 1) / (float)segTube) * Mathf.PI * 2f;

                    Vector3 n0 = (Mathf.Cos(u0) * side + Mathf.Sin(u0) * up).normalized;
                    Vector3 n1 = (Mathf.Cos(u1) * side + Mathf.Sin(u1) * up).normalized;

                    buffer.AddQuad(p0 + n0 * minorR, p0 + n1 * minorR, p1 + n1 * minorR, p1 + n0 * minorR, n0, n1, n1, n0, uv);
                }
            }
        }

        private static void AddCylinderAngledToBuffer(ProceduralMeshBuilder.MeshBuffer buffer, Vector3 p0, Vector3 p1, float r0, float r1, int seg, Vector2 uv)
        {
            Vector3 dir = (p1 - p0).normalized;
            Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;
            if (side.sqrMagnitude < 0.001f) side = Vector3.Cross(dir, Vector3.right).normalized;
            Vector3 up = Vector3.Cross(dir, side).normalized;

            for (int i = 0; i < seg; i++)
            {
                float a0 = (i / (float)seg) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)seg) * Mathf.PI * 2f;

                Vector3 rad0 = (Mathf.Cos(a0) * side + Mathf.Sin(a0) * up).normalized;
                Vector3 rad1 = (Mathf.Cos(a1) * side + Mathf.Sin(a1) * up).normalized;

                Vector3 v00 = p0 + rad0 * r0;
                Vector3 v01 = p0 + rad1 * r0;
                Vector3 v11 = p1 + rad1 * r1;
                Vector3 v10 = p1 + rad0 * r1;

                buffer.AddQuad(v00, v01, v11, v10, rad0, rad1, rad1, rad0, uv);
            }
        }

        private static void AddBoxToBuffer(ProceduralMeshBuilder.MeshBuffer buffer, Vector3 center, Vector3 size, Vector2 uvFront, Vector2 uvSides = default)
        {
            Vector2 sideUV = (uvSides == default) ? uvFront : uvSides;
            Vector3 half = size * 0.5f;

            // Top (+Y)
            buffer.AddQuad(center + new Vector3(-half.x, half.y, -half.z), center + new Vector3(half.x, half.y, -half.z),
                           center + new Vector3(half.x, half.y, half.z), center + new Vector3(-half.x, half.y, half.z), Vector3.up, sideUV);
            // Bottom (-Y)
            buffer.AddQuad(center + new Vector3(-half.x, -half.y, half.z), center + new Vector3(half.x, -half.y, half.z),
                           center + new Vector3(half.x, -half.y, -half.z), center + new Vector3(-half.x, -half.y, -half.z), Vector3.down, sideUV);
            // Front (+Z)
            buffer.AddQuad(center + new Vector3(-half.x, -half.y, half.z), center + new Vector3(half.x, -half.y, half.z),
                           center + new Vector3(half.x, half.y, half.z), center + new Vector3(-half.x, half.y, half.z), Vector3.forward, uvFront);
            // Back (-Z)
            buffer.AddQuad(center + new Vector3(half.x, -half.y, -half.z), center + new Vector3(-half.x, -half.y, -half.z),
                           center + new Vector3(-half.x, half.y, -half.z), center + new Vector3(half.x, half.y, -half.z), Vector3.back, uvFront);
            // Right (+X)
            buffer.AddQuad(center + new Vector3(half.x, -half.y, half.z), center + new Vector3(half.x, -half.y, -half.z),
                           center + new Vector3(half.x, half.y, -half.z), center + new Vector3(half.x, half.y, half.z), Vector3.right, sideUV);
            // Left (-X)
            buffer.AddQuad(center + new Vector3(-half.x, -half.y, -half.z), center + new Vector3(-half.x, -half.y, half.z),
                           center + new Vector3(-half.x, half.y, half.z), center + new Vector3(-half.x, half.y, -half.z), Vector3.left, sideUV);
        }

        private static void AddHalfCylinderToBuffer(ProceduralMeshBuilder.MeshBuffer buffer, Vector3 center, float radius, float depth, int seg, Vector2 uvFace, Vector2 uvCrust)
        {
            float halfD = depth * 0.5f;
            Vector3 frontCenter = center + Vector3.forward * halfD;
            Vector3 backCenter = center - Vector3.forward * halfD;

            for (int i = 0; i < seg; i++)
            {
                float a0 = (i / (float)seg) * Mathf.PI;
                float a1 = ((i + 1) / (float)seg) * Mathf.PI;

                Vector3 p0 = new Vector3(Mathf.Cos(a0) * radius, Mathf.Sin(a0) * radius, 0f);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius, 0f);

                // Front Cap
                buffer.AddTriangle(frontCenter, frontCenter + p0, frontCenter + p1, Vector3.forward, Vector3.forward, Vector3.forward, uvFace);
                // Back Cap
                buffer.AddTriangle(backCenter, backCenter + p1, backCenter + p0, Vector3.back, Vector3.back, Vector3.back, uvFace);
                // Arched Crust Rim
                Vector3 n0 = p0.normalized;
                Vector3 n1 = p1.normalized;
                buffer.AddQuad(frontCenter + p0, frontCenter + p1, backCenter + p1, backCenter + p0, n0, n1, n1, n0, uvCrust);
            }
        }

        private static void AddLeafQuadToBuffer(ProceduralMeshBuilder.MeshBuffer buffer, Vector3 pos, float len, float wid, Vector2 uv)
        {
            Vector3 p0 = pos;
            Vector3 p1 = pos + new Vector3(len * 0.5f, wid, 0f);
            Vector3 p2 = pos + new Vector3(len, 0f, 0f);
            Vector3 p3 = pos + new Vector3(len * 0.5f, -wid, 0f);

            buffer.AddQuad(p0, p1, p2, p3, Vector3.forward, uv);
            buffer.AddQuad(p0, p3, p2, p1, Vector3.back, uv);
        }

        private static void AddLeafStalk(ProceduralMeshBuilder.MeshBuffer buffer, Vector3 basePos, Quaternion rot, float length, Vector2 uv)
        {
            Vector3 dir = rot * Vector3.up;
            Vector3 side = rot * Vector3.right * 0.025f;
            Vector3 tip = basePos + dir * length;
            Vector3 mid = basePos + dir * (length * 0.5f);

            buffer.AddQuad(basePos - side * 0.3f, mid - side, tip, mid + side, Vector3.forward, uv);
            buffer.AddQuad(basePos - side * 0.3f, mid + side, tip, mid - side, Vector3.back, uv);
        }

#if UNITY_EDITOR
        [MenuItem("AnimalCrossing/Build Market Props", priority = 30)]
        public static void MenuItem_BuildMarketProps()
        {
            var existing = GameObject.Find("MarketProps");
            if (existing != null)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(existing);
                else
                    UnityEngine.Object.DestroyImmediate(existing);
            }

            // Remove standalone prop objects if present
            string[] propPrefixes = { "Apple", "Orange", "Peach", "Turnip", "CoffeeMug", "Donut", "Teapot", "SlicedBread", "WateringCan", "PottedSapling", "SeedPacket" };
            var allGrabs = UnityEngine.Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include);
            foreach (var grab in allGrabs)
            {
                if (grab == null) continue;
                string gName = grab.gameObject.name;
                if (gName.Contains("ShoppingBasket") || gName.Contains("Origin") || gName.Contains("Controller")) continue;
                foreach (var prefix in propPrefixes)
                {
                    if (gName.StartsWith(prefix))
                    {
                        if (Application.isPlaying)
                            UnityEngine.Object.Destroy(grab.gameObject);
                        else
                            UnityEngine.Object.DestroyImmediate(grab.gameObject);
                        break;
                    }
                }
            }

            GameObject props = BuildAllProps();
            Selection.activeGameObject = props;
            EditorUtility.SetDirty(props);
            Debug.Log("[MarketPropsBuilder] Realistic market props successfully created in active scene.");
        }

        [MenuItem("AnimalCrossing/Focus on Produce Stall Props", priority = 31)]
        public static void MenuItem_FocusProduceProps()
        {
            var target = GameObject.Find("Apple");
            if (target != null)
            {
                Selection.activeGameObject = target;
                var view = SceneView.lastActiveSceneView ?? (SceneView.sceneViews.Count > 0 ? SceneView.sceneViews[0] as SceneView : null);
                if (view != null)
                {
                    view.LookAt(target.transform.position, Quaternion.Euler(15f, -45f, 0f), 3.2f);
                }
            }
        }

        [MenuItem("AnimalCrossing/Focus on Cafe Stall Props", priority = 32)]
        public static void MenuItem_FocusCafeProps()
        {
            var target = GameObject.Find("Donut");
            if (target != null)
            {
                Selection.activeGameObject = target;
                var view = SceneView.lastActiveSceneView ?? (SceneView.sceneViews.Count > 0 ? SceneView.sceneViews[0] as SceneView : null);
                if (view != null)
                {
                    view.LookAt(target.transform.position, Quaternion.Euler(20f, 225f, 0f), 3.2f);
                }
            }
        }

        [MenuItem("AnimalCrossing/Focus on Crafting Stall Props", priority = 33)]
        public static void MenuItem_FocusCraftingProps()
        {
            var target = GameObject.Find("WateringCan");
            if (target != null)
            {
                Selection.activeGameObject = target;
                var view = SceneView.lastActiveSceneView ?? (SceneView.sceneViews.Count > 0 ? SceneView.sceneViews[0] as SceneView : null);
                if (view != null)
                {
                    view.LookAt(target.transform.position, Quaternion.Euler(20f, 0f, 0f), 3.2f);
                }
            }
        }

        [MenuItem("AnimalCrossing/Batch Regenerate All Props in All Scenes", priority = 33)]
        public static void RegenerateAllScenesBatch()
        {
            RegenerateScene("Assets/Scenes/AnimalCrossingMarketSquare.unity");
            RegenerateScene("Assets/Scenes/SampleScene.unity");
            Debug.Log("[MarketPropsBuilder] Batch scene regeneration complete for both scenes.");
        }

        private static void RegenerateScene(string scenePath)
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            MenuItem_BuildMarketProps();
            ShoppingBasketBuilder.MenuItem_BuildShoppingBasket();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log($"[MarketPropsBuilder] Regenerated and saved: {scenePath}");
        }
#endif
    }
}
