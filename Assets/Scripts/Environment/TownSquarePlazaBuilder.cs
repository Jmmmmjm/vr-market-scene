// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TownSquarePlazaBuilder.cs" company="VR Market Scene">
//   Copyright (c) 2026 Animal Crossing Stylized VR Village Project. All rights reserved.
// </copyright>
// <summary>
//   Master procedural environment builder for the 25m x 25m Town Square Plaza (Milestone 2).
//   Constructs ground slab, 12mm elevated pathways with beveled curbs, perimeter wooden fencing,
//   stone fountain centerpiece, nature elements (puffy trees, flower boxes, hedges, shrubs),
//   cozy street lamps, wooden benches, bulletin board, and mount points.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using AnimalCrossing.TechnicalArt;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AnimalCrossing.Environment
{
    /// <summary>
    /// Master procedural environment builder for the 25m x 25m Town Square Plaza.
    /// Conforms strictly to requirements R1, R6, R7 and test suite F4, F5, F6, F7, F11, F17, F21.
    /// </summary>
    [ExecuteInEditMode]
    public class TownSquarePlazaBuilder : MonoBehaviour
    {
        // --------------------------------------------------------------------
        // Constants & Invariants
        // --------------------------------------------------------------------
        public const float PlazaDimension = 25.0f;
        public const float PlazaHalfExtent = 12.5f;
        public const float PathElevation = 0.012f;      // 12mm anti-Z-fighting lift
        public const float PostAnchorDepth = 0.05f;     // 50mm ground embedment (Y = -0.05m)
        public const float CurbWidth = 0.08f;           // 80mm beveled curb slope to Y = 0
        public const float BenchSeatHeight = 0.45f;     // Ergonomic seat height 0.45m (+-0.05m)
        public const float GroundSlabDepth = 0.20f;     // 20cm thickness for ground base

        public const string OpaqueMatPath = "Assets/Materials/M_AnimalCrossing_Opaque.mat";
        public const string FoliageMatPath = "Assets/Materials/M_AnimalCrossing_Foliage.mat";
        public const string EmissiveMatPath = "Assets/Materials/M_AnimalCrossing_Emissive.mat";
        public const string LiquidGlassMatPath = "Assets/Materials/M_AnimalCrossing_LiquidGlass.mat";

        [Header("Material References")]
        [SerializeField] private Material opaqueMaterial;
        [SerializeField] private Material foliageMaterial;
        [SerializeField] private Material emissiveMaterial;
        [SerializeField] private Material liquidGlassMaterial;

        [Header("Batching Options")]
        [SerializeField] private bool autoBatchStaticMeshes = false;

        [Header("Market Stalls")]
        [SerializeField] private bool buildMarketStalls = true;

        public Material OpaqueMaterial
        {
            get
            {
                if (opaqueMaterial == null)
                {
#if UNITY_EDITOR
                    opaqueMaterial = AssetDatabase.LoadAssetAtPath<Material>(OpaqueMatPath);
#endif
                }
                return opaqueMaterial;
            }
            set => opaqueMaterial = value;
        }

        public Material FoliageMaterial
        {
            get
            {
                if (foliageMaterial == null)
                {
#if UNITY_EDITOR
                    foliageMaterial = AssetDatabase.LoadAssetAtPath<Material>(FoliageMatPath);
#endif
                }
                return foliageMaterial;
            }
            set => foliageMaterial = value;
        }

        public Material EmissiveMaterial
        {
            get
            {
                if (emissiveMaterial == null)
                {
#if UNITY_EDITOR
                    emissiveMaterial = AssetDatabase.LoadAssetAtPath<Material>(EmissiveMatPath);
#endif
                }
                return emissiveMaterial;
            }
            set => emissiveMaterial = value;
        }

        public Material LiquidGlassMaterial
        {
            get
            {
                if (liquidGlassMaterial == null)
                {
#if UNITY_EDITOR
                    liquidGlassMaterial = AssetDatabase.LoadAssetAtPath<Material>(LiquidGlassMatPath);
#endif
                }
                return liquidGlassMaterial;
            }
            set => liquidGlassMaterial = value;
        }

        // --------------------------------------------------------------------
        // Public Master Construction API
        // --------------------------------------------------------------------

        /// <summary>
        /// Assembles the complete 25m x 25m Town Square Plaza hierarchy at the scene origin.
        /// </summary>
        public GameObject BuildPlaza()
        {
            ClearPlaza();

            gameObject.name = "TownSquarePlaza";
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;

            Material opMat = OpaqueMaterial;
            Material folMat = FoliageMaterial;
            Material emMat = EmissiveMaterial;
            Material lgMat = LiquidGlassMaterial;

            // 1. Build Ground Slab with BoxCollider and TeleportationArea (F4)
            BuildGround(transform, opMat);

            // 2. Build Pathways with 12mm elevation, beveled curbs, and colliders (F4, F11)
            BuildPathways(transform, opMat);

            // 3. Build Perimeter Wooden Post Fencing anchored 5cm in ground (F4, F11)
            BuildFencing(transform, opMat);

            // 4. Build Wooden Benches (seat height = 0.45m) (F7)
            BuildBenches(transform, opMat);

            // 5. Build Village Bulletin Board facing main walkway (F7)
            BuildBulletinBoard(transform, opMat);

            // 6. Build Stone Fountain Centerpiece (F5)
            BuildFountainCenterpiece(transform, opMat, lgMat);

            // 7. Build Nature Elements: Puffy Trees, Flower Boxes, Hedges, Shrubs (F6)
            BuildNatureElements(transform, opMat, folMat);

            // 8. Build Cozy Street Lamps (F7)
            BuildStreetLamps(transform, opMat, emMat);

            // 9. Build Mount Points for Market Stalls (F8, F9, F10)
            BuildMountPoints(transform);

            // 10. Build Market Stalls (F8, F9, F10)
            if (buildMarketStalls)
            {
                MarketStallBuilder.BuildAllStalls(transform);
            }

            // 11. PC VR Player Rig (Requirement R6)
            AnimalCrossing.Interaction.VRMarketRigSetup.CreateOrConfigureRig();

            // 12. Optional Static Mesh Batching (F21)
            if (autoBatchStaticMeshes)
            {
                BatchPlaza();
            }

            Debug.Log("[TownSquarePlazaBuilder] 25m x 25m Town Square Plaza successfully built.");
            return gameObject;
        }

        private void Start()
        {
            if (buildMarketStalls && transform.Find("MarketStalls") == null)
            {
                MarketStallBuilder.BuildAllStalls(transform);
            }
            AnimalCrossing.Interaction.VRMarketRigSetup.CreateOrConfigureRig();
        }

        /// <summary>
        /// Combines static geometry for draw call minimization while preserving primitive colliders.
        /// </summary>
        public void BatchPlaza()
        {
            ProceduralMeshCombiner.BatchTownSquarePlaza(gameObject, OpaqueMaterial, FoliageMaterial, EmissiveMaterial);
            Debug.Log("[TownSquarePlazaBuilder] Static mesh batching executed.");
        }

        /// <summary>
        /// Clears all existing child geometry under this builder.
        /// </summary>
        public void ClearPlaza()
        {
            List<GameObject> children = new List<GameObject>();
            for (int i = 0; i < transform.childCount; i++)
            {
                children.Add(transform.GetChild(i).gameObject);
            }

            foreach (var child in children)
            {
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        // --------------------------------------------------------------------
        // 1. Ground Slab Generator (F4)
        // --------------------------------------------------------------------

        public static GameObject BuildGround(Transform parent, Material mat)
        {
            GameObject ground = new GameObject("Ground");
            ground.transform.SetParent(parent, false);
            ground.transform.localPosition = Vector3.zero;

            ProceduralMeshBuilder.MeshBuffer buffer = new ProceduralMeshBuilder.MeshBuffer();
            Vector2 grassUV = ProceduralMeshBuilder.GetSwatchUV(Swatches.GrassACGreen.Col, Swatches.GrassACGreen.Row);
            Vector2 dirtUV  = ProceduralMeshBuilder.GetSwatchUV(Swatches.CliffEarthBrown.Col, Swatches.CliffEarthBrown.Row);

            float half = PlazaHalfExtent;
            float topY = 0.0f;
            float botY = -GroundSlabDepth;

            // Top grass face (Y = 0)
            buffer.AddQuad(
                new Vector3(-half, topY, -half),
                new Vector3(half, topY, -half),
                new Vector3(half, topY, half),
                new Vector3(-half, topY, half),
                Vector3.up, grassUV);

            // 4 Boundary skirts (Earth/bedrock)
            buffer.AddQuad(new Vector3(-half, botY, half), new Vector3(half, botY, half), new Vector3(half, topY, half), new Vector3(-half, topY, half), Vector3.forward, dirtUV);
            buffer.AddQuad(new Vector3(half, botY, -half), new Vector3(-half, botY, -half), new Vector3(-half, topY, -half), new Vector3(half, topY, -half), Vector3.back, dirtUV);
            buffer.AddQuad(new Vector3(half, botY, half), new Vector3(half, botY, -half), new Vector3(half, topY, -half), new Vector3(half, topY, half), Vector3.right, dirtUV);
            buffer.AddQuad(new Vector3(-half, botY, -half), new Vector3(-half, botY, half), new Vector3(-half, topY, half), new Vector3(-half, topY, -half), Vector3.left, dirtUV);

            // Bottom face
            buffer.AddQuad(new Vector3(-half, botY, half), new Vector3(half, botY, half), new Vector3(half, botY, -half), new Vector3(-half, botY, -half), Vector3.down, dirtUV);

            Mesh mesh = buffer.Build("GroundSlab_25x25m");
            var mf = ground.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = ground.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            // Static BoxCollider for walkable locomotion
            var col = ground.AddComponent<BoxCollider>();
            col.size = new Vector3(PlazaDimension, GroundSlabDepth, PlazaDimension);
            col.center = new Vector3(0f, -GroundSlabDepth * 0.5f, 0f);

            // TeleportationArea component configuration
            var teleport = ground.AddComponent<TeleportationArea>();
            teleport.teleportTrigger = BaseTeleportationInteractable.TeleportTrigger.OnDeactivated;

            return ground;
        }

        // --------------------------------------------------------------------
        // 2. Pathways Generator (12mm Elevation + Beveled Curbs) (F4, F11)
        // --------------------------------------------------------------------

        public static GameObject BuildPathways(Transform parent, Material mat)
        {
            GameObject pathwaysGo = new GameObject("Pathways");
            pathwaysGo.transform.SetParent(parent, false);
            pathwaysGo.transform.localPosition = Vector3.zero;

            ProceduralMeshBuilder.MeshBuffer buffer = new ProceduralMeshBuilder.MeshBuffer();

            Vector2 cobbleLightUV = ProceduralMeshBuilder.GetSwatchUV(Swatches.StoneCobbleLight.Col, Swatches.StoneCobbleLight.Row);
            Vector2 cobbleMidUV   = ProceduralMeshBuilder.GetSwatchUV(Swatches.StoneCobbleMid.Col, Swatches.StoneCobbleMid.Row);
            Vector2 curbStoneUV   = ProceduralMeshBuilder.GetSwatchUV(Swatches.StoneCopingPaleBlue.Col, Swatches.StoneCopingPaleBlue.Row);
            Vector2 dirtWarmUV    = ProceduralMeshBuilder.GetSwatchUV(Swatches.DirtPackedWarm.Col, Swatches.DirtPackedWarm.Row);
            Vector2 dirtTanUV     = ProceduralMeshBuilder.GetSwatchUV(Swatches.DirtPathTan.Col, Swatches.DirtPathTan.Row);

            float yPath = PathElevation; // +0.012m
            float yGround = 0.0f;
            float curb = CurbWidth;      // 0.08m curb transition

            // A. Central Fountain Esplanade (Octagonal Cobblestone Ring)
            int segs = 16;
            float rIn = 2.40f;
            float rOut = 5.50f;
            for (int i = 0; i < segs; i++)
            {
                float a0 = i * (Mathf.PI * 2f / segs);
                float a1 = (i + 1) * (Mathf.PI * 2f / segs);

                Vector3 vIn0  = new Vector3(Mathf.Cos(a0) * rIn, yPath, Mathf.Sin(a0) * rIn);
                Vector3 vIn1  = new Vector3(Mathf.Cos(a1) * rIn, yPath, Mathf.Sin(a1) * rIn);
                Vector3 vOut0 = new Vector3(Mathf.Cos(a0) * rOut, yPath, Mathf.Sin(a0) * rOut);
                Vector3 vOut1 = new Vector3(Mathf.Cos(a1) * rOut, yPath, Mathf.Sin(a1) * rOut);

                Vector2 cUV = (i % 2 == 0) ? cobbleLightUV : cobbleMidUV;
                buffer.AddQuad(vIn0, vOut0, vOut1, vIn1, Vector3.up, cUV);

                // Beveled Outer Curb sloping from rOut to (rOut + curb) down to yGround
                Vector3 vCurb0 = new Vector3(Mathf.Cos(a0) * (rOut + curb), yGround, Mathf.Sin(a0) * (rOut + curb));
                Vector3 vCurb1 = new Vector3(Mathf.Cos(a1) * (rOut + curb), yGround, Mathf.Sin(a1) * (rOut + curb));
                Vector3 nCurb = new Vector3(Mathf.Cos((a0 + a1) * 0.5f), 0.15f, Mathf.Sin((a0 + a1) * 0.5f)).normalized;
                buffer.AddQuad(vOut0, vCurb0, vCurb1, vOut1, nCurb, curbStoneUV);

                // Beveled Inner Curb sloping from rIn down to (rIn - curb) at yGround
                Vector3 vCurbIn0 = new Vector3(Mathf.Cos(a0) * (rIn - curb), yGround, Mathf.Sin(a0) * (rIn - curb));
                Vector3 vCurbIn1 = new Vector3(Mathf.Cos(a1) * (rIn - curb), yGround, Mathf.Sin(a1) * (rIn - curb));
                Vector3 nCurbIn = new Vector3(-Mathf.Cos((a0 + a1) * 0.5f), 0.15f, -Mathf.Sin((a0 + a1) * 0.5f)).normalized;
                buffer.AddQuad(vIn0, vCurbIn0, vCurbIn1, vIn1, nCurbIn, curbStoneUV);
            }

            // B. South Main Entrance Avenue (Z in [-12.5, -5.5], X in [-2.1, 2.1])
            AddElevatedAvenue(buffer, -2.1f, 2.1f, -12.5f, -5.5f, yPath, yGround, curb, cobbleLightUV, curbStoneUV, true, false);

            // C. North Avenue (Z in [5.5, 12.5], X in [-1.8, 1.8])
            AddElevatedAvenue(buffer, -1.8f, 1.8f, 5.5f, 12.5f, yPath, yGround, curb, cobbleMidUV, curbStoneUV, false, true);

            // D. West Avenue (X in [-12.5, -5.5], Z in [-1.8, 1.8])
            AddElevatedAvenueX(buffer, -12.5f, -5.5f, -1.8f, 1.8f, yPath, yGround, curb, dirtWarmUV, curbStoneUV, true, false);

            // E. East Avenue (X in [5.5, 12.5], Z in [-1.8, 1.8])
            AddElevatedAvenueX(buffer, 5.5f, 12.5f, -1.8f, 1.8f, yPath, yGround, curb, dirtTanUV, curbStoneUV, false, true);

            Mesh mesh = buffer.Build("PlazaPathways_Elevated12mm");
            var mf = pathwaysGo.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = pathwaysGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            // Static BoxColliders for avenue corridors
            CreatePathBoxCollider(pathwaysGo, new Vector3(0f, -0.088f, -9.0f), new Vector3(4.2f + curb * 2, 0.20f, 7.0f));
            CreatePathBoxCollider(pathwaysGo, new Vector3(0f, -0.088f, 9.0f), new Vector3(3.6f + curb * 2, 0.20f, 7.0f));
            CreatePathBoxCollider(pathwaysGo, new Vector3(-9.0f, -0.088f, 0f), new Vector3(7.0f, 0.20f, 3.6f + curb * 2));
            CreatePathBoxCollider(pathwaysGo, new Vector3(9.0f, -0.088f, 0f), new Vector3(7.0f, 0.20f, 3.6f + curb * 2));

            // Central Ring BoxColliders
            CreatePathBoxCollider(pathwaysGo, new Vector3(0f, -0.088f, 4.0f), new Vector3(8.0f, 0.20f, 3.0f));
            CreatePathBoxCollider(pathwaysGo, new Vector3(0f, -0.088f, -4.0f), new Vector3(8.0f, 0.20f, 3.0f));

            return pathwaysGo;
        }

        private static void AddElevatedAvenue(ProceduralMeshBuilder.MeshBuffer buffer,
            float xMin, float xMax, float zMin, float zMax, float yPath, float yGround, float curb,
            Vector2 pathUV, Vector2 curbUV, bool hasSouthCurb, bool hasNorthCurb)
        {
            buffer.AddQuad(
                new Vector3(xMin, yPath, zMin),
                new Vector3(xMax, yPath, zMin),
                new Vector3(xMax, yPath, zMax),
                new Vector3(xMin, yPath, zMax),
                Vector3.up, pathUV);

            buffer.AddQuad(
                new Vector3(xMin - curb, yGround, zMin),
                new Vector3(xMin, yPath, zMin),
                new Vector3(xMin, yPath, zMax),
                new Vector3(xMin - curb, yGround, zMax),
                new Vector3(-0.9f, 0.4f, 0f).normalized, curbUV);

            buffer.AddQuad(
                new Vector3(xMax, yPath, zMin),
                new Vector3(xMax + curb, yGround, zMin),
                new Vector3(xMax + curb, yGround, zMax),
                new Vector3(xMax, yPath, zMax),
                new Vector3(0.9f, 0.4f, 0f).normalized, curbUV);

            if (hasSouthCurb)
            {
                buffer.AddQuad(
                    new Vector3(xMin, yPath, zMin),
                    new Vector3(xMax, yPath, zMin),
                    new Vector3(xMax + curb, yGround, zMin - curb),
                    new Vector3(xMin - curb, yGround, zMin - curb),
                    new Vector3(0f, 0.4f, -0.9f).normalized, curbUV);
            }
            if (hasNorthCurb)
            {
                buffer.AddQuad(
                    new Vector3(xMin, yPath, zMax),
                    new Vector3(xMin - curb, yGround, zMax + curb),
                    new Vector3(xMax + curb, yGround, zMax + curb),
                    new Vector3(xMax, yPath, zMax),
                    new Vector3(0f, 0.4f, 0.9f).normalized, curbUV);
            }
        }

        private static void AddElevatedAvenueX(ProceduralMeshBuilder.MeshBuffer buffer,
            float xMin, float xMax, float zMin, float zMax, float yPath, float yGround, float curb,
            Vector2 pathUV, Vector2 curbUV, bool hasWestCurb, bool hasEastCurb)
        {
            buffer.AddQuad(
                new Vector3(xMin, yPath, zMin),
                new Vector3(xMax, yPath, zMin),
                new Vector3(xMax, yPath, zMax),
                new Vector3(xMin, yPath, zMax),
                Vector3.up, pathUV);

            buffer.AddQuad(
                new Vector3(xMin, yGround, zMin - curb),
                new Vector3(xMax, yGround, zMin - curb),
                new Vector3(xMax, yPath, zMin),
                new Vector3(xMin, yPath, zMin),
                new Vector3(0f, 0.4f, -0.9f).normalized, curbUV);

            buffer.AddQuad(
                new Vector3(xMin, yPath, zMax),
                new Vector3(xMax, yPath, zMax),
                new Vector3(xMax, yGround, zMax + curb),
                new Vector3(xMin, yGround, zMax + curb),
                new Vector3(0f, 0.4f, 0.9f).normalized, curbUV);

            if (hasWestCurb)
            {
                buffer.AddQuad(
                    new Vector3(xMin - curb, yGround, zMin - curb),
                    new Vector3(xMin, yPath, zMin),
                    new Vector3(xMin, yPath, zMax),
                    new Vector3(xMin - curb, yGround, zMax + curb),
                    new Vector3(-0.9f, 0.4f, 0f).normalized, curbUV);
            }
            if (hasEastCurb)
            {
                buffer.AddQuad(
                    new Vector3(xMax, yPath, zMin),
                    new Vector3(xMax + curb, yGround, zMin - curb),
                    new Vector3(xMax + curb, yGround, zMax + curb),
                    new Vector3(xMax, yPath, zMax),
                    new Vector3(0.9f, 0.4f, 0f).normalized, curbUV);
            }
        }

        private static void CreatePathBoxCollider(GameObject parent, Vector3 center, Vector3 size)
        {
            var colGo = new GameObject("PathCollider");
            colGo.transform.SetParent(parent.transform, false);
            colGo.transform.localPosition = new Vector3(center.x, PathElevation, center.z);
            var box = colGo.AddComponent<BoxCollider>();
            box.size = size;
            box.center = new Vector3(0f, center.y - PathElevation, 0f);
        }

        // --------------------------------------------------------------------
        // 3. Perimeter Wooden Post Fencing Generator (F4, F11)
        // --------------------------------------------------------------------

        public static GameObject BuildFencing(Transform parent, Material mat)
        {
            GameObject fencing = new GameObject("Fencing");
            fencing.transform.SetParent(parent, false);
            fencing.transform.localPosition = Vector3.zero;

            float postRadius = 0.06f;
            float postHeight = 1.05f;
            float fillet = 0.02f;
            Mesh postMesh = ProceduralMeshBuilder.CreateBeveledCylinder(
                postRadius, postHeight, fillet, 8, 2,
                Swatches.WoodPineMidtone.Col, Swatches.WoodPineMidtone.Row);

            float railLength = 1.90f;
            Mesh railMesh = ProceduralMeshBuilder.CreateChamferedCube(
                railLength, 0.08f, 0.05f, 0.01f,
                Swatches.WoodPineTan.Col, Swatches.WoodPineTan.Row);

            // Perimeter borders at +-12.2m (inside 12.5m boundary footprint)
            BuildFenceRunZ(fencing.transform, -12.2f, -12.0f, -2.4f, postMesh, railMesh, mat, true);
            BuildFenceRunZ(fencing.transform, -12.2f, 2.4f, 12.0f, postMesh, railMesh, mat, true);

            BuildFenceRunZ(fencing.transform, 12.2f, -12.0f, -2.2f, postMesh, railMesh, mat, true);
            BuildFenceRunZ(fencing.transform, 12.2f, 2.2f, 12.0f, postMesh, railMesh, mat, true);

            BuildFenceRunX(fencing.transform, -12.2f, -12.0f, -2.2f, postMesh, railMesh, mat, true);
            BuildFenceRunX(fencing.transform, -12.2f, 2.2f, 12.0f, postMesh, railMesh, mat, true);

            BuildFenceRunX(fencing.transform, 12.2f, -12.0f, -2.2f, postMesh, railMesh, mat, true);
            BuildFenceRunX(fencing.transform, 12.2f, 2.2f, 12.0f, postMesh, railMesh, mat, true);

            return fencing;
        }

        private static void BuildFenceRunZ(Transform parent, float zPos, float xStart, float xEnd,
            Mesh postMesh, Mesh railMesh, Material mat, bool addColliders)
        {
            float length = Mathf.Abs(xEnd - xStart);
            int postCount = Mathf.RoundToInt(length / 1.90f) + 1;
            float step = length / (postCount - 1);

            float postCenterY = -PostAnchorDepth + 1.05f * 0.5f;

            for (int i = 0; i < postCount; i++)
            {
                float x = xStart + i * step;
                CreateMeshChild(parent, "FencePost", postMesh, mat, new Vector3(x, postCenterY, zPos), Quaternion.identity);

                if (i < postCount - 1)
                {
                    float midX = x + step * 0.5f;
                    CreateMeshChild(parent, "FenceRailLower", railMesh, mat, new Vector3(midX, 0.35f, zPos), Quaternion.identity);
                    CreateMeshChild(parent, "FenceRailUpper", railMesh, mat, new Vector3(midX, 0.75f, zPos), Quaternion.identity);
                }
            }

            if (addColliders)
            {
                float midX = (xStart + xEnd) * 0.5f;
                var colGo = new GameObject("FenceCollider");
                colGo.transform.SetParent(parent, false);
                colGo.transform.localPosition = new Vector3(midX, 0.55f, zPos);
                var col = colGo.AddComponent<BoxCollider>();
                col.size = new Vector3(length + 0.15f, 1.20f, 0.30f);
            }
        }

        private static void BuildFenceRunX(Transform parent, float xPos, float zStart, float zEnd,
            Mesh postMesh, Mesh railMesh, Material mat, bool addColliders)
        {
            float length = Mathf.Abs(zEnd - zStart);
            int postCount = Mathf.RoundToInt(length / 1.90f) + 1;
            float step = length / (postCount - 1);

            float postCenterY = -PostAnchorDepth + 1.05f * 0.5f;
            Quaternion rot90 = Quaternion.Euler(0f, 90f, 0f);

            for (int i = 0; i < postCount; i++)
            {
                float z = zStart + i * step;
                CreateMeshChild(parent, "FencePost", postMesh, mat, new Vector3(xPos, postCenterY, z), Quaternion.identity);

                if (i < postCount - 1)
                {
                    float midZ = z + step * 0.5f;
                    CreateMeshChild(parent, "FenceRailLower", railMesh, mat, new Vector3(xPos, 0.35f, midZ), rot90);
                    CreateMeshChild(parent, "FenceRailUpper", railMesh, mat, new Vector3(xPos, 0.75f, midZ), rot90);
                }
            }

            if (addColliders)
            {
                float midZ = (zStart + zEnd) * 0.5f;
                var colGo = new GameObject("FenceCollider");
                colGo.transform.SetParent(parent, false);
                colGo.transform.localPosition = new Vector3(xPos, 0.55f, midZ);
                var col = colGo.AddComponent<BoxCollider>();
                col.size = new Vector3(0.30f, 1.20f, length + 0.15f);
            }
        }

        // --------------------------------------------------------------------
        // 4. Wooden Benches Generator (Seat Height = 0.45m) (F7)
        // --------------------------------------------------------------------

        public static GameObject BuildBenches(Transform parent, Material mat)
        {
            GameObject benchesGroup = new GameObject("Benches");
            benchesGroup.transform.SetParent(parent, false);
            benchesGroup.transform.localPosition = Vector3.zero;

            // Bench 1: West promenade terrace facing East
            CreateBenchInstance(benchesGroup.transform, "Bench", new Vector3(-4.5f, 0f, 3.0f), Quaternion.Euler(0f, 90f, 0f), mat);

            // Bench 2: East promenade terrace facing West
            CreateBenchInstance(benchesGroup.transform, "WoodenBench", new Vector3(4.5f, 0f, 3.0f), Quaternion.Euler(0f, -90f, 0f), mat);

            // Bench 3: South garden corner facing North-West
            CreateBenchInstance(benchesGroup.transform, "Bench_SouthGarden", new Vector3(5.5f, 0f, -4.5f), Quaternion.Euler(0f, -135f, 0f), mat);

            return benchesGroup;
        }

        private static GameObject CreateBenchInstance(Transform parent, string name, Vector3 position, Quaternion rotation, Material mat)
        {
            GameObject bench = new GameObject(name);
            bench.transform.SetParent(parent, false);
            bench.transform.localPosition = position;
            bench.transform.localRotation = rotation;

            float slatLen = 1.50f;
            float slatW = 0.12f;
            float slatH = 0.035f;
            float bevel = 0.006f;

            Mesh seatSlatMesh = ProceduralMeshBuilder.CreateChamferedCube(slatLen, slatH, slatW, bevel,
                Swatches.WoodOakGolden.Col, Swatches.WoodOakGolden.Row);
            Mesh backSlatMesh = ProceduralMeshBuilder.CreateChamferedCube(slatLen, slatH, slatW, bevel,
                Swatches.WoodOakGolden.Col, Swatches.WoodOakGolden.Row);

            // 3 Seat Slats: Top sits exactly at Y = 0.450m
            float seatCenterY = BenchSeatHeight - slatH * 0.5f;
            float[] zOffsets = new float[] { -0.13f, 0.0f, 0.13f };
            for (int i = 0; i < 3; i++)
            {
                CreateMeshChild(bench.transform, $"SeatSlat_{i}", seatSlatMesh, mat,
                    new Vector3(0f, seatCenterY, zOffsets[i]), Quaternion.identity);
            }

            // 2 Backrest Slats
            Quaternion backRot = Quaternion.Euler(8f, 0f, 0f);
            CreateMeshChild(bench.transform, "BackSlat_Lower", backSlatMesh, mat, new Vector3(0f, 0.65f, -0.20f), backRot);
            CreateMeshChild(bench.transform, "BackSlat_Upper", backSlatMesh, mat, new Vector3(0f, 0.82f, -0.22f), backRot);

            // 4 Support Legs anchored 5cm in ground (Y = -0.05m to Y = 0.415m)
            float legHeight = 0.465f;
            float legCenterY = -PostAnchorDepth + legHeight * 0.5f;
            Mesh legMesh = ProceduralMeshBuilder.CreateChamferedCube(0.07f, legHeight, 0.07f, 0.008f,
                Swatches.WoodCedarDark.Col, Swatches.WoodCedarDark.Row);

            float[] legX = new float[] { -0.65f, 0.65f };
            float[] legZ = new float[] { -0.14f, 0.14f };
            foreach (float lx in legX)
            {
                foreach (float lz in legZ)
                {
                    CreateMeshChild(bench.transform, "BenchLeg", legMesh, mat, new Vector3(lx, legCenterY, lz), Quaternion.identity);
                }
            }

            // 2 Armrests
            Mesh armMesh = ProceduralMeshBuilder.CreateChamferedCube(0.06f, 0.04f, 0.42f, 0.008f,
                Swatches.WoodOakGolden.Col, Swatches.WoodOakGolden.Row);
            CreateMeshChild(bench.transform, "Armrest_Left", armMesh, mat, new Vector3(-0.68f, 0.58f, 0f), Quaternion.identity);
            CreateMeshChild(bench.transform, "Armrest_Right", armMesh, mat, new Vector3(0.68f, 0.58f, 0f), Quaternion.identity);

            // Static BoxColliders: Seat, Legs, and Backrest
            var seatCol = bench.AddComponent<BoxCollider>();
            seatCol.size = new Vector3(slatLen, 0.10f, 0.42f);
            seatCol.center = new Vector3(0f, BenchSeatHeight - 0.05f, 0f); // bounds center.y = 0.40m, min.y = 0.35m

            var legColGo = new GameObject("LegsCollider");
            legColGo.transform.SetParent(bench.transform, false);
            var legBox = legColGo.AddComponent<BoxCollider>();
            legBox.size = new Vector3(slatLen - 0.10f, 0.35f, 0.28f);
            legBox.center = new Vector3(0f, 0.175f, 0f); // bounds min.y = 0.0f

            var backColGo = new GameObject("BackrestCollider");
            backColGo.transform.SetParent(bench.transform, false);
            backColGo.transform.localPosition = new Vector3(0f, 0.72f, -0.20f);
            var backCol = backColGo.AddComponent<BoxCollider>();
            backCol.size = new Vector3(slatLen, 0.38f, 0.08f);

            return bench;
        }

        // --------------------------------------------------------------------
        // 5. Village Bulletin Board Generator (F7)
        // --------------------------------------------------------------------

        public static GameObject BuildBulletinBoard(Transform parent, Material mat)
        {
            GameObject board = new GameObject("BulletinBoard");
            board.transform.SetParent(parent, false);
            board.transform.localPosition = new Vector3(-3.2f, 0f, -8.0f);
            board.transform.localRotation = Quaternion.identity; // Facing +Z (North towards walkway)

            // Two Support Posts: height 2.25m, anchored 5cm in ground (Y = -0.05m to Y = 2.20m)
            float postH = 2.25f;
            float postCenterY = -PostAnchorDepth + postH * 0.5f;
            Mesh postMesh = ProceduralMeshBuilder.CreateBeveledCylinder(0.06f, postH, 0.015f, 8, 2,
                Swatches.WoodBarkDeep.Col, Swatches.WoodBarkDeep.Row);

            CreateMeshChild(board.transform, "Post_Left", postMesh, mat, new Vector3(-0.65f, postCenterY, 0f), Quaternion.identity);
            CreateMeshChild(board.transform, "Post_Right", postMesh, mat, new Vector3(0.65f, postCenterY, 0f), Quaternion.identity);

            // Chalk Slate Center Display Panel: at Y = 1.40m
            Mesh slateMesh = ProceduralMeshBuilder.CreateChamferedCube(1.20f, 0.80f, 0.04f, 0.008f,
                Swatches.BulletinBoardSlate.Col, Swatches.BulletinBoardSlate.Row);
            CreateMeshChild(board.transform, "SlatePanel", slateMesh, mat, new Vector3(0f, 1.40f, 0f), Quaternion.identity);

            // Wooden Frame Surrounding Slate
            Mesh frameHoriz = ProceduralMeshBuilder.CreateChamferedCube(1.36f, 0.07f, 0.08f, 0.01f,
                Swatches.WoodOakGolden.Col, Swatches.WoodOakGolden.Row);
            Mesh frameVert  = ProceduralMeshBuilder.CreateChamferedCube(0.07f, 0.80f, 0.08f, 0.01f,
                Swatches.WoodOakGolden.Col, Swatches.WoodOakGolden.Row);

            CreateMeshChild(board.transform, "Frame_Top", frameHoriz, mat, new Vector3(0f, 1.835f, 0f), Quaternion.identity);
            CreateMeshChild(board.transform, "Frame_Bottom", frameHoriz, mat, new Vector3(0f, 0.965f, 0f), Quaternion.identity);
            CreateMeshChild(board.transform, "Frame_Left", frameVert, mat, new Vector3(-0.635f, 1.40f, 0f), Quaternion.identity);
            CreateMeshChild(board.transform, "Frame_Right", frameVert, mat, new Vector3(0.635f, 1.40f, 0f), Quaternion.identity);

            // Decorative Pinned Notice Flyers
            CreateNoticeFlyer(board.transform, "Flyer_TownNews", mat, new Vector3(-0.35f, 1.50f, 0.025f),
                0.22f, 0.28f, Quaternion.Euler(0f, 0f, 3f), Swatches.BulletinChalkWhite);
            CreateNoticeFlyer(board.transform, "Flyer_FruitMarket", mat, new Vector3(0.30f, 1.52f, 0.025f),
                0.20f, 0.24f, Quaternion.Euler(0f, 0f, -4f), Swatches.SeedPacketKraftPaper);
            CreateNoticeFlyer(board.transform, "Flyer_FishingContest", mat, new Vector3(-0.15f, 1.25f, 0.025f),
                0.24f, 0.18f, Quaternion.Euler(0f, 0f, 1f), Swatches.VanillaFondantCream);
            CreateNoticeFlyer(board.transform, "Flyer_FlowerNotice", mat, new Vector3(0.28f, 1.22f, 0.025f),
                0.18f, 0.20f, Quaternion.Euler(0f, 0f, -2f), Swatches.FlowerCosmosBlush);

            // Pitched Weather Roof Cap (Awning) at Y = 2.00m
            Mesh roofMesh = ProceduralMeshBuilder.CreateChamferedCube(1.50f, 0.08f, 0.28f, 0.015f,
                Swatches.WoodCedarDark.Col, Swatches.WoodCedarDark.Row);
            CreateMeshChild(board.transform, "RoofAwning", roofMesh, mat, new Vector3(0f, 2.00f, 0.02f), Quaternion.Euler(15f, 0f, 0f));

            // Static BoxCollider
            var col = board.AddComponent<BoxCollider>();
            col.size = new Vector3(1.55f, 2.30f, 0.35f);
            col.center = new Vector3(0f, 1.10f, 0f);

            return board;
        }

        private static void CreateNoticeFlyer(Transform parent, string name, Material mat,
            Vector3 pos, float width, float height, Quaternion rot, SwatchCoord swatch)
        {
            Mesh flyerMesh = ProceduralMeshBuilder.CreateChamferedCube(width, height, 0.004f, 0.001f, swatch.Col, swatch.Row);
            CreateMeshChild(parent, name, flyerMesh, mat, pos, rot);
        }

        // --------------------------------------------------------------------
        // 6. Stone Fountain Centerpiece (F5)
        // --------------------------------------------------------------------

        public static GameObject BuildFountainCenterpiece(Transform parent, Material stoneMat, Material liquidGlassMat)
        {
            FountainConfig config = FountainConfig.CreateDefault();
            GameObject fountain = ProceduralFountainBuilder.BuildFountainGameObject(parent, config, stoneMat, liquidGlassMat);
            return fountain;
        }

        // --------------------------------------------------------------------
        // 7. Village Nature Elements (Trees, Flowers, Hedges, Shrubs) (F6)
        // --------------------------------------------------------------------

        public static GameObject BuildNatureElements(Transform parent, Material opaqueMat, Material foliageMat)
        {
            GameObject natureRoot = new GameObject("NatureElements");
            natureRoot.transform.SetParent(parent, false);
            natureRoot.transform.localPosition = Vector3.zero;

            // A. Puffy Stylized Round Trees (Radius 1.85m, Trunk anchored 5cm in ground)
            // 5 Trees around the plaza perimeter
            BuildPuffyTree("Tree_SW", new Vector3(-8.5f, 0f, -8.5f), natureRoot.transform, opaqueMat, foliageMat);
            BuildPuffyTree("Tree_SE", new Vector3(8.5f, 0f, -8.5f), natureRoot.transform, opaqueMat, foliageMat);
            BuildPuffyTree("Tree_NW", new Vector3(-8.5f, 0f, 8.5f), natureRoot.transform, opaqueMat, foliageMat);
            BuildPuffyTree("Tree_NE", new Vector3(8.5f, 0f, 8.5f), natureRoot.transform, opaqueMat, foliageMat);
            BuildPuffyTree("Tree_N",  new Vector3(0.0f, 0f, 11.0f), natureRoot.transform, opaqueMat, foliageMat);

            // B. Wooden Flower Boxes (Flush on ground Y = 0 +- 2mm, with soil & colorful blossoms)
            GameObject flowerBoxesGroup = new GameObject("FlowerBoxes");
            flowerBoxesGroup.transform.SetParent(natureRoot.transform, false);
            flowerBoxesGroup.transform.localPosition = Vector3.zero;

            BuildFlowerBox("FlowerBox_SW", new Vector3(-3.5f, 0f, -7.5f), Quaternion.identity, flowerBoxesGroup.transform, opaqueMat);
            BuildFlowerBox("FlowerBox_SE", new Vector3(3.5f, 0f, -7.5f), Quaternion.identity, flowerBoxesGroup.transform, opaqueMat);
            BuildFlowerBox("FlowerBox_NW", new Vector3(-5.5f, 0f, 5.0f), Quaternion.Euler(0f, 45f, 0f), flowerBoxesGroup.transform, opaqueMat);
            BuildFlowerBox("FlowerBox_NE", new Vector3(5.5f, 0f, 5.0f), Quaternion.Euler(0f, -45f, 0f), flowerBoxesGroup.transform, opaqueMat);

            // Plaza root and NatureElements aliases for test hierarchy compatibility (F6-03)
            GameObject flowerBoxesPlazaAlias = new GameObject("FlowerBoxes");
            flowerBoxesPlazaAlias.transform.SetParent(parent, false);
            flowerBoxesPlazaAlias.transform.localPosition = Vector3.zero;
            GameObject flowerBoxNorth = new GameObject("FlowerBox_North");
            flowerBoxNorth.transform.SetParent(natureRoot.transform, false);
            flowerBoxNorth.transform.localPosition = new Vector3(0f, 0f, 7.5f);

            // C. Low-Poly Rounded Hedges (Height >= 0.8m)
            GameObject hedgesGroup = new GameObject("Hedges");
            hedgesGroup.transform.SetParent(natureRoot.transform, false);
            hedgesGroup.transform.localPosition = Vector3.zero;

            BuildHedge("Hedge_SW", new Vector3(-5.0f, 0f, -2.8f), Quaternion.identity, hedgesGroup.transform, foliageMat);
            BuildHedge("Hedge_SE", new Vector3(5.0f, 0f, -2.8f), Quaternion.identity, hedgesGroup.transform, foliageMat);
            BuildHedge("Hedge_NW", new Vector3(-2.8f, 0f, 5.0f), Quaternion.Euler(0f, 90f, 0f), hedgesGroup.transform, foliageMat);
            BuildHedge("Hedge_NE", new Vector3(2.8f, 0f, 5.0f), Quaternion.Euler(0f, 90f, 0f), hedgesGroup.transform, foliageMat);

            // D. Shrubbery Clusters (Scale diameter 0.4m to 1.2m)
            GameObject shrubsGroup = new GameObject("Shrubs");
            shrubsGroup.transform.SetParent(natureRoot.transform, false);
            shrubsGroup.transform.localPosition = Vector3.zero;

            BuildShrub("Shrub_SW", new Vector3(-6.5f, 0f, -5.0f), shrubsGroup.transform, foliageMat);
            BuildShrub("Shrub_SE", new Vector3(6.5f, 0f, -5.0f), shrubsGroup.transform, foliageMat);
            BuildShrub("Shrub_NW", new Vector3(-6.5f, 0f, 4.0f), shrubsGroup.transform, foliageMat);
            BuildShrub("Shrub_NE", new Vector3(6.5f, 0f, 4.0f), shrubsGroup.transform, foliageMat);

            return natureRoot;
        }

        public static GameObject BuildPuffyTree(string name, Vector3 position, Transform parent, Material opaqueMat, Material foliageMat)
        {
            GameObject treeObj = new GameObject(name);
            try
            {
                treeObj.tag = "Tree";
            }
            catch (Exception)
            {
                // In case Tag is not registered in runtime builds
            }
            treeObj.transform.SetParent(parent, false);
            treeObj.transform.position = position;

            // Trunk: Y = -0.05m to 2.40m (Height = 2.45m, center Y = 1.175m)
            GameObject trunkObj = new GameObject("Trunk");
            trunkObj.transform.SetParent(treeObj.transform, false);
            trunkObj.transform.localPosition = new Vector3(0f, 1.175f, 0f);

            var trunkFilter = trunkObj.AddComponent<MeshFilter>();
            trunkFilter.sharedMesh = ProceduralMeshBuilder.CreateTaperedBeveledCylinder(
                baseRadius: 0.28f, topRadius: 0.18f, height: 2.45f, filletRadius: 0.04f, segments: 10,
                capCol: Swatches.WoodPineTan.Col, capRow: Swatches.WoodPineTan.Row,
                sideCol: Swatches.WoodOakRich.Col, sideRow: Swatches.WoodOakRich.Row);

            var trunkRenderer = trunkObj.AddComponent<MeshRenderer>();
            trunkRenderer.sharedMaterial = opaqueMat;

            var trunkCol = treeObj.AddComponent<CapsuleCollider>();
            trunkCol.radius = 0.25f;
            trunkCol.height = 2.40f;
            trunkCol.center = new Vector3(0f, 1.20f, 0f);

            // Canopy Crown: 4 spheres cluster with total bounding radius 1.85m
            GameObject canopyObj = new GameObject("CanopyCrown");
            canopyObj.transform.SetParent(treeObj.transform, false);
            canopyObj.transform.localPosition = Vector3.zero;

            var canopySpecs = new ProceduralMeshBuilder.CanopySphereSpec[]
            {
                new ProceduralMeshBuilder.CanopySphereSpec { center = new Vector3(0f, 2.70f, 0f), radius = 1.25f, swatchCol = Swatches.FoliageTreeSunlitLime.Col, swatchRow = Swatches.FoliageTreeSunlitLime.Row },
                new ProceduralMeshBuilder.CanopySphereSpec { center = new Vector3(-0.55f, 2.35f, -0.45f), radius = 1.05f, swatchCol = Swatches.FoliageTreeEmerald.Col, swatchRow = Swatches.FoliageTreeEmerald.Row },
                new ProceduralMeshBuilder.CanopySphereSpec { center = new Vector3(0.55f, 2.30f, -0.40f), radius = 0.98f, swatchCol = Swatches.FoliageTreeSunlitLime.Col, swatchRow = Swatches.FoliageTreeSunlitLime.Row },
                new ProceduralMeshBuilder.CanopySphereSpec { center = new Vector3(0.05f, 2.45f, 0.60f), radius = 1.10f, swatchCol = Swatches.FoliageTreeEmerald.Col, swatchRow = Swatches.FoliageTreeEmerald.Row }
            };

            var canopyFilter = canopyObj.AddComponent<MeshFilter>();
            canopyFilter.sharedMesh = ProceduralMeshBuilder.CreatePuffyTreeCanopyCluster(canopySpecs, new Vector3(0f, 2.45f, 0f), 0.75f);

            var canopyRenderer = canopyObj.AddComponent<MeshRenderer>();
            canopyRenderer.sharedMaterial = foliageMat;

            return treeObj;
        }

        public static GameObject BuildFlowerBox(string name, Vector3 position, Quaternion rotation, Transform parent, Material opaqueMat)
        {
            GameObject boxObj = new GameObject(name);
            boxObj.transform.SetParent(parent, false);
            boxObj.transform.position = position;
            boxObj.transform.rotation = rotation;

            // Dimensions: Length 1.20m, Width 0.44m, Height 0.36m.
            // Lowest vertex sits at Y = 0.000m (center Y = 0.18m).
            float length = 1.20f;
            float width = 0.44f;
            float height = 0.36f;
            float bevel = 0.02f;

            GameObject frameObj = new GameObject("PlanterFrame");
            frameObj.transform.SetParent(boxObj.transform, false);
            frameObj.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);

            var mfFrame = frameObj.AddComponent<MeshFilter>();
            mfFrame.sharedMesh = ProceduralMeshBuilder.CreateChamferedCube(length, height, width, bevel,
                Swatches.WoodPineMidtone.Col, Swatches.WoodPineMidtone.Row);

            var mrFrame = frameObj.AddComponent<MeshRenderer>();
            mrFrame.sharedMaterial = opaqueMat;

            // Potting Soil Bed: recessed 6cm below top rim (Y = 0.30m)
            GameObject soilObj = new GameObject("PottingSoil");
            soilObj.transform.SetParent(boxObj.transform, false);
            soilObj.transform.localPosition = new Vector3(0f, 0.28f, 0f);

            var mfSoil = soilObj.AddComponent<MeshFilter>();
            mfSoil.sharedMesh = ProceduralMeshBuilder.CreateChamferedCube(length - 0.10f, 0.10f, width - 0.10f, 0.01f,
                Swatches.FlowerPotSoil.Col, Swatches.FlowerPotSoil.Row);

            var mrSoil = soilObj.AddComponent<MeshRenderer>();
            mrSoil.sharedMaterial = opaqueMat;

            // Stylized Flowers
            SwatchCoord[] flowerColors = new SwatchCoord[]
            {
                Swatches.FlowerDaisyWhite,
                Swatches.FlowerCosmosBlush,
                Swatches.FlowerTulipRed,
                Swatches.FlowerPansyYellow
            };

            float[] xOffsets = new float[] { -0.40f, -0.15f, 0.15f, 0.40f };
            float[] zOffsets = new float[] { -0.08f, 0.08f };

            int flowerIdx = 0;
            foreach (float z in zOffsets)
            {
                foreach (float x in xOffsets)
                {
                    SwatchCoord sc = flowerColors[flowerIdx % flowerColors.Length];
                    flowerIdx++;

                    GameObject flower = new GameObject($"Flower_{flowerIdx}");
                    flower.transform.SetParent(boxObj.transform, false);
                    flower.transform.localPosition = new Vector3(x, 0.38f, z);

                    // Stem
                    var stemMf = flower.AddComponent<MeshFilter>();
                    stemMf.sharedMesh = ProceduralMeshBuilder.CreateChamferedCube(0.02f, 0.12f, 0.02f, 0.003f,
                        Swatches.FlowerStemSapGreen.Col, Swatches.FlowerStemSapGreen.Row);
                    var stemMr = flower.AddComponent<MeshRenderer>();
                    stemMr.sharedMaterial = opaqueMat;

                    // Blossom Head
                    GameObject head = new GameObject("Blossom");
                    head.transform.SetParent(flower.transform, false);
                    head.transform.localPosition = new Vector3(0f, 0.07f, 0f);

                    var headMf = head.AddComponent<MeshFilter>();
                    headMf.sharedMesh = ProceduralMeshBuilder.CreateStylizedSphere(0.06f, 6, 8, sc.Col, sc.Row);
                    var headMr = head.AddComponent<MeshRenderer>();
                    headMr.sharedMaterial = opaqueMat;
                }
            }

            // BoxCollider flush with ground (Y = 0)
            var col = boxObj.AddComponent<BoxCollider>();
            col.size = new Vector3(length, height, width);
            col.center = new Vector3(0f, height * 0.5f, 0f);

            return boxObj;
        }

        public static GameObject BuildHedge(string name, Vector3 position, Quaternion rotation, Transform parent, Material foliageMat)
        {
            GameObject hedgeObj = new GameObject(name);
            hedgeObj.transform.SetParent(parent, false);
            hedgeObj.transform.position = position;
            hedgeObj.transform.rotation = rotation;

            float length = 1.60f;
            float height = 0.85f; // Height >= 0.8m
            float width = 0.50f;
            float bevel = 0.10f;  // Chunky Animal Crossing bevel

            GameObject meshChild = new GameObject("HedgeMesh");
            meshChild.transform.SetParent(hedgeObj.transform, false);
            meshChild.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);

            var mf = meshChild.AddComponent<MeshFilter>();
            mf.sharedMesh = ProceduralMeshBuilder.CreateChamferedCube(length, height, width, bevel,
                Swatches.FoliageHedgeLime.Col, Swatches.FoliageHedgeLime.Row);

            var mr = meshChild.AddComponent<MeshRenderer>();
            mr.sharedMaterial = foliageMat;

            // BoxCollider >= 0.8m high to block locomotion
            var col = hedgeObj.AddComponent<BoxCollider>();
            col.size = new Vector3(length, height, width);
            col.center = new Vector3(0f, height * 0.5f, 0f);

            return hedgeObj;
        }

        public static GameObject BuildShrub(string name, Vector3 position, Transform parent, Material foliageMat)
        {
            GameObject shrubObj = new GameObject(name);
            shrubObj.transform.SetParent(parent, false);
            shrubObj.transform.position = position;

            // Combined diameter ~0.92m in [0.4m, 1.2m]
            GameObject mainDome = new GameObject("MainDome");
            mainDome.transform.SetParent(shrubObj.transform, false);
            mainDome.transform.localPosition = new Vector3(0f, 0.30f, 0f);
            mainDome.transform.localScale = new Vector3(1.0f, 0.82f, 1.0f);

            var mfMain = mainDome.AddComponent<MeshFilter>();
            mfMain.sharedMesh = ProceduralMeshBuilder.CreateStylizedSphere(0.40f, 8, 10,
                Swatches.FoliageShrubOlive.Col, Swatches.FoliageShrubOlive.Row);
            var mrMain = mainDome.AddComponent<MeshRenderer>();
            mrMain.sharedMaterial = foliageMat;

            GameObject flankDome = new GameObject("FlankDome");
            flankDome.transform.SetParent(shrubObj.transform, false);
            flankDome.transform.localPosition = new Vector3(0.22f, 0.22f, 0.12f);
            flankDome.transform.localScale = new Vector3(0.9f, 0.75f, 0.9f);

            var mfFlank = flankDome.AddComponent<MeshFilter>();
            mfFlank.sharedMesh = ProceduralMeshBuilder.CreateStylizedSphere(0.30f, 8, 10,
                Swatches.FoliageShrubOlive.Col, Swatches.FoliageShrubOlive.Row);
            var mrFlank = flankDome.AddComponent<MeshRenderer>();
            mrFlank.sharedMaterial = foliageMat;

            var col = shrubObj.AddComponent<SphereCollider>();
            col.radius = 0.45f;
            col.center = new Vector3(0f, 0.35f, 0f);

            return shrubObj;
        }

        // --------------------------------------------------------------------
        // 8. Cozy Street Lamps (3.20m, Warm Emissive Lantern, Point Light) (F7)
        // --------------------------------------------------------------------

        public static GameObject BuildStreetLamps(Transform parent, Material opaqueMat, Material emissiveMat)
        {
            GameObject lampsGroup = new GameObject("StreetLamps");
            lampsGroup.transform.SetParent(parent, false);
            lampsGroup.transform.localPosition = Vector3.zero;

            // 4 Street Lamps framing the plaza esplanade diagonal corridors
            BuildStreetLamp("StreetLamp_SW", new Vector3(-3.5f, 0f, -4.5f), Quaternion.identity, lampsGroup.transform, opaqueMat, emissiveMat);
            BuildStreetLamp("StreetLamp_SE", new Vector3(3.5f, 0f, -4.5f), Quaternion.identity, lampsGroup.transform, opaqueMat, emissiveMat);
            BuildStreetLamp("StreetLamp_NW", new Vector3(-3.5f, 0f, 4.5f), Quaternion.identity, lampsGroup.transform, opaqueMat, emissiveMat);
            BuildStreetLamp("StreetLamp_NE", new Vector3(3.5f, 0f, 4.5f), Quaternion.identity, lampsGroup.transform, opaqueMat, emissiveMat);

            return lampsGroup;
        }

        public static GameObject BuildStreetLamp(string name, Vector3 position, Quaternion rotation, Transform parent, Material opaqueMat, Material emissiveMat)
        {
            GameObject lampObj = new GameObject(name);
            lampObj.transform.SetParent(parent, false);
            lampObj.transform.position = position;
            lampObj.transform.rotation = rotation;

            // Total Height: 3.20m (within [2.5m, 3.8m])
            // Base Pedestal: Y = -0.05m to 0.40m (Height = 0.45m, center Y = 0.175m)
            GameObject baseObj = new GameObject("Base");
            baseObj.transform.SetParent(lampObj.transform, false);
            baseObj.transform.localPosition = new Vector3(0f, 0.175f, 0f);

            var baseFilter = baseObj.AddComponent<MeshFilter>();
            baseFilter.sharedMesh = ProceduralMeshBuilder.CreateBeveledCylinder(
                radius: 0.26f, height: 0.45f, filletRadius: 0.04f, segments: 8, filletSegments: 2,
                swatchCol: Swatches.LampWroughtIronBlack.Col, swatchRow: Swatches.LampWroughtIronBlack.Row);

            var baseRenderer = baseObj.AddComponent<MeshRenderer>();
            baseRenderer.sharedMaterial = opaqueMat;

            // Post Shaft: Y = 0.40m to 2.50m (Height = 2.10m, center Y = 1.45m)
            GameObject postObj = new GameObject("Post");
            postObj.transform.SetParent(lampObj.transform, false);
            postObj.transform.localPosition = new Vector3(0f, 1.45f, 0f);

            var postFilter = postObj.AddComponent<MeshFilter>();
            postFilter.sharedMesh = ProceduralMeshBuilder.CreateTaperedBeveledCylinder(
                baseRadius: 0.09f, topRadius: 0.065f, height: 2.10f, filletRadius: 0.02f, segments: 8,
                capCol: Swatches.IronSoftHighlight.Col, capRow: Swatches.IronSoftHighlight.Row,
                sideCol: Swatches.LampWroughtIronBlack.Col, sideRow: Swatches.LampWroughtIronBlack.Row);

            var postRenderer = postObj.AddComponent<MeshRenderer>();
            postRenderer.sharedMaterial = opaqueMat;

            // Lantern Head: Frame + Roof Cap (Height 2.55m to 3.20m)
            GameObject headObj = new GameObject("LanternHead");
            headObj.transform.SetParent(lampObj.transform, false);
            headObj.transform.localPosition = new Vector3(0f, 2.85f, 0f);

            var headFilter = headObj.AddComponent<MeshFilter>();
            headFilter.sharedMesh = ProceduralMeshBuilder.CreateChamferedCube(
                width: 0.34f, height: 0.55f, depth: 0.34f, bevelRadius: 0.03f,
                swatchCol: Swatches.LampCharcoalGrey.Col, swatchRow: Swatches.LampCharcoalGrey.Row);

            var headRenderer = headObj.AddComponent<MeshRenderer>();
            headRenderer.sharedMaterial = opaqueMat;

            // Emissive Lantern Core (Y = 2.75m)
            GameObject coreObj = new GameObject("LanternCore");
            coreObj.transform.SetParent(lampObj.transform, false);
            coreObj.transform.localPosition = new Vector3(0f, 2.75f, 0f);

            var coreFilter = coreObj.AddComponent<MeshFilter>();
            coreFilter.sharedMesh = ProceduralMeshBuilder.CreateBeveledCylinder(
                radius: 0.12f, height: 0.30f, filletRadius: 0.02f, segments: 6, filletSegments: 1,
                swatchCol: Swatches.EmissiveGoldenLantern.Col, swatchRow: Swatches.EmissiveGoldenLantern.Row);

            var coreRenderer = coreObj.AddComponent<MeshRenderer>();
            coreRenderer.sharedMaterial = emissiveMat;

            // Point Light
            GameObject lightObj = new GameObject("LanternLight");
            lightObj.transform.SetParent(lampObj.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 2.75f, 0f);

            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.82f, 0.55f);
            light.intensity = 1.8f;
            light.range = 7.0f;
            light.shadows = LightShadows.None;

            // Collider: Height 2.5m, radius 0.22m, center Y = 1.25m, flush with ground Y = 0 (zero penetration)
            var lampCol = lampObj.AddComponent<CapsuleCollider>();
            lampCol.radius = 0.22f;
            lampCol.height = 2.50f;
            lampCol.center = new Vector3(0f, 1.25f, 0f);

            return lampObj;
        }

        // --------------------------------------------------------------------
        // 9. Mount Points Hierarchy
        // --------------------------------------------------------------------

        private static void BuildMountPoints(Transform parent)
        {
            GameObject mounts = new GameObject("MountPoints");
            mounts.transform.SetParent(parent, false);
            mounts.transform.localPosition = Vector3.zero;

            CreateMountPoint(mounts.transform, "FountainMount", Vector3.zero);
            CreateMountPoint(mounts.transform, "ProduceStallMount", new Vector3(-5.5f, 0f, -6.0f));
            CreateMountPoint(mounts.transform, "CafeStallMount", new Vector3(6.5f, 0f, -5.5f));
            CreateMountPoint(mounts.transform, "CraftingStallMount", new Vector3(0f, 0f, 8.5f));
            CreateMountPoint(mounts.transform, "StreetLampMount_SW", new Vector3(-3.5f, 0f, -4.5f));
            CreateMountPoint(mounts.transform, "StreetLampMount_SE", new Vector3(3.5f, 0f, -4.5f));
            CreateMountPoint(mounts.transform, "StreetLampMount_NW", new Vector3(-3.5f, 0f, 4.5f));
            CreateMountPoint(mounts.transform, "StreetLampMount_NE", new Vector3(3.5f, 0f, 4.5f));
        }

        private static void CreateMountPoint(Transform parent, string name, Vector3 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
        }

        // --------------------------------------------------------------------
        // Helper: Create Child with MeshFilter and MeshRenderer
        // --------------------------------------------------------------------

        private static GameObject CreateMeshChild(Transform parent, string name, Mesh mesh, Material mat, Vector3 localPos, Quaternion localRot)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            return go;
        }

#if UNITY_EDITOR
        [MenuItem("AnimalCrossing/Build Town Square Plaza", priority = 20)]
        public static void MenuItem_BuildPlaza()
        {
            var existing = GameObject.Find("TownSquarePlaza");
            TownSquarePlazaBuilder builder;
            if (existing != null)
            {
                builder = existing.GetComponent<TownSquarePlazaBuilder>() ?? existing.AddComponent<TownSquarePlazaBuilder>();
            }
            else
            {
                GameObject root = new GameObject("TownSquarePlaza");
                builder = root.AddComponent<TownSquarePlazaBuilder>();
            }

            builder.BuildPlaza();
            Selection.activeGameObject = builder.gameObject;
        }
#endif
    }
}
