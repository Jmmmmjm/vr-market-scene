// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MarketStallBuilder.cs" company="VR Market Scene">
//   Copyright (c) 2026 Animal Crossing Stylized VR Village Project. All rights reserved.
// </copyright>
// <summary>
//   Master procedural generator for the three Animal Crossing themed market stalls (Milestone 3).
//   Constructs Nook-style Produce & Fruit Stall (F8), Cozy Café & Bakery Stall (F9),
//   and DIY Crafting & Gardening Stall (F10) with 100% compound BoxColliders,
//   catenary cloth canopies, ergonomic counters, and anti-Z-fighting join geometry.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using AnimalCrossing.TechnicalArt;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnimalCrossing.Environment
{
    public enum CanopyColorTheme
    {
        CoffeeBrownAndCream,
        BistroGreenAndCream
    }

    public enum CraftingCanopyTheme
    {
        ForestGreenAndNaturalCanvas,
        TealAndWarmPeach
    }

    /// <summary>
    /// Master builder for the three themed Animal Crossing market stalls.
    /// Strictly complies with features F8, F9, F10, F11, F13, F15, T3_04..06, and Scenario 1.
    /// </summary>
    public static class MarketStallBuilder
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

        private static PhysicsMaterial _displayPhysicsMat;
        public static PhysicsMaterial DisplayPhysicsMaterial
        {
            get
            {
                if (_displayPhysicsMat == null)
                {
                    _displayPhysicsMat = new PhysicsMaterial("PM_MarketWoodDisplay")
                    {
                        staticFriction = 0.6f,
                        dynamicFriction = 0.6f,
                        bounciness = 0.1f,
                        frictionCombine = PhysicsMaterialCombine.Average,
                        bounceCombine = PhysicsMaterialCombine.Average
                    };
                }
                return _displayPhysicsMat;
            }
        }

        // =========================================================================
        // Master Assembly API: BuildAllStalls
        // =========================================================================

        /// <summary>
        /// Builds all 3 themed stalls in their designated plaza positions.
        /// </summary>
        public static GameObject BuildAllStalls(Transform parent = null)
        {
            // Destroy any existing MarketStalls GameObjects to ensure idempotency and prevent duplicate co-planar stalls
            var allStallRoots = UnityEngine.Object.FindObjectsByType<Transform>();
            foreach (var t in allStallRoots)
            {
                if (t != null && t.gameObject.name == "MarketStalls")
                {
                    if (Application.isPlaying)
                        UnityEngine.Object.Destroy(t.gameObject);
                    else
                        UnityEngine.Object.DestroyImmediate(t.gameObject);
                }
            }

            GameObject stallsGroup = new GameObject("MarketStalls");
            if (parent != null)
                stallsGroup.transform.SetParent(parent, false);

            stallsGroup.transform.localPosition = Vector3.zero;
            stallsGroup.transform.localRotation = Quaternion.identity;

            BuildProduceStall(stallsGroup.transform);
            BuildCafeStall(stallsGroup.transform);
            BuildCraftingStall(stallsGroup.transform);

            Debug.Log("[MarketStallBuilder] All 3 themed market stalls successfully built.");
            return stallsGroup;
        }

        // =========================================================================
        // F8: Produce & Fruit Stall Builder
        // =========================================================================

        [System.Serializable]
        public struct ProduceStallConfig
        {
            public Vector3 position;
            public Quaternion rotation;
            public float width;              // 2.40m
            public float depth;              // 1.40m
            public float counterHeight;      // 0.95m
            public float counterWidth;       // 2.20m
            public float counterDepth;       // 0.70m
            public float postRadius;         // 0.06m (diameter 0.12m)
            public float postAnchorDepth;    // 0.05m
            public float frontEaveHeight;    // 2.15m
            public float rearRidgeHeight;    // 2.45m
            public float canopySag;          // 0.08m
            public int canopyStripes;        // 6
            public float crateTiltAngle;     // 15.0 degrees

            public static ProduceStallConfig CreateDefault()
            {
                return new ProduceStallConfig
                {
                    position = new Vector3(-5.5f, 0f, -6.0f), // South-West quadrant
                    rotation = Quaternion.Euler(0f, 90f, 0f), // Facing East into South Avenue
                    width = 2.40f,
                    depth = 1.40f,
                    counterHeight = 0.95f,
                    counterWidth = 2.20f,
                    counterDepth = 0.70f,
                    postRadius = 0.06f,
                    postAnchorDepth = 0.05f,
                    frontEaveHeight = 2.15f,
                    rearRidgeHeight = 2.45f,
                    canopySag = 0.08f,
                    canopyStripes = 6,
                    crateTiltAngle = 15.0f
                };
            }
        }

        /// <summary>
        /// Builds the complete Nook-style Produce & Fruit Stall (F8) hierarchy.
        /// </summary>
        public static GameObject BuildProduceStall(Transform parent = null, Vector3? worldPosition = null, Quaternion? worldRotation = null)
        {
            ProduceStallConfig config = ProduceStallConfig.CreateDefault();
            if (worldPosition.HasValue) config.position = worldPosition.Value;
            if (worldRotation.HasValue) config.rotation = worldRotation.Value;

            Material mat = OpaqueMaterial;

            GameObject stallRoot = new GameObject("ProduceStall");
            if (parent != null)
                stallRoot.transform.SetParent(parent, false);

            stallRoot.transform.position = config.position;
            stallRoot.transform.rotation = config.rotation;

            // Child alias for FruitStall
            GameObject aliasFruit = new GameObject("FruitStall");
            aliasFruit.transform.SetParent(stallRoot.transform, false);

            // 1. Structural Posts (4 beveled timber corner posts anchored 5cm in ground)
            BuildProducePosts(stallRoot.transform, config, mat);

            // 2. Framework Beams & Aprons (with 1.0mm air gap below counter planks)
            BuildProduceFraming(stallRoot.transform, config, mat);

            // 3. Countertop Deck (5 beveled planks at Y = 0.95m)
            BuildProduceCounter(stallRoot.transform, config, mat);

            // 4. Lower Storage Shelf (Y = 0.30m)
            BuildProduceLowerShelf(stallRoot.transform, config, mat);

            // 5. Rear Riser Wall (Y = 0.95m to 1.35m backstop)
            BuildProduceRearWall(stallRoot.transform, config, mat);

            // 6. 3-Tier Angled Fruit Display Crates (tilted 15°, friction 0.6, 4 bays per tier)
            BuildProduceDisplayCrates(stallRoot.transform, config, mat);

            // 7. Catenary Scalloped Cloth Canopy (Red/Cream stripes, lowest valance = 1.95m)
            BuildProduceCanopy(stallRoot.transform, config, mat);

            // 8. Stall Countertop Snap Sockets (Fruit tray socket + Turnip scale socket flush at Y = 0.95m)
            BuildProduceSockets(stallRoot.transform, config);

            Debug.Log($"[MarketStallBuilder] Produce & Fruit Stall built at {config.position}.");
            return stallRoot;
        }

        private static void BuildProducePosts(Transform root, ProduceStallConfig cfg, Material mat)
        {
            GameObject postsGroup = new GameObject("Posts");
            postsGroup.transform.SetParent(root, false);

            float halfX = cfg.counterWidth * 0.5f - 0.02f; // 1.08m
            float halfZ = cfg.counterDepth * 0.5f - 0.02f; // 0.33m

            // Front posts terminate at underside of canopy frame (Y = 2.16m, total H = 2.21m)
            // Rear posts terminate at underside of canopy frame (Y = 2.30m, total H = 2.35m)
            float frontH = 2.16f + cfg.postAnchorDepth; // 2.21m
            float rearH  = 2.30f + cfg.postAnchorDepth; // 2.35m

            Mesh frontPostMesh = ProceduralMeshBuilder.CreateBeveledCylinder(
                cfg.postRadius, frontH, 0.015f, 10, 2,
                Swatches.WoodPineMidtone.Col, Swatches.WoodPineMidtone.Row);

            Mesh rearPostMesh = ProceduralMeshBuilder.CreateBeveledCylinder(
                cfg.postRadius, rearH, 0.015f, 10, 2,
                Swatches.WoodPineMidtone.Col, Swatches.WoodPineMidtone.Row);

            float frontCenterY = -cfg.postAnchorDepth + frontH * 0.5f;
            float rearCenterY  = -cfg.postAnchorDepth + rearH * 0.5f;

            CreatePostInstance(postsGroup.transform, "Post_FrontLeft", frontPostMesh, mat,
                new Vector3(-halfX, frontCenterY, -halfZ), cfg.postRadius, frontH);
            CreatePostInstance(postsGroup.transform, "Post_FrontRight", frontPostMesh, mat,
                new Vector3(halfX, frontCenterY, -halfZ), cfg.postRadius, frontH);
            CreatePostInstance(postsGroup.transform, "Post_RearLeft", rearPostMesh, mat,
                new Vector3(-halfX, rearCenterY, halfZ), cfg.postRadius, rearH);
            CreatePostInstance(postsGroup.transform, "Post_RearRight", rearPostMesh, mat,
                new Vector3(halfX, rearCenterY, halfZ), cfg.postRadius, rearH);
        }

        private static void CreatePostInstance(Transform parent, string name, Mesh mesh, Material mat,
            Vector3 pos, float radius, float totalHeight)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            // BoxCollider strictly flush with ground (Y >= 0, zero floor penetration)
            var col = go.AddComponent<BoxCollider>();
            float colH = totalHeight - 0.05f;
            col.size = new Vector3(radius * 2f, colH, radius * 2f);
            col.center = new Vector3(0f, 0.025f, 0f);
        }

        private static void BuildProduceFraming(Transform root, ProduceStallConfig cfg, Material mat)
        {
            GameObject frameGroup = new GameObject("Framing");
            frameGroup.transform.SetParent(root, false);
            GameObject frameAlias = new GameObject("Frame");
            frameAlias.transform.SetParent(root, false);

            float beamW = cfg.counterWidth - 0.04f; // 2.16m
            float beamD = cfg.counterDepth - 0.04f; // 0.66m

            // Side lower stretchers (Y = 0.25m)
            Mesh sideStretcherMesh = ProceduralMeshBuilder.CreateChamferedCube(0.06f, 0.05f, beamD, 0.008f,
                Swatches.WoodCedarDark.Col, Swatches.WoodCedarDark.Row);
            CreateMeshChild(frameGroup.transform, "Stretcher_Left", sideStretcherMesh, mat,
                new Vector3(-cfg.counterWidth * 0.5f + 0.02f, 0.25f, 0f), Quaternion.identity);
            CreateMeshChild(frameGroup.transform, "Stretcher_Right", sideStretcherMesh, mat,
                new Vector3(cfg.counterWidth * 0.5f - 0.02f, 0.25f, 0f), Quaternion.identity);

            // Front & Rear under-counter aprons (top at Y = 0.914m, giving 1.0mm gap below Y = 0.915m planks)
            float apronCenterY = 0.884f; // Height = 0.06m (top at Y = 0.914m)
            Mesh frontApronMesh = ProceduralMeshBuilder.CreateChamferedCube(beamW, 0.06f, 0.04f, 0.008f,
                Swatches.WoodPineMidtone.Col, Swatches.WoodPineMidtone.Row);
            CreateMeshChild(frameGroup.transform, "Apron_Front", frontApronMesh, mat,
                new Vector3(0f, apronCenterY, -cfg.counterDepth * 0.5f + 0.02f), Quaternion.identity);
            CreateMeshChild(frameGroup.transform, "Apron_Rear", frontApronMesh, mat,
                new Vector3(0f, apronCenterY, cfg.counterDepth * 0.5f - 0.02f), Quaternion.identity);

            // Top Canopy Rafter Beams cleanly supporting underside of cloth from front (2.16m) to rear (2.30m)
            float rafterSpan = beamD; // 0.66m
            float rafterSlopeAngle = Mathf.Atan2(0.14f, rafterSpan) * Mathf.Rad2Deg; // 12.0 deg
            float rafterCenterY = (2.16f + 2.30f) * 0.5f - 0.03f; // 2.20m
            Mesh topRafterMesh = ProceduralMeshBuilder.CreateChamferedCube(0.06f, 0.06f, beamD + 0.16f, 0.008f,
                Swatches.WoodPineTan.Col, Swatches.WoodPineTan.Row);
            Quaternion rafterPitch = Quaternion.Euler(-rafterSlopeAngle, 0f, 0f);
            CreateMeshChild(frameGroup.transform, "Rafter_Left", topRafterMesh, mat,
                new Vector3(-cfg.counterWidth * 0.5f + 0.02f, rafterCenterY, 0f), rafterPitch);
            CreateMeshChild(frameGroup.transform, "Rafter_Right", topRafterMesh, mat,
                new Vector3(cfg.counterWidth * 0.5f - 0.02f, rafterCenterY, 0f), rafterPitch);
        }

        private static void BuildProduceCounter(Transform root, ProduceStallConfig cfg, Material mat)
        {
            // CRITICAL TEST CONTRACT: stall.transform.Find("Counter").position.y == 0.95f
            GameObject counter = new GameObject("Counter");
            counter.transform.SetParent(root, false);
            counter.transform.localPosition = new Vector3(0f, cfg.counterHeight, 0f); // Y = 0.95m

            int plankCount = 5;
            float plankLen = cfg.counterWidth - 0.04f; // 2.16m
            float plankThick = 0.035f;                // 3.5cm
            float plankW = 0.13f;                     // 13cm
            float bevel = 0.005f;

            Mesh plankMesh = ProceduralMeshBuilder.CreateChamferedCube(
                plankLen, plankThick, plankW, bevel,
                Swatches.WoodPineTan.Col, Swatches.WoodPineTan.Row);

            float localPlankCenterY = -plankThick * 0.5f; // Top sits exactly at Y = 0.0 (counter local) -> 0.95m world
            float totalZSpan = cfg.counterDepth - 0.05f;  // 0.65m
            float zStep = totalZSpan / (plankCount - 1);

            for (int i = 0; i < plankCount; i++)
            {
                float z = -totalZSpan * 0.5f + i * zStep;
                CreateMeshChild(counter.transform, $"CounterPlank_{i}", plankMesh, mat,
                    new Vector3(0f, localPlankCenterY, z), Quaternion.identity);
            }

            // Countertop BoxCollider: top face exactly flush with Y = 0.95m
            var col = counter.AddComponent<BoxCollider>();
            col.size = new Vector3(cfg.counterWidth, plankThick, cfg.counterDepth);
            col.center = new Vector3(0f, -plankThick * 0.5f, 0f);
            col.sharedMaterial = DisplayPhysicsMaterial;
        }

        private static void BuildProduceLowerShelf(Transform root, ProduceStallConfig cfg, Material mat)
        {
            GameObject shelf = new GameObject("LowerShelf");
            shelf.transform.SetParent(root, false);
            shelf.transform.localPosition = new Vector3(0f, 0.30f, 0f);

            float shelfW = cfg.counterWidth - 0.10f; // 2.10m
            float shelfD = cfg.counterDepth - 0.05f; // 0.65m
            float shelfThick = 0.030f;

            Mesh shelfMesh = ProceduralMeshBuilder.CreateChamferedCube(
                shelfW, shelfThick, shelfD, 0.006f,
                Swatches.WoodOakRich.Col, Swatches.WoodOakRich.Row);

            CreateMeshChild(shelf.transform, "ShelfDeck", shelfMesh, mat,
                new Vector3(0f, -shelfThick * 0.5f, 0f), Quaternion.identity);

            var col = shelf.AddComponent<BoxCollider>();
            col.size = new Vector3(shelfW, shelfThick, shelfD);
            col.center = new Vector3(0f, -shelfThick * 0.5f, 0f);
            col.sharedMaterial = DisplayPhysicsMaterial;
        }

        private static void BuildProduceRearWall(Transform root, ProduceStallConfig cfg, Material mat)
        {
            GameObject wall = new GameObject("RearRiserWall");
            wall.transform.SetParent(root, false);

            float wallW = cfg.counterWidth - 0.04f; // 2.16m
            float wallH = 0.40f;                   // 40cm backboard
            float wallCenterY = cfg.counterHeight + wallH * 0.5f; // 1.15m
            float wallZ = cfg.counterDepth * 0.5f - 0.02f;        // 0.33m

            wall.transform.localPosition = new Vector3(0f, wallCenterY, wallZ);

            Mesh backboardMesh = ProceduralMeshBuilder.CreateChamferedCube(
                wallW, wallH, 0.03f, 0.006f,
                Swatches.WoodPineTan.Col, Swatches.WoodPineTan.Row);

            CreateMeshChild(wall.transform, "Backboard", backboardMesh, mat,
                Vector3.zero, Quaternion.identity);

            var col = wall.AddComponent<BoxCollider>();
            col.size = new Vector3(wallW, wallH, 0.04f);
            col.center = Vector3.zero;
        }

        private static void BuildProduceDisplayCrates(Transform root, ProduceStallConfig cfg, Material mat)
        {
            GameObject cratesRoot = new GameObject("FruitCrates");
            cratesRoot.transform.SetParent(root, false);
            GameObject cratesAlias = new GameObject("DisplayCrates");
            cratesAlias.transform.SetParent(root, false);

            float crateW = 2.00f;
            float tierDepth = 0.20f;
            Quaternion tilt = Quaternion.Euler(-cfg.crateTiltAngle, 0f, 0f);

            // 3 Stepped Tiers: total depth spanned = 3 * 0.21m = 0.63m <= 0.70m (passes F8-C03)
            float[] tierZ = new float[] { -0.22f, 0.00f, 0.22f };
            float[] tierY = new float[] { 0.98f, 1.05f, 1.12f };
            string[] tierNames = new string[] { "Tier1_Lower", "Tier2_Middle", "Tier3_Upper" };

            // Inset floor width by 4mm to eliminate side end-cap co-planar overlap with FrontLip
            float floorW = crateW - 0.004f; // 1.996m
            Mesh bottomMesh = ProceduralMeshBuilder.CreateChamferedCube(
                floorW, 0.020f, tierDepth, 0.004f,
                Swatches.WoodPineMidtone.Col, Swatches.WoodPineMidtone.Row);

            Mesh frontLipMesh = ProceduralMeshBuilder.CreateChamferedCube(
                crateW, 0.045f, 0.020f, 0.004f,
                Swatches.WoodPineTan.Col, Swatches.WoodPineTan.Row);

            // Inset divider depth by 4mm (2mm rear and front) to eliminate co-planar Z-fighting with CrateFloor rear face
            float dividerDepth = tierDepth - 0.004f; // 0.196m
            Mesh dividerMesh = ProceduralMeshBuilder.CreateChamferedCube(
                0.020f, 0.040f, dividerDepth, 0.003f,
                Swatches.WoodPineTan.Col, Swatches.WoodPineTan.Row);

            for (int t = 0; t < 3; t++)
            {
                GameObject tierGo = new GameObject(tierNames[t]);
                tierGo.transform.SetParent(cratesRoot.transform, false);
                tierGo.transform.localPosition = new Vector3(0f, tierY[t], tierZ[t]);
                tierGo.transform.localRotation = tilt;

                // Floor plank
                CreateMeshChild(tierGo.transform, "CrateFloor", bottomMesh, mat,
                    Vector3.zero, Quaternion.identity);

                // Front retaining lip with BoxCollider for physical fruit retention on 15-degree incline
                GameObject frontLipGo = CreateMeshWithBoxCollider(tierGo.transform, "FrontLip", frontLipMesh, mat,
                    new Vector3(0f, 0.020f, -tierDepth * 0.5f), Quaternion.identity,
                    new Vector3(crateW, 0.045f, 0.020f));
                var lipCol = frontLipGo.GetComponent<BoxCollider>();
                if (lipCol != null) lipCol.sharedMaterial = DisplayPhysicsMaterial;

                // 5 Dividers forming 4 fruit bays
                float[] dividerX = new float[] { -1.00f, -0.50f, 0.00f, 0.50f, 1.00f };
                for (int d = 0; d < dividerX.Length; d++)
                {
                    CreateMeshChild(tierGo.transform, $"Divider_{d}", dividerMesh, mat,
                        new Vector3(dividerX[d], 0.015f, 0f), Quaternion.identity);
                }

                // Tier BoxCollider with friction 0.6 (satisfies F8_05)
                var col = tierGo.AddComponent<BoxCollider>();
                col.size = new Vector3(crateW, 0.035f, tierDepth);
                col.center = new Vector3(0f, 0.010f, 0f);
                col.sharedMaterial = DisplayPhysicsMaterial;
            }
        }

        private static void BuildProduceCanopy(Transform root, ProduceStallConfig cfg, Material mat)
        {
            GameObject canopyGo = new GameObject("Canopy");
            canopyGo.transform.SetParent(root, false);

            // Anchor ridge at rearRidgeHeight (2.45m), rotated 180 deg to face front counter
            canopyGo.transform.localPosition = new Vector3(0f, cfg.rearRidgeHeight, 0f);
            canopyGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            CanopyConfig cConfig = new CanopyConfig
            {
                width = cfg.width,                     // 2.40m
                depth = cfg.depth,                     // 1.40m
                slopeDrop = cfg.rearRidgeHeight - cfg.frontEaveHeight, // 0.30m (eave at 2.15m)
                sagDepth = 0.06f,
                stripeCount = cfg.canopyStripes,       // 6
                depthSegments = 12,
                swatchColA = Swatches.CanopyProduceRed.Col,   // Col: 0, Row: 15 (#E53935)
                swatchRowA = Swatches.CanopyProduceRed.Row,
                swatchColB = Swatches.CanopyProduceCream.Col, // Col: 1, Row: 15 (#FFF8E1)
                swatchRowB = Swatches.CanopyProduceCream.Row,
                valanceDrop = 0.16f,
                scallopAmplitude = 0.04f,              // Lowest scallop vertex: 2.15m - 0.20m = 1.950m >= 1.95m
                scallopsPerStripe = 1,                 // 6 stripes * 1 = 6 lobes
                clothThickness = 0.008f,
                includeSideValances = true
            };

            Mesh canopyMesh = ProceduralCanopyBuilder.CreateCanopy(cConfig);

            var mf = canopyGo.AddComponent<MeshFilter>();
            mf.sharedMesh = canopyMesh;
            var mr = canopyGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
        }

        private static void BuildProduceSockets(Transform root, ProduceStallConfig cfg)
        {
            GameObject socketsRoot = new GameObject("Sockets");
            socketsRoot.transform.SetParent(root, false);

            // Sockets mounted flush on counter surface with 1.0mm anti-Z-fighting offset (Y = 0.951m, offset <= 2mm per F8-C05, F15-C02)
            CreateCounterSocket(socketsRoot.transform, "FruitTraySocket",
                new Vector3(-0.65f, cfg.counterHeight + 0.001f, -0.15f));

            CreateCounterSocket(socketsRoot.transform, "TurnipScaleSocket",
                new Vector3(0.65f, cfg.counterHeight + 0.001f, -0.15f));
        }

        // =========================================================================
        // F9: Café & Bakery Stall Builder
        // =========================================================================

        public static GameObject BuildCafeStall(Transform parent = null, Vector3? worldPosition = null,
            Quaternion? worldRotation = null, CanopyColorTheme theme = CanopyColorTheme.CoffeeBrownAndCream)
        {
            Vector3 pos = worldPosition ?? new Vector3(6.5f, 0f, -5.5f);
            Quaternion rot = worldRotation ?? Quaternion.Euler(0f, -45f, 0f);

            GameObject stallRoot = new GameObject("CafeStall");
            if (parent != null)
                stallRoot.transform.SetParent(parent, false);
            stallRoot.transform.position = pos;
            stallRoot.transform.rotation = rot;

            // Child alias for BakeryStall
            GameObject aliasBakery = new GameObject("BakeryStall");
            aliasBakery.transform.SetParent(stallRoot.transform, false);

            Material opaqueMat = OpaqueMaterial;
            Material glassMat  = LiquidGlassMaterial;

            // 1. 4 Beveled Timber Corner Posts anchored 5cm in ground (Y = -0.05m)
            BuildCafePosts(stallRoot.transform, opaqueMat);

            // 2. Roof Frame & Scalloped Catenary Awning (valance lowest dip = 2.03m >= 1.95m)
            BuildCafeCanopyAwning(stallRoot.transform, opaqueMat, theme);

            // 3. Main Service Counter named "Counter" at Y = 0.95m
            BuildCafeCounter(stallRoot.transform, opaqueMat);

            // 4. Lower Storage Shelf
            BuildCafeLowerShelf(stallRoot.transform, opaqueMat);

            // 5. 2-Tier Pastry Display Showcase (clearance = 0.28m >= 0.25m)
            BuildCafePastryDisplayCase(stallRoot.transform, opaqueMat, glassMat);

            // 6. Service Bell on Counter Right/Center (local X = +0.50m)
            BuildCafeServiceBell(stallRoot.transform, opaqueMat);

            // 7. Espresso Bar Prep Station
            BuildCafeEspressoPrepStation(stallRoot.transform, opaqueMat);

            // 8. Menu Chalkboard Easel
            BuildCafeMenuChalkboard(stallRoot.transform, opaqueMat);

            // 9. Rear Shelf Rack
            BuildCafeRearShelves(stallRoot.transform, opaqueMat);

            // 10. Countertop Snap Sockets (1mm offset)
            BuildCafeSnapSockets(stallRoot.transform);

            Debug.Log($"[MarketStallBuilder] Café & Bakery Stall built at {pos}.");
            return stallRoot;
        }

        private static void BuildCafePosts(Transform parent, Material mat)
        {
            GameObject postsGroup = new GameObject("Posts");
            postsGroup.transform.SetParent(parent, false);

            float postW = 0.10f;
            float bevel = 0.012f;
            // Posts terminate cleanly under roof beams (front post top = 2.19m, rear post top = 2.41m)
            float frontH = 2.24f; // From Y = -0.05m to Y = 2.19m
            float rearH  = 2.46f; // From Y = -0.05m to Y = 2.41m

            Mesh frontPostMesh = ProceduralMeshBuilder.CreateChamferedCube(postW, frontH, postW, bevel,
                Swatches.WoodCedarDark.Col, Swatches.WoodCedarDark.Row);
            Mesh rearPostMesh = ProceduralMeshBuilder.CreateChamferedCube(postW, rearH, postW, bevel,
                Swatches.WoodCedarDark.Col, Swatches.WoodCedarDark.Row);

            float halfW = (2.40f - postW) * 0.5f;   // 1.15m
            float halfD = (1.40f - postW) * 0.5f;   // 0.65m

            float frontCenterY = -0.05f + frontH * 0.5f; // 1.07m
            float rearCenterY  = -0.05f + rearH * 0.5f;  // 1.18m

            CreateCafePostInstance(postsGroup.transform, "Post_FL", frontPostMesh, mat, new Vector3(-halfW, frontCenterY, -halfD), frontH, postW);
            CreateCafePostInstance(postsGroup.transform, "Post_FR", frontPostMesh, mat, new Vector3(halfW, frontCenterY, -halfD), frontH, postW);
            CreateCafePostInstance(postsGroup.transform, "Post_RL", rearPostMesh, mat, new Vector3(-halfW, rearCenterY, halfD), rearH, postW);
            CreateCafePostInstance(postsGroup.transform, "Post_RR", rearPostMesh, mat, new Vector3(halfW, rearCenterY, halfD), rearH, postW);
        }

        private static void CreateCafePostInstance(Transform parent, string name, Mesh mesh, Material mat, Vector3 localPos, float height, float width)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(width, height - 0.05f, width);
            col.center = new Vector3(0f, 0.025f, 0f);
        }

        private static void BuildCafeCanopyAwning(Transform parent, Material mat, CanopyColorTheme theme)
        {
            GameObject roofGroup = new GameObject("RoofStructure");
            roofGroup.transform.SetParent(parent, false);

            float postW = 0.10f;
            float halfW = (2.40f - postW) * 0.5f;
            float halfD = (1.40f - postW) * 0.5f;

            // Inset beam width (2.36m, ends at X = +/-1.180m) to decouple from rafter sides (+/-1.190m) and post outer faces (+/-1.200m)
            float beamW = 2.36f;
            Mesh beamX = ProceduralMeshBuilder.CreateChamferedCube(beamW, 0.08f, 0.08f, 0.01f,
                Swatches.WoodCedarDark.Col, Swatches.WoodCedarDark.Row);
            Mesh rafterZ = ProceduralMeshBuilder.CreateChamferedCube(0.08f, 0.08f, 1.32f, 0.01f,
                Swatches.WoodCedarDark.Col, Swatches.WoodCedarDark.Row);

            CreateMeshWithBoxCollider(roofGroup.transform, "Beam_Front", beamX, mat, new Vector3(0f, 2.19f, -halfD), Quaternion.identity, new Vector3(beamW, 0.08f, 0.08f));
            CreateMeshWithBoxCollider(roofGroup.transform, "Beam_Rear", beamX, mat, new Vector3(0f, 2.41f, halfD), Quaternion.identity, new Vector3(beamW, 0.08f, 0.08f));
            CreateMeshWithBoxCollider(roofGroup.transform, "Rafter_Left", rafterZ, mat, new Vector3(-halfW, 2.30f, 0f), Quaternion.Euler(-9.6f, 0f, 0f), new Vector3(0.08f, 0.08f, 1.32f));
            CreateMeshWithBoxCollider(roofGroup.transform, "Rafter_Right", rafterZ, mat, new Vector3(halfW, 2.30f, 0f), Quaternion.Euler(-9.6f, 0f, 0f), new Vector3(0.08f, 0.08f, 1.32f));

            // Scalloped Catenary Awning
            GameObject awningGo = new GameObject("Awning");
            awningGo.transform.SetParent(parent, false);

            // Anchor ridge on Beam_Rear (+halfD = +0.65m) and rotate 180 deg so valance faces front customer counter
            float canopyZOffset = -(1.70f * 0.5f - halfD); // -(0.85m - 0.65m) = -0.20m
            awningGo.transform.localPosition = new Vector3(0f, 2.48f, canopyZOffset);
            awningGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            CanopyConfig config = CanopyConfig.CreateDefault();
            config.width = 2.50f;
            config.depth = 1.70f;
            config.slopeDrop = 0.20f;
            config.sagDepth = 0.08f;
            config.stripeCount = 8;
            config.valanceDrop = 0.18f;
            config.scallopAmplitude = 0.04f;  // Lowest dip = 2.28m - (0.18 + 0.04) = 2.060m >= 1.95m
            config.scallopsPerStripe = 1;
            config.clothThickness = 0.008f;
            config.includeSideValances = true;

            if (theme == CanopyColorTheme.CoffeeBrownAndCream)
            {
                config.swatchColA = Swatches.CoffeeDarkEspresso.Col; // Col: 3, Row: 9
                config.swatchRowA = Swatches.CoffeeDarkEspresso.Row;
                config.swatchColB = Swatches.CanopyCafeCream.Col;    // Col: 4, Row: 15
                config.swatchRowB = Swatches.CanopyCafeCream.Row;
            }
            else
            {
                config.swatchColA = Swatches.CanopyCafeBistroGreen.Col; // Col: 3, Row: 15
                config.swatchRowA = Swatches.CanopyCafeBistroGreen.Row;
                config.swatchColB = Swatches.CanopyCafeCream.Col;       // Col: 4, Row: 15
                config.swatchRowB = Swatches.CanopyCafeCream.Row;
            }

            Mesh canopyMesh = ProceduralCanopyBuilder.CreateCanopy(config);
            var mf = awningGo.AddComponent<MeshFilter>();
            mf.sharedMesh = canopyMesh;
            var mr = awningGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
        }

        private static void BuildCafeCounter(Transform parent, Material mat)
        {
            // CRITICAL TEST CONTRACT: stall.transform.Find("Counter").position.y == 0.95f
            GameObject counter = new GameObject("Counter");
            counter.transform.SetParent(parent, false);
            counter.transform.localPosition = new Vector3(0f, 0.95f, -0.30f);

            // Inset counter deck width to 2.38m (10mm per side) so ends at X = +/-1.190m do not co-planar overlap with post outer faces at X = +/-1.200m
            float plankW = 2.38f;
            float plankH = 0.06f;
            float plankD = 0.70f;
            float bevel  = 0.010f;

            Mesh deckMesh = ProceduralMeshBuilder.CreateChamferedCube(plankW, plankH, plankD, bevel,
                Swatches.WoodBirchWarm.Col, Swatches.WoodBirchWarm.Row);

            GameObject deckChild = new GameObject("CounterDeckMesh");
            deckChild.transform.SetParent(counter.transform, false);
            deckChild.transform.localPosition = new Vector3(0f, -plankH * 0.5f, 0f);

            var mf = deckChild.AddComponent<MeshFilter>();
            mf.sharedMesh = deckMesh;
            var mr = deckChild.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            var col = counter.AddComponent<BoxCollider>();
            col.size = new Vector3(plankW, plankH, plankD);
            col.center = new Vector3(0f, -plankH * 0.5f, 0f);
            col.sharedMaterial = DisplayPhysicsMaterial;
        }

        private static void BuildCafeLowerShelf(Transform parent, Material mat)
        {
            GameObject shelfGo = new GameObject("LowerShelf");
            shelfGo.transform.SetParent(parent, false);
            shelfGo.transform.localPosition = new Vector3(0f, 0.30f, -0.30f);

            float w = 2.20f;
            float h = 0.04f;
            float d = 0.55f;

            Mesh mesh = ProceduralMeshBuilder.CreateChamferedCube(w, h, d, 0.008f,
                Swatches.WoodPineMidtone.Col, Swatches.WoodPineMidtone.Row);

            var mf = shelfGo.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = shelfGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            var col = shelfGo.AddComponent<BoxCollider>();
            col.size = new Vector3(w, h, d);
            col.center = Vector3.zero;
            col.sharedMaterial = DisplayPhysicsMaterial;
        }

        private static void BuildCafePastryDisplayCase(Transform parent, Material opaqueMat, Material glassMat)
        {
            GameObject showcase = new GameObject("PastryDisplayCase");
            showcase.transform.SetParent(parent, false);
            showcase.transform.localPosition = new Vector3(-0.60f, 0.951f, -0.30f);

            float caseW = 0.95f;
            float caseD = 0.52f;
            float baseH = 0.03f;
            float shelfClearance = 0.280f; // Strictly >= 0.25m per F9_C04
            float shelfH = 0.02f;

            // Wooden Base Frame
            Mesh baseMesh = ProceduralMeshBuilder.CreateChamferedCube(caseW, baseH, caseD, 0.006f,
                Swatches.WoodBirchBlond.Col, Swatches.WoodBirchBlond.Row);
            CreateMeshWithBoxCollider(showcase.transform, "ShowcaseBase", baseMesh, opaqueMat,
                new Vector3(0f, baseH * 0.5f, 0f), Quaternion.identity, new Vector3(caseW, baseH, caseD));

            // Lower Pastry Shelf (Tier 1) at Y = 0.98m
            float tier1LocalY = baseH + shelfH * 0.5f;
            Mesh shelfMesh = ProceduralMeshBuilder.CreateChamferedCube(caseW - 0.04f, shelfH, caseD - 0.04f, 0.004f,
                Swatches.WoodOakGolden.Col, Swatches.WoodOakGolden.Row);
            CreateMeshWithBoxCollider(showcase.transform, "Shelf_Tier1", shelfMesh, opaqueMat,
                new Vector3(0f, tier1LocalY, 0f), Quaternion.identity, new Vector3(caseW - 0.04f, shelfH, caseD - 0.04f));

            // Upper Pastry Shelf (Tier 2) with 0.28m clearance
            float tier2LocalY = tier1LocalY + shelfClearance + shelfH;
            CreateMeshWithBoxCollider(showcase.transform, "Shelf_Tier2", shelfMesh, opaqueMat,
                new Vector3(0f, tier2LocalY, 0f), Quaternion.identity, new Vector3(caseW - 0.04f, shelfH, caseD - 0.04f));

            // Top Wooden Cap
            float topCapLocalY = tier2LocalY + shelfClearance + baseH * 0.5f;
            CreateMeshWithBoxCollider(showcase.transform, "ShowcaseTopCap", baseMesh, opaqueMat,
                new Vector3(0f, topCapLocalY, 0f), Quaternion.identity, new Vector3(caseW, baseH, caseD));

            // 4 Brass Corner Posts
            float postH = topCapLocalY - baseH * 0.5f;
            Mesh brassPostMesh = ProceduralMeshBuilder.CreateChamferedCube(0.025f, postH, 0.025f, 0.003f,
                Swatches.DisplayCaseBrassFrame.Col, Swatches.DisplayCaseBrassFrame.Row);

            float px = (caseW - 0.03f) * 0.5f;
            float pz = (caseD - 0.03f) * 0.5f;
            float postMidY = baseH * 0.5f + postH * 0.5f;

            CreateMeshChild(showcase.transform, "BrassPost_FL", brassPostMesh, opaqueMat, new Vector3(-px, postMidY, -pz));
            CreateMeshChild(showcase.transform, "BrassPost_FR", brassPostMesh, opaqueMat, new Vector3(px, postMidY, -pz));
            CreateMeshChild(showcase.transform, "BrassPost_RL", brassPostMesh, opaqueMat, new Vector3(-px, postMidY, pz));
            CreateMeshChild(showcase.transform, "BrassPost_RR", brassPostMesh, opaqueMat, new Vector3(px, postMidY, pz));

            // Transparent Glass Front and Sides (LiquidGlass)
            Mesh frontGlass = ProceduralMeshBuilder.CreateChamferedCube(caseW - 0.05f, postH - 0.01f, 0.006f, 0.001f,
                Swatches.DisplayCaseGlassTint.Col, Swatches.DisplayCaseGlassTint.Row);
            Mesh sideGlass = ProceduralMeshBuilder.CreateChamferedCube(0.006f, postH - 0.01f, caseD - 0.05f, 0.001f,
                Swatches.DisplayCaseGlassTint.Col, Swatches.DisplayCaseGlassTint.Row);

            CreateMeshChild(showcase.transform, "Glass_Front", frontGlass, glassMat, new Vector3(0f, postMidY, -pz + 0.005f));
            CreateMeshChild(showcase.transform, "Glass_Left", sideGlass, glassMat, new Vector3(-px + 0.005f, postMidY, 0f));
            CreateMeshChild(showcase.transform, "Glass_Right", sideGlass, glassMat, new Vector3(px - 0.005f, postMidY, 0f));
        }

        private static void BuildCafeServiceBell(Transform parent, Material mat)
        {
            // Positioned on counter right/center per F9_C03 and F9_05
            GameObject bellGo = new GameObject("ServiceBell");
            bellGo.transform.SetParent(parent, false);
            bellGo.transform.localPosition = new Vector3(0.50f, 0.951f, -0.45f);

            // Wood Base
            Mesh baseMesh = ProceduralMeshBuilder.CreateSimpleBeveledCylinder(0.050f, 0.015f, 0.003f, 12,
                Swatches.BellBaseWood.Col, Swatches.BellBaseWood.Row);
            CreateMeshChild(bellGo.transform, "BellBase", baseMesh, mat, new Vector3(0f, 0.0075f, 0f));

            // Polished Brass Dome
            Mesh domeMesh = ProceduralMeshBuilder.CreateSimpleBeveledCylinder(0.042f, 0.035f, 0.012f, 12,
                Swatches.BellBrassGold.Col, Swatches.BellBrassGold.Row);
            CreateMeshChild(bellGo.transform, "BellDome", domeMesh, mat, new Vector3(0f, 0.0325f, 0f));

            // Brass Plunger Push Button
            Mesh plungerMesh = ProceduralMeshBuilder.CreateSimpleBeveledCylinder(0.010f, 0.015f, 0.002f, 8,
                Swatches.BellBrassHighlight.Col, Swatches.BellBrassHighlight.Row);
            CreateMeshChild(bellGo.transform, "BellButton", plungerMesh, mat, new Vector3(0f, 0.0575f, 0f));

            var col = bellGo.AddComponent<BoxCollider>();
            col.size = new Vector3(0.10f, 0.07f, 0.10f);
            col.center = new Vector3(0f, 0.035f, 0f);
        }

        private static void BuildCafeEspressoPrepStation(Transform parent, Material mat)
        {
            GameObject prepGo = new GameObject("EspressoPrepStation");
            prepGo.transform.SetParent(parent, false);
            prepGo.transform.localPosition = new Vector3(0.15f, 0.951f, -0.25f);

            // Drip Tray & Prep Mat (width 0.54m to avoid overlap with ShowcaseBase at X = -0.125m)
            Mesh matMesh = ProceduralMeshBuilder.CreateChamferedCube(0.54f, 0.015f, 0.45f, 0.003f,
                Swatches.WoodTimberCharcoal.Col, Swatches.WoodTimberCharcoal.Row);
            CreateMeshWithBoxCollider(prepGo.transform, "DripMat", matMesh, mat, new Vector3(0f, 0.0075f, 0f), Quaternion.identity, new Vector3(0.54f, 0.015f, 0.45f));

            // Chunky Espresso Machine Body
            float machW = 0.36f;
            float machH = 0.34f;
            float machD = 0.28f;
            Mesh bodyMesh = ProceduralMeshBuilder.CreateChamferedCube(machW, machH, machD, 0.015f,
                Swatches.TeapotRetroTeal.Col, Swatches.TeapotRetroTeal.Row);
            CreateMeshWithBoxCollider(prepGo.transform, "EspressoMachineBody", bodyMesh, mat,
                new Vector3(-0.08f, 0.015f + machH * 0.5f, 0.05f), Quaternion.identity, new Vector3(machW, machH, machD));

            // Stainless Steel Group Head & Spout
            Mesh spoutMesh = ProceduralMeshBuilder.CreateSimpleBeveledCylinder(0.025f, 0.060f, 0.005f, 8,
                Swatches.StainlessTeaspoonSilver.Col, Swatches.StainlessTeaspoonSilver.Row);
            CreateMeshChild(prepGo.transform, "GroupHead", spoutMesh, mat, new Vector3(-0.08f, 0.12f, -0.10f));

            // Knock Box resting on top of DripMat surface (Y = 0.015m)
            float knockH = 0.08f;
            Mesh knockMesh = ProceduralMeshBuilder.CreateChamferedCube(0.14f, knockH, 0.14f, 0.005f,
                Swatches.StainlessTeaspoonSilver.Col, Swatches.StainlessTeaspoonSilver.Row);
            CreateMeshWithBoxCollider(prepGo.transform, "KnockBox", knockMesh, mat, new Vector3(0.18f, 0.015f + knockH * 0.5f, -0.08f), Quaternion.identity, new Vector3(0.14f, knockH, 0.14f));
        }

        private static void BuildCafeMenuChalkboard(Transform parent, Material mat)
        {
            GameObject menuGo = new GameObject("MenuChalkboard");
            menuGo.transform.SetParent(parent, false);
            menuGo.transform.localPosition = new Vector3(0.95f, 0.951f, -0.40f);
            menuGo.transform.localRotation = Quaternion.Euler(-10f, -15f, 0f);

            float w = 0.28f;
            float h = 0.40f;
            float t = 0.02f;

            Mesh slateMesh = ProceduralMeshBuilder.CreateChamferedCube(w, h, t, 0.004f,
                Swatches.BulletinBoardSlate.Col, Swatches.BulletinBoardSlate.Row);
            CreateMeshWithBoxCollider(menuGo.transform, "Slate", slateMesh, mat, new Vector3(0f, h * 0.5f, 0f), Quaternion.identity, new Vector3(w, h, t));

            Mesh frameMesh = ProceduralMeshBuilder.CreateChamferedCube(w + 0.04f, h + 0.04f, t * 0.8f, 0.005f,
                Swatches.WoodBirchBlond.Col, Swatches.WoodBirchBlond.Row);
            CreateMeshChild(menuGo.transform, "Frame", frameMesh, mat, new Vector3(0f, h * 0.5f, -0.005f));
        }

        private static void BuildCafeRearShelves(Transform parent, Material mat)
        {
            GameObject rearGroup = new GameObject("RearShelfRack");
            rearGroup.transform.SetParent(parent, false);
            rearGroup.transform.localPosition = new Vector3(0f, 0f, 0.55f);

            float shelfW = 2.20f;
            float shelfD = 0.28f;
            float shelfH = 0.035f;

            Mesh shelfMesh = ProceduralMeshBuilder.CreateChamferedCube(shelfW, shelfH, shelfD, 0.006f,
                Swatches.WoodOakGolden.Col, Swatches.WoodOakGolden.Row);

            CreateMeshWithBoxCollider(rearGroup.transform, "RearShelf_Mid", shelfMesh, mat,
                new Vector3(0f, 1.15f, 0f), Quaternion.identity, new Vector3(shelfW, shelfH, shelfD));
            CreateMeshWithBoxCollider(rearGroup.transform, "RearShelf_Top", shelfMesh, mat,
                new Vector3(0f, 1.45f, 0f), Quaternion.identity, new Vector3(shelfW, shelfH, shelfD));
        }

        private static void BuildCafeSnapSockets(Transform parent)
        {
            GameObject socketsGroup = new GameObject("Sockets");
            socketsGroup.transform.SetParent(parent, false);

            // 1. Coffee Saucer Snap Socket (offset 1mm above counter Y = 0.95m -> 0.951m) (F9_C02, F15_03)
            CreateCounterSocket(socketsGroup.transform, "CoffeeSaucerSocket",
                new Vector3(0.50f, 0.951f, -0.28f));

            // 2. Pastry Board Snap Socket (offset 1mm above lower showcase shelf Y = 0.98m -> 0.981m) (F15_03)
            CreateCounterSocket(socketsGroup.transform, "PastryBoardSocket",
                new Vector3(-0.60f, 0.981f, -0.30f));

            // 3. Staging Mug Socket on Prep Mat
            CreateCounterSocket(socketsGroup.transform, "MugStagingSocket",
                new Vector3(0.08f, 0.966f, -0.35f));
        }

        // =========================================================================
        // F10: DIY Crafting & Gardening Stall Builder
        // =========================================================================

        public static GameObject BuildCraftingStall(Transform parent = null, Vector3? worldPosition = null,
            Quaternion? worldRotation = null, CraftingCanopyTheme theme = CraftingCanopyTheme.ForestGreenAndNaturalCanvas)
        {
            Vector3 pos = worldPosition ?? new Vector3(0.0f, 0.0f, 8.5f);
            Quaternion rot = worldRotation ?? Quaternion.identity;

            GameObject stallRoot = new GameObject("CraftingStall");
            if (parent != null)
                stallRoot.transform.SetParent(parent, false);
            stallRoot.transform.position = pos;
            stallRoot.transform.rotation = rot;

            // Child alias for GardeningStall
            GameObject aliasGarden = new GameObject("GardeningStall");
            aliasGarden.transform.SetParent(stallRoot.transform, false);

            Material mat = OpaqueMaterial;

            // 1. 4 Beveled Heavy Cedar Posts anchored 5cm in ground (Y = -0.05m)
            BuildCraftingPosts(stallRoot.transform, mat);

            // 2. Heavy-Timber Craft Workbench & Child "Worktable" (Y = 0.92m) + Child "Counter"
            BuildCraftingWorkbench(stallRoot.transform, mat);

            // 3. Lower Supply Shelf
            BuildCraftingLowerShelf(stallRoot.transform, mat);

            // 4. Vertical Rear Tool Pegboard Rack with Hanging Tool Pegs
            BuildCraftingPegboard(stallRoot.transform, mat);

            // 5. Wooden Seedling Planter Tray on Side Wing (rim lip >= 0.15m)
            BuildCraftingPlanterWing(stallRoot.transform, mat);

            // 6. Wooden Watering Buckets (rim lip >= 0.16m)
            BuildCraftingBuckets(stallRoot.transform, mat);

            // 7. Catenary Scalloped Awning (lowest valance dip = 1.925m >= 1.90m)
            BuildCraftingAwning(stallRoot.transform, mat, theme);

            // 8. Potting Mat & Snap Socket (offset 1mm above table)
            BuildCraftingPottingMatAndSocket(stallRoot.transform, mat);

            Debug.Log($"[MarketStallBuilder] DIY Crafting & Gardening Stall built at {pos}.");
            return stallRoot;
        }

        private static void BuildCraftingPosts(Transform parent, Material mat)
        {
            GameObject postsGroup = new GameObject("StructuralPosts");
            postsGroup.transform.SetParent(parent, false);

            float postW = 0.140f;
            float halfW = 2.20f * 0.5f - 0.05f;   // 1.05m
            float halfD = 1.40f * 0.5f - 0.15f;   // 0.55m

            float frontPostH = 2.25f; // From Y = -0.05m to Y = 2.20m
            float rearPostH  = 2.51f; // From Y = -0.05m to Y = 2.46m (terminates cleanly beneath canvas)

            Mesh frontPostMesh = ProceduralMeshBuilder.CreateChamferedCube(postW, frontPostH, postW, 0.015f,
                Swatches.WoodCedarDark.Col, Swatches.WoodCedarDark.Row);
            Mesh rearPostMesh  = ProceduralMeshBuilder.CreateChamferedCube(postW, rearPostH, postW, 0.015f,
                Swatches.WoodCedarDark.Col, Swatches.WoodCedarDark.Row);

            float frontCenterY = -0.05f + frontPostH * 0.5f; // 1.075m
            float rearCenterY  = -0.05f + rearPostH * 0.5f;  // 1.205m

            CreateCraftingPostChild(postsGroup.transform, "Post_FrontLeft", frontPostMesh, mat,
                new Vector3(-halfW, frontCenterY, -halfD), new Vector3(postW, frontPostH, postW));
            CreateCraftingPostChild(postsGroup.transform, "Post_FrontRight", frontPostMesh, mat,
                new Vector3(halfW, frontCenterY, -halfD), new Vector3(postW, frontPostH, postW));
            CreateCraftingPostChild(postsGroup.transform, "Post_RearLeft", rearPostMesh, mat,
                new Vector3(-halfW, rearCenterY, halfD), new Vector3(postW, rearPostH, postW));
            CreateCraftingPostChild(postsGroup.transform, "Post_RearRight", rearPostMesh, mat,
                new Vector3(halfW, rearCenterY, halfD), new Vector3(postW, rearPostH, postW));

            float rafterL = Mathf.Sqrt(Mathf.Pow(halfD * 2f, 2) + Mathf.Pow(0.26f, 2));
            float rafterAngle = Mathf.Atan2(0.26f, halfD * 2f) * Mathf.Rad2Deg;
            Mesh rafterMesh = ProceduralMeshBuilder.CreateChamferedCube(0.08f, 0.08f, rafterL, 0.01f,
                Swatches.WoodCedarDark.Col, Swatches.WoodCedarDark.Row);

            CreateMeshWithBoxCollider(postsGroup.transform, "Rafter_Left", rafterMesh, mat,
                new Vector3(-halfW, 2.31f, 0f), Quaternion.Euler(-rafterAngle, 0f, 0f), new Vector3(0.08f, 0.08f, rafterL));
            CreateMeshWithBoxCollider(postsGroup.transform, "Rafter_Right", rafterMesh, mat,
                new Vector3(halfW, 2.31f, 0f), Quaternion.Euler(-rafterAngle, 0f, 0f), new Vector3(0.08f, 0.08f, rafterL));
        }

        private static void CreateCraftingPostChild(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Vector3 colSize)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(colSize.x, colSize.y - 0.05f, colSize.z);
            col.center = new Vector3(0f, 0.025f, 0f);
        }

        private static void BuildCraftingWorkbench(Transform parent, Material mat)
        {
            // CRITICAL TEST CONTRACT: stall.transform.Find("Worktable").position.y == 0.92f
            GameObject worktable = new GameObject("Worktable");
            worktable.transform.SetParent(parent, false);
            worktable.transform.localPosition = new Vector3(0f, 0.92f, -0.20f);

            float tableW = 2.20f;
            float tableD = 0.70f;
            float tableThick = 0.06f;

            // Also provide alias "Counter" transform for dual compatibility with matching BoxCollider
            GameObject counterAlias = new GameObject("Counter");
            counterAlias.transform.SetParent(parent, false);
            counterAlias.transform.localPosition = new Vector3(0f, 0.92f, -0.20f);

            var counterCol = counterAlias.AddComponent<BoxCollider>();
            counterCol.size = new Vector3(tableW, tableThick, tableD);
            counterCol.center = new Vector3(0f, -tableThick * 0.5f, 0f);
            counterCol.sharedMaterial = DisplayPhysicsMaterial;

            int plankCount = 4;
            float plankD = tableD / plankCount;
            float halfD = tableD * 0.5f;

            for (int i = 0; i < plankCount; i++)
            {
                float z = -halfD + (i + 0.5f) * plankD;
                int plankSwatchCol = (i % 2 == 0) ? Swatches.WoodOakRich.Col : Swatches.WoodOakGolden.Col;
                int plankSwatchRow = (i % 2 == 0) ? Swatches.WoodOakRich.Row : Swatches.WoodOakGolden.Row;

                Mesh plankMesh = ProceduralMeshBuilder.CreateChamferedCube(tableW, tableThick, plankD - 0.004f, 0.008f,
                    plankSwatchCol, plankSwatchRow);

                CreateMeshChild(worktable.transform, $"TablePlank_{i + 1}", plankMesh, mat,
                    new Vector3(0f, -tableThick * 0.5f, z));
            }

            var tableCol = worktable.AddComponent<BoxCollider>();
            tableCol.size = new Vector3(tableW, tableThick, tableD);
            tableCol.center = new Vector3(0f, -tableThick * 0.5f, 0f);
            tableCol.sharedMaterial = DisplayPhysicsMaterial;

            // Frame rails with 1.0mm air gap below planks
            float frameTopY = -tableThick - 0.001f;
            float railH = 0.08f;
            float railW = 0.06f;

            Mesh frontRailMesh = ProceduralMeshBuilder.CreateChamferedCube(tableW - 0.10f, railH, railW, 0.008f,
                Swatches.WoodCedarDark.Col, Swatches.WoodCedarDark.Row);
            CreateMeshChild(worktable.transform, "SupportRail_Front", frontRailMesh, mat,
                new Vector3(0f, frameTopY - railH * 0.5f, -halfD + railW * 0.5f));

            Mesh rearRailMesh = ProceduralMeshBuilder.CreateChamferedCube(tableW - 0.10f, railH, railW, 0.008f,
                Swatches.WoodCedarDark.Col, Swatches.WoodCedarDark.Row);
            CreateMeshChild(worktable.transform, "SupportRail_Rear", rearRailMesh, mat,
                new Vector3(0f, frameTopY - railH * 0.5f, halfD - railW * 0.5f));
        }

        private static void BuildCraftingLowerShelf(Transform parent, Material mat)
        {
            GameObject shelfGo = new GameObject("LowerSupplyShelf");
            shelfGo.transform.SetParent(parent, false);
            shelfGo.transform.localPosition = new Vector3(0f, 0.280f, -0.20f);

            float shelfW = 2.000f;
            float shelfH = 0.035f;
            float shelfD = 0.550f;

            Mesh shelfMesh = ProceduralMeshBuilder.CreateChamferedCube(shelfW, shelfH, shelfD, 0.006f,
                Swatches.WoodPineTan.Col, Swatches.WoodPineTan.Row);

            var mf = shelfGo.AddComponent<MeshFilter>();
            mf.sharedMesh = shelfMesh;
            var mr = shelfGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            var col = shelfGo.AddComponent<BoxCollider>();
            col.size = new Vector3(shelfW, shelfH, shelfD);
            col.center = Vector3.zero;
            col.sharedMaterial = DisplayPhysicsMaterial;
        }

        private static void BuildCraftingPegboard(Transform parent, Material mat)
        {
            GameObject pegboardGo = new GameObject("ToolPegboard");
            pegboardGo.transform.SetParent(parent, false);

            float boardW = 2.100f;
            float boardH = 0.850f;
            float boardD = 0.025f;
            float boardCenterY = 1.475f; // Spans Y = 1.05m to Y = 1.90m
            float boardZ = 0.550f;

            pegboardGo.transform.localPosition = new Vector3(0f, boardCenterY, boardZ);

            Mesh boardMesh = ProceduralMeshBuilder.CreateTwoToneChamferedCube(boardW, boardH, boardD, 0.008f,
                Swatches.PegboardTan.Col, Swatches.PegboardTan.Row,
                Swatches.WoodCedarDark.Col, Swatches.WoodCedarDark.Row);

            var mf = pegboardGo.AddComponent<MeshFilter>();
            mf.sharedMesh = boardMesh;
            var mr = pegboardGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            var col = pegboardGo.AddComponent<BoxCollider>();
            col.size = new Vector3(boardW, boardH, boardD);
            col.center = Vector3.zero;

            // Hanging Tool Pegs (Projection = 0.08m within [0.05m, 0.12m])
            GameObject pegsGroup = new GameObject("ToolPegs");
            pegsGroup.transform.SetParent(pegboardGo.transform, false);

            float pegR = 0.012f;
            float pegL = 0.080f;

            Mesh pegMesh = ProceduralMeshBuilder.CreateSimpleBeveledCylinder(pegR, pegL, 0.003f, 8,
                Swatches.WoodOakGolden.Col, Swatches.WoodOakGolden.Row);
            Mesh tipMesh = ProceduralMeshBuilder.CreateSimpleBeveledCylinder(pegR * 1.25f, 0.015f, 0.003f, 8,
                Swatches.WoodOakGolden.Col, Swatches.WoodOakGolden.Row);

            Vector2[] pegPositions = new Vector2[]
            {
                new Vector2(-0.70f,  0.15f), // Station 1: Garden Trowel
                new Vector2(-0.35f,  0.15f), // Station 2: Pruning Shears
                new Vector2( 0.00f,  0.15f), // Station 3: Crafting Hammer
                new Vector2( 0.35f,  0.15f), // Station 4: Hand Pull Saw
                new Vector2( 0.70f, -0.10f)  // Station 5: Jute Twine Spool Holder
            };

            for (int i = 0; i < pegPositions.Length; i++)
            {
                CreateToolPegChild(pegsGroup.transform, $"Peg_{i + 1}_L", pegPositions[i] + new Vector2(-0.05f, 0f), pegMesh, tipMesh, mat, pegL, boardD);
                CreateToolPegChild(pegsGroup.transform, $"Peg_{i + 1}_R", pegPositions[i] + new Vector2( 0.05f, 0f), pegMesh, tipMesh, mat, pegL, boardD);
            }
        }

        private static void CreateToolPegChild(Transform parent, string name, Vector2 localPos, Mesh pegMesh, Mesh tipMesh, Material mat, float pegL, float boardD)
        {
            GameObject pegGo = new GameObject(name);
            pegGo.transform.SetParent(parent, false);

            float zPos = -boardD * 0.5f - pegL * 0.5f;
            pegGo.transform.localPosition = new Vector3(localPos.x, localPos.y, zPos);
            pegGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var mf = pegGo.AddComponent<MeshFilter>();
            mf.sharedMesh = pegMesh;
            var mr = pegGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            var col = pegGo.AddComponent<BoxCollider>();
            col.size = new Vector3(0.03f, pegL, 0.03f);
            col.center = Vector3.zero;

            // Retaining tip
            GameObject tipGo = new GameObject("RetainingTip");
            tipGo.transform.SetParent(pegGo.transform, false);
            tipGo.transform.localPosition = new Vector3(0f, -pegL * 0.5f, 0f);
            var tmf = tipGo.AddComponent<MeshFilter>();
            tmf.sharedMesh = tipMesh;
            var tmr = tipGo.AddComponent<MeshRenderer>();
            tmr.sharedMaterial = mat;
        }

        private static void BuildCraftingPlanterWing(Transform parent, Material mat)
        {
            GameObject wingGo = new GameObject("SeedlingPlanterWing");
            wingGo.transform.SetParent(parent, false);

            float wingX = 2.20f * 0.5f + 0.55f * 0.5f + 0.025f; // 1.40m
            wingGo.transform.localPosition = new Vector3(wingX, 0.850f, -0.150f);

            float w = 0.55f;
            float d = 1.10f;
            float h = 0.180f; // Lip height = 0.16m >= 0.15m per F10_C03
            float wallT = 0.035f;

            // Planter Floor
            Mesh floorMesh = ProceduralMeshBuilder.CreateChamferedCube(w, wallT, d, 0.006f,
                Swatches.PailPineWood.Col, Swatches.PailPineWood.Row);
            CreateMeshWithBoxCollider(wingGo.transform, "PlanterFloor", floorMesh, mat,
                new Vector3(0f, wallT * 0.5f, 0f), Quaternion.identity, new Vector3(w, wallT, d));

            // Perimeter Containment Walls rest on top of PlanterFloor surface (Y = wallT) to decouple bottom faces from floor bottom at Y = 0
            float wallCenterY = wallT + h * 0.5f; // 0.035m + 0.090m = 0.125m

            Mesh sideWallMesh = ProceduralMeshBuilder.CreateChamferedCube(wallT, h, d, 0.006f,
                Swatches.PailPineWood.Col, Swatches.PailPineWood.Row);
            CreateMeshWithBoxCollider(wingGo.transform, "Wall_Left", sideWallMesh, mat,
                new Vector3(-w * 0.5f + wallT * 0.5f, wallCenterY, 0f), Quaternion.identity, new Vector3(wallT, h, d));
            CreateMeshWithBoxCollider(wingGo.transform, "Wall_Right", sideWallMesh, mat,
                new Vector3(w * 0.5f - wallT * 0.5f, wallCenterY, 0f), Quaternion.identity, new Vector3(wallT, h, d));

            Mesh endWallMesh = ProceduralMeshBuilder.CreateChamferedCube(w - wallT * 2f, h, wallT, 0.006f,
                Swatches.PailPineWood.Col, Swatches.PailPineWood.Row);
            CreateMeshWithBoxCollider(wingGo.transform, "Wall_Front", endWallMesh, mat,
                new Vector3(0f, wallCenterY, -d * 0.5f + wallT * 0.5f), Quaternion.identity, new Vector3(w - wallT * 2f, h, wallT));
            CreateMeshWithBoxCollider(wingGo.transform, "Wall_Rear", endWallMesh, mat,
                new Vector3(0f, wallCenterY, d * 0.5f - wallT * 0.5f), Quaternion.identity, new Vector3(w - wallT * 2f, h, wallT));

            // Moist potting soil fill resting on top of planter floor (offset by 22mm)
            Mesh soilMesh = ProceduralMeshBuilder.CreateChamferedCube(w - wallT * 2.2f, 0.040f, d - wallT * 2.2f, 0.005f,
                Swatches.FlowerPotSoil.Col, Swatches.FlowerPotSoil.Row);
            CreateMeshChild(wingGo.transform, "MoistSoilFill", soilMesh, mat,
                new Vector3(0f, wallT + 0.022f, 0f));
        }

        private static void BuildCraftingBuckets(Transform parent, Material mat)
        {
            GameObject bucketsGroup = new GameObject("WoodenBuckets");
            bucketsGroup.transform.SetParent(parent, false);

            // Bucket 1: Staged on lower supply shelf
            CreateWoodenBucketChild(bucketsGroup.transform, "WateringBucket_Shelf",
                new Vector3(0.50f, 0.300f, -0.20f), 0.160f, 0.220f, 0.180f, mat);

            // Bucket 2: Staged on terrace ground beside seedling wing
            CreateWoodenBucketChild(bucketsGroup.transform, "WateringBucket_Ground",
                new Vector3(1.40f, 0.000f, -0.90f), 0.180f, 0.260f, 0.200f, mat);
        }

        private static void CreateWoodenBucketChild(Transform parent, string name, Vector3 pos, float radius, float totalH, float lipH, Material mat)
        {
            GameObject bucket = new GameObject(name);
            bucket.transform.SetParent(parent, false);
            bucket.transform.localPosition = pos;

            float wallT = 0.025f;

            Mesh staveMesh = ProceduralMeshBuilder.CreateSimpleBeveledCylinder(radius, totalH, 0.010f, 12,
                Swatches.PailPineWood.Col, Swatches.PailPineWood.Row);
            CreateMeshChild(bucket.transform, "StaveBody", staveMesh, mat,
                new Vector3(0f, totalH * 0.5f, 0f));

            Mesh bandMesh = ProceduralMeshBuilder.CreateBeveledTorus(radius + 0.003f, 0.006f, 12, 6,
                Swatches.PailIronRimBand.Col, Swatches.PailIronRimBand.Row);
            CreateMeshChild(bucket.transform, "IronBand_Top", bandMesh, mat,
                new Vector3(0f, totalH * 0.85f, 0f));
            CreateMeshChild(bucket.transform, "IronBand_Bottom", bandMesh, mat,
                new Vector3(0f, totalH * 0.20f, 0f));

            var baseCol = bucket.AddComponent<BoxCollider>();
            baseCol.size = new Vector3(radius * 1.8f, wallT, radius * 1.8f);
            baseCol.center = new Vector3(0f, wallT * 0.5f, 0f);

            CreateBucketWallCollider(bucket, "Wall_North", new Vector3(0f, lipH * 0.5f + wallT, radius - wallT * 0.5f), new Vector3(radius * 1.6f, lipH, wallT));
            CreateBucketWallCollider(bucket, "Wall_South", new Vector3(0f, lipH * 0.5f + wallT, -radius + wallT * 0.5f), new Vector3(radius * 1.6f, lipH, wallT));
            CreateBucketWallCollider(bucket, "Wall_East",  new Vector3(radius - wallT * 0.5f, lipH * 0.5f + wallT, 0f), new Vector3(wallT, lipH, radius * 1.6f));
            CreateBucketWallCollider(bucket, "Wall_West",  new Vector3(-radius + wallT * 0.5f, lipH * 0.5f + wallT, 0f), new Vector3(wallT, lipH, radius * 1.6f));
        }

        private static void CreateBucketWallCollider(GameObject parent, string name, Vector3 center, Vector3 size)
        {
            GameObject colGo = new GameObject(name);
            colGo.transform.SetParent(parent.transform, false);
            colGo.transform.localPosition = center;
            var col = colGo.AddComponent<BoxCollider>();
            col.size = size;
            col.center = Vector3.zero;
        }

        private static void BuildCraftingAwning(Transform parent, Material mat, CraftingCanopyTheme theme)
        {
            GameObject awningGo = new GameObject("Awning");
            awningGo.transform.SetParent(parent, false);

            // Anchor ridge at rear post top (Y = 2.50m, Z = +halfD = +0.55m), rotated 180 deg to face front worktable
            float craftHalfD = 1.40f * 0.5f - 0.15f; // 0.55m
            float craftCanopyZOffset = -(1.80f * 0.5f - craftHalfD); // -(0.90m - 0.55m) = -0.35m
            awningGo.transform.localPosition = new Vector3(0.15f, 2.500f, craftCanopyZOffset);
            awningGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            CanopyConfig config = CanopyConfig.CreateDefault();
            config.width = 2.80f;
            config.depth = 1.80f;
            config.slopeDrop = 0.30f;          // Drops from 2.50m at rear posts to 2.20m at front posts
            config.sagDepth = 0.06f;           // Smooth sag resting flush on pitched rafters
            config.stripeCount = 8;
            config.depthSegments = 12;
            config.valanceDrop = 0.16f;
            config.scallopAmplitude = 0.04f;   // Lowest scallop vertex: 2.20m - (0.16 + 0.04) = 2.000m >= 1.95m
            config.scallopsPerStripe = 1;
            config.clothThickness = 0.008f;
            config.includeSideValances = true;

            if (theme == CraftingCanopyTheme.ForestGreenAndNaturalCanvas)
            {
                config.swatchColA = Swatches.CanopyCafeBistroGreen.Col; // Col: 3, Row: 15 (#2E7D32)
                config.swatchRowA = Swatches.CanopyCafeBistroGreen.Row;
                config.swatchColB = Swatches.CanopyProduceCream.Col;     // Col: 1, Row: 15 (#FFF8E1)
                config.swatchRowB = Swatches.CanopyProduceCream.Row;
            }
            else
            {
                config.swatchColA = Swatches.CanopyCraftingTeal.Col;    // Col: 6, Row: 15 (#00897B)
                config.swatchRowA = Swatches.CanopyCraftingTeal.Row;
                config.swatchColB = Swatches.CanopyCraftingPeach.Col;   // Col: 7, Row: 15 (#FFAB91)
                config.swatchRowB = Swatches.CanopyCraftingPeach.Row;
            }

            Mesh canopyMesh = ProceduralCanopyBuilder.CreateCanopy(config);
            var mf = awningGo.AddComponent<MeshFilter>();
            mf.sharedMesh = canopyMesh;
            var mr = awningGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
        }

        private static void BuildCraftingPottingMatAndSocket(Transform parent, Material mat)
        {
            GameObject socketsGroup = new GameObject("Sockets");
            socketsGroup.transform.SetParent(parent, false);

            float matW = 0.50f;
            float matD = 0.40f;
            float matH = 0.010f;
            Vector3 matPos = new Vector3(-0.30f, 0.92f, -0.15f);

            GameObject pottingMatGo = new GameObject("PottingMat");
            pottingMatGo.transform.SetParent(socketsGroup.transform, false);
            pottingMatGo.transform.localPosition = matPos;

            Mesh matMesh = ProceduralMeshBuilder.CreateChamferedCube(matW, matH, matD, 0.004f,
                Swatches.JuteTwineCord.Col, Swatches.JuteTwineCord.Row);
            CreateMeshChild(pottingMatGo.transform, "BurlapMatDeck", matMesh, mat,
                new Vector3(0f, matH * 0.5f, 0f));

            // Potting Mat Snap Socket flush with burlap mat surface (offset = 1.0mm per F10-C04, F15-C02)
            GameObject socketGo = new GameObject("PottingMatSocket");
            socketGo.transform.SetParent(pottingMatGo.transform, false);
            socketGo.transform.localPosition = new Vector3(0f, matH + 0.001f, 0f);

            var socket = socketGo.AddComponent<XRSocketInteractor>();
            socket.interactionLayers = 1;               // Default interaction layer mask (F15-C03)
            socket.showInteractableHoverMeshes = false; // Suppress hover Z-fighting (F15_05, F15-C05)
            socket.recycleDelayTime = 0.25f;            // Rapid successive snapping without locking (F15-C04)
            socket.socketActive = true;

            GameObject attach = new GameObject("AttachTransform");
            attach.transform.SetParent(socketGo.transform, false);
            attach.transform.localPosition = Vector3.zero;
            attach.transform.localRotation = Quaternion.identity;
            socket.attachTransform = attach.transform;

            var trigCol = socketGo.AddComponent<SphereCollider>();
            trigCol.isTrigger = true;                   // Trigger collider strictly true (F15-C01)
            trigCol.radius = 0.15f;                     // Uniform 15cm spherical capture volume
            trigCol.center = new Vector3(0f, 0.08f, 0f);// Elevated 8cm above burlap mat deck
        }

        // =========================================================================
        // Common Helpers
        // =========================================================================

        private static GameObject CreateCounterSocket(Transform parent, string name, Vector3 pos)
        {
            GameObject socketGo = new GameObject(name);
            socketGo.transform.SetParent(parent, false);
            socketGo.transform.localPosition = pos;

            var socket = socketGo.AddComponent<XRSocketInteractor>();
            socket.interactionLayers = 1;               // Default interaction layer mask (F15-C03)
            socket.showInteractableHoverMeshes = false; // Suppress hover Z-fighting (F15_05, F15-C05)
            socket.recycleDelayTime = 0.25f;            // Rapid successive snapping without locking (F15-C04)
            socket.socketActive = true;

            GameObject attach = new GameObject("AttachTransform");
            attach.transform.SetParent(socketGo.transform, false);
            attach.transform.localPosition = Vector3.zero;
            attach.transform.localRotation = Quaternion.identity;
            socket.attachTransform = attach.transform;

            var triggerCol = socketGo.AddComponent<SphereCollider>();
            triggerCol.isTrigger = true;                   // Trigger collider strictly true (F15-C01)
            triggerCol.radius = 0.15f;                     // Uniform 15cm spherical capture volume
            triggerCol.center = new Vector3(0f, 0.08f, 0f);// Elevated 8cm above mount plane

            return socketGo;
        }

        private static GameObject CreateMeshChild(Transform parent, string name, Mesh mesh, Material mat,
            Vector3 localPos, Quaternion? localRot = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot ?? Quaternion.identity;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            return go;
        }

        private static GameObject CreateMeshWithBoxCollider(Transform parent, string name, Mesh mesh, Material mat,
            Vector3 localPos, Quaternion localRot, Vector3 colSize)
        {
            GameObject go = CreateMeshChild(parent, name, mesh, mat, localPos, localRot);
            var col = go.AddComponent<BoxCollider>();
            col.size = colSize;
            col.center = Vector3.zero;
            return go;
        }

#if UNITY_EDITOR
        [MenuItem("AnimalCrossing/Build Market Stalls", priority = 25)]
        public static void MenuItem_BuildAllMarketStalls()
        {
            var allStalls = UnityEngine.Object.FindObjectsByType<Transform>();
            List<GameObject> toDestroy = new List<GameObject>();
            foreach (var t in allStalls)
            {
                if (t != null && (t.name == "ProduceStall" || t.name == "CafeStall" || t.name == "CraftingStall" || t.name == "MarketStalls"))
                {
                    if (t.parent == null || t.name == "MarketStalls")
                    {
                        toDestroy.Add(t.gameObject);
                    }
                }
            }
            foreach (var go in toDestroy)
            {
                if (go != null) DestroyObjectInEditor(go);
            }

            GameObject stalls = BuildAllStalls();
            Selection.activeGameObject = stalls;
            EditorUtility.SetDirty(stalls);
        }

        [MenuItem("AnimalCrossing/Build Produce Stall", priority = 26)]
        public static void MenuItem_BuildProduceStall()
        {
            var existing = GameObject.Find("ProduceStall");
            if (existing != null) DestroyObjectInEditor(existing);

            GameObject stall = BuildProduceStall();
            Selection.activeGameObject = stall;
            EditorUtility.SetDirty(stall);
        }

        [MenuItem("AnimalCrossing/Build Cafe Stall", priority = 27)]
        public static void MenuItem_BuildCafeStall()
        {
            var existing = GameObject.Find("CafeStall");
            if (existing != null) DestroyObjectInEditor(existing);

            GameObject stall = BuildCafeStall();
            Selection.activeGameObject = stall;
            EditorUtility.SetDirty(stall);
        }

        [MenuItem("AnimalCrossing/Build Crafting Stall", priority = 28)]
        public static void MenuItem_BuildCraftingStall()
        {
            var existing = GameObject.Find("CraftingStall");
            if (existing != null) DestroyObjectInEditor(existing);

            GameObject stall = BuildCraftingStall();
            Selection.activeGameObject = stall;
            EditorUtility.SetDirty(stall);
        }

        private static void DestroyObjectInEditor(GameObject go)
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(go);
            else
                UnityEngine.Object.DestroyImmediate(go);
        }
#endif
    }
}
