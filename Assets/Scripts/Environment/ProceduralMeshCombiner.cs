// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProceduralMeshCombiner.cs" company="VR Market Scene">
//   Copyright (c) 2026 Animal Crossing Stylized VR Village Project. All rights reserved.
// </copyright>
// <summary>
//   Static batching utility for the Animal Crossing VR Village Market Scene (Feature F21).
//   Combines static procedural geometry sharing identical master materials into
//   unified MeshFilter/MeshRenderer pairs while strictly preserving separate
//   primitive colliders (Box, Capsule, Sphere) on source GameObjects.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimalCrossing.Environment
{
    /// <summary>
    /// Static batching utility for the Animal Crossing VR Village Market Scene.
    /// Combines static procedural geometry sharing identical master materials into
    /// unified MeshFilter/MeshRenderer pairs while strictly preserving separate
    /// primitive colliders (Box, Capsule, Sphere) on source GameObjects.
    /// </summary>
    public static class ProceduralMeshCombiner
    {
        public const int MaxVerticesPerSubmesh = 65534; // UInt16 limit for peak VR mobile cache performance

        /// <summary>
        /// Checks whether a GameObject represents an existing static batch container or sub-chunk.
        /// </summary>
        public static bool IsBatchContainer(GameObject go)
        {
            if (go == null) return false;
            string name = go.name;
            return name.StartsWith("Batch_Plaza_") ||
                   name.StartsWith("Combined_StaticMesh") ||
                   name.Contains("_Chunk");
        }

        /// <summary>
        /// Safely destroys a batch container GameObject and its procedural meshes across Editor and Play modes.
        /// In Play mode, unparents and deactivates immediately so subsequent hierarchy queries are accurate.
        /// </summary>
        private static void DestroyBatchGameObject(GameObject go)
        {
            if (go == null) return;

            // Clean up dynamically generated procedural meshes to prevent Editor memory leaks
            var meshFilters = go.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                if (mf != null && mf.sharedMesh != null)
                {
                    Mesh m = mf.sharedMesh;
                    mf.sharedMesh = null;
                    if (!Application.isPlaying)
                    {
                        UnityEngine.Object.DestroyImmediate(m);
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(m);
                    }
                }
            }

            if (Application.isPlaying)
            {
                go.transform.SetParent(null);
                go.SetActive(false);
                UnityEngine.Object.Destroy(go);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Searches for and destroys any existing batch container GameObjects under root matching batchName.
        /// </summary>
        public static void DestroyExistingBatches(GameObject root, string batchName)
        {
            if (root == null || string.IsNullOrEmpty(batchName)) return;

            List<GameObject> toDestroy = new List<GameObject>();
            for (int i = 0; i < root.transform.childCount; i++)
            {
                Transform child = root.transform.GetChild(i);
                if (child.name == batchName || child.name.StartsWith($"{batchName}_"))
                {
                    toDestroy.Add(child.gameObject);
                }
            }

            foreach (var go in toDestroy)
            {
                DestroyBatchGameObject(go);
            }
        }

        /// <summary>
        /// Searches for and destroys all existing plaza batch containers (Opaque, Foliage, Emissive).
        /// </summary>
        public static void ClearTownSquarePlazaBatches(GameObject plazaRoot)
        {
            if (plazaRoot == null) return;

            DestroyExistingBatches(plazaRoot, "Batch_Plaza_Opaque");
            DestroyExistingBatches(plazaRoot, "Batch_Plaza_Foliage");
            DestroyExistingBatches(plazaRoot, "Batch_Plaza_Emissive");
        }

        /// <summary>
        /// Combines all static mesh renderers under root that share targetMaterial into one or more combined meshes.
        /// Preserves primitive colliders and deactivates source mesh renderers.
        /// Automatically cleans up existing containers matching combinedName for 100% idempotent repeated calls.
        /// </summary>
        public static GameObject CombineStaticMeshes(GameObject root, Material targetMaterial, string combinedName = "Combined_StaticMesh")
        {
            if (root == null || targetMaterial == null) return null;

            // 1. Search for and destroy any existing batch container matching combinedName
            List<GameObject> existing = new List<GameObject>();
            for (int i = 0; i < root.transform.childCount; i++)
            {
                Transform child = root.transform.GetChild(i);
                if (child.name == combinedName || child.name.StartsWith($"{combinedName}_"))
                {
                    existing.Add(child.gameObject);
                }
            }

            if (existing.Count > 0)
            {
                foreach (var go in existing)
                {
                    DestroyBatchGameObject(go);
                }

                // Re-enable static source renderers matching targetMaterial that were deactivated by the previous batch
                var sourceRenderers = root.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var mr in sourceRenderers)
                {
                    if (mr == null) continue;
                    if (mr.sharedMaterial != targetMaterial) continue;
                    if (mr.GetComponentInParent<Rigidbody>() != null) continue;
                    if (IsBatchContainer(mr.gameObject) || mr.gameObject.name == combinedName || mr.gameObject.name.StartsWith($"{combinedName}_")) continue;
                    try
                    {
                        if (mr.CompareTag("Player") || mr.CompareTag("Interactable")) continue;
                    }
                    catch (Exception) { }

                    mr.enabled = true;
                }
            }

            // 2. Gather valid static mesh filters
            var meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            var validFilters = new List<MeshFilter>();

            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || !mr.enabled) continue;

                // Strictly ignore existing batch containers
                if (IsBatchContainer(mf.gameObject) || mf.gameObject.name == combinedName || mf.gameObject.name.StartsWith($"{combinedName}_")) continue;

                // Only combine renderers matching target material
                if (mr.sharedMaterial != targetMaterial) continue;

                // Never combine dynamic grabbables or physics rigidbodies
                if (mf.GetComponentInParent<Rigidbody>() != null) continue;
                try
                {
                    if (mf.CompareTag("Player") || mf.CompareTag("Interactable")) continue;
                }
                catch (Exception)
                {
                    // Tags might not be registered yet
                }

                validFilters.Add(mf);
            }

            if (validFilters.Count == 0) return null;

            // Chunk combine instances to respect 16-bit vertex limits
            List<List<CombineInstance>> chunks = new List<List<CombineInstance>>();
            List<CombineInstance> currentChunk = new List<CombineInstance>();
            int currentVertexCount = 0;

            Matrix4x4 rootInv = root.transform.worldToLocalMatrix;

            foreach (var mf in validFilters)
            {
                int vCount = mf.sharedMesh.vertexCount;
                if (currentVertexCount + vCount > MaxVerticesPerSubmesh && currentChunk.Count > 0)
                {
                    chunks.Add(currentChunk);
                    currentChunk = new List<CombineInstance>();
                    currentVertexCount = 0;
                }

                CombineInstance ci = new CombineInstance
                {
                    mesh = mf.sharedMesh,
                    transform = rootInv * mf.transform.localToWorldMatrix
                };

                currentChunk.Add(ci);
                currentVertexCount += vCount;
            }

            if (currentChunk.Count > 0)
            {
                chunks.Add(currentChunk);
            }

            GameObject combinedContainer = new GameObject(combinedName);
            combinedContainer.transform.SetParent(root.transform, false);
            combinedContainer.transform.localPosition = Vector3.zero;
            combinedContainer.transform.localRotation = Quaternion.identity;
            combinedContainer.transform.localScale = Vector3.one;

            for (int i = 0; i < chunks.Count; i++)
            {
                GameObject subObj = (chunks.Count == 1) ? combinedContainer : new GameObject($"{combinedName}_Chunk{i}");
                if (chunks.Count > 1)
                {
                    subObj.transform.SetParent(combinedContainer.transform, false);
                }

                Mesh combinedMesh = new Mesh();
                combinedMesh.name = $"{combinedName}_Mesh_{i}";
                combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
                combinedMesh.CombineMeshes(chunks[i].ToArray(), true, true, false);
                combinedMesh.RecalculateBounds();

                var mf = subObj.AddComponent<MeshFilter>();
                mf.sharedMesh = combinedMesh;

                var mr = subObj.AddComponent<MeshRenderer>();
                mr.sharedMaterial = targetMaterial;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                mr.receiveShadows = true;
            }

            // Deactivate source renderers while leaving Box/Sphere/Capsule colliders intact
            foreach (var mf in validFilters)
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.enabled = false;
                }
            }

            return combinedContainer;
        }

        /// <summary>
        /// Executes full static batching across the entire town square plaza for all 4 master materials.
        /// Searches for and destroys any existing batch containers before creating new batches, making repeated calls 100% idempotent.
        /// </summary>
        public static void BatchTownSquarePlaza(GameObject plazaRoot, Material opaqueMat, Material foliageMat, Material emissiveMat)
        {
            if (plazaRoot == null) return;

            // 1. Destroy any existing batch containers before re-batching
            ClearTownSquarePlazaBatches(plazaRoot);

            // 2. Restore/re-enable all static mesh renderers under plazaRoot so they are eligible for re-batching
            var allRenderers = plazaRoot.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in allRenderers)
            {
                if (mr == null) continue;
                if (mr.GetComponentInParent<Rigidbody>() != null) continue;
                if (IsBatchContainer(mr.gameObject)) continue;
                try
                {
                    if (mr.CompareTag("Player") || mr.CompareTag("Interactable")) continue;
                }
                catch (Exception)
                {
                    // Tags might not be registered yet
                }

                mr.enabled = true;
            }

            // 3. Combine Opaque Plaza geometry (Ground, paths, curbs, fountain stone, benches, flower box wood, lamp iron)
            if (opaqueMat != null)
            {
                CombineStaticMeshes(plazaRoot, opaqueMat, "Batch_Plaza_Opaque");
            }

            // 4. Combine Static Foliage geometry (Hedges, shrubs)
            if (foliageMat != null)
            {
                CombineStaticMeshes(plazaRoot, foliageMat, "Batch_Plaza_Foliage");
            }

            // 5. Combine Static Emissive geometry (Street lamp lantern cores)
            if (emissiveMat != null)
            {
                CombineStaticMeshes(plazaRoot, emissiveMat, "Batch_Plaza_Emissive");
            }
        }
    }
}
