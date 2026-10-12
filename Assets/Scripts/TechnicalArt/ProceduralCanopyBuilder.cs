using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimalCrossing.TechnicalArt
{
    /// <summary>
    /// Configuration data for generating a stylized Animal Crossing cloth market stall awning.
    /// </summary>
    [System.Serializable]
    public struct CanopyConfig
    {
        public float width;              // Total awning width along X (e.g. 2.2m)
        public float depth;              // Total awning depth along Z (e.g. 1.8m)
        public float slopeDrop;          // Slope drop along depth (e.g. 0.35m)
        public float sagDepth;           // Catenary mid-span sagging amplitude (e.g. 0.10m)
        public int stripeCount;          // Alternating stripe count across width (e.g. 8)
        public int depthSegments;        // Subdivision segments along depth (e.g. 12)
        public int swatchColA;           // Swatch A column (primary color)
        public int swatchRowA;           // Swatch A row
        public int swatchColB;           // Swatch B column (secondary cream/accent color)
        public int swatchRowB;           // Swatch B row
        public float valanceDrop;        // Front hanging valance height (e.g. 0.18m)
        public float scallopAmplitude;   // Hanging scallop wave dip depth (e.g. 0.05m)
        public int scallopsPerStripe;    // Number of scallop lobes per stripe (default 1)
        public float clothThickness;     // Double-sided cloth thickness for VR interior view (e.g. 0.008m = 8mm)
        public bool includeSideValances; // Generate side valances along left and right eaves

        public static CanopyConfig CreateDefault()
        {
            return new CanopyConfig
            {
                width = 2.2f,
                depth = 1.8f,
                slopeDrop = 0.35f,
                sagDepth = 0.10f,
                stripeCount = 8,
                depthSegments = 12,
                swatchColA = 0,
                swatchRowA = 4,   // Default Red
                swatchColB = 3,
                swatchRowB = 0,   // Default Cream
                valanceDrop = 0.18f,
                scallopAmplitude = 0.05f,
                scallopsPerStripe = 1,
                clothThickness = 0.008f,
                includeSideValances = true
            };
        }
    }

    /// <summary>
    /// Generates stylized catenary sagging cloth awnings with sinusoidal hanging scalloped valances
    /// and zero-bleed alternating stripe color palette UV mapping.
    /// </summary>
    public static class ProceduralCanopyBuilder
    {
        /// <summary>
        /// Generates a complete procedural stall canopy mesh from the provided configuration.
        /// </summary>
        public static Mesh CreateCanopy(CanopyConfig config)
        {
            float w = Mathf.Max(0.5f, config.width);
            float d = Mathf.Max(0.5f, config.depth);
            float slope = Mathf.Max(0f, config.slopeDrop);
            float sag = Mathf.Max(0f, config.sagDepth);
            int stripes = Mathf.Max(2, config.stripeCount);
            int depthSeg = Mathf.Max(4, config.depthSegments);
            float thickness = Mathf.Max(0.002f, config.clothThickness);
            float vDrop = Mathf.Max(0.05f, config.valanceDrop);
            float sAmp = Mathf.Clamp(config.scallopAmplitude, 0f, vDrop * 0.8f);
            int sPerStripe = Mathf.Max(1, config.scallopsPerStripe);

            Vector2 uvA = ProceduralMeshBuilder.GetSwatchUV(config.swatchColA, config.swatchRowA);
            Vector2 uvB = ProceduralMeshBuilder.GetSwatchUV(config.swatchColB, config.swatchRowB);

            ProceduralMeshBuilder.MeshBuffer buffer = new ProceduralMeshBuilder.MeshBuffer();

            float halfW = w * 0.5f;
            float halfD = d * 0.5f;
            float stripeW = w / stripes;

            // Helper function to evaluate catenary deck surface position and analytic normal
            Vector3 EvalDeck(float x, float tz)
            {
                float z = -halfD + tz * d;
                float ySlope = -tz * slope;
                float ySag = -4f * sag * tz * (1f - tz);
                return new Vector3(x, ySlope + ySag, z);
            }

            Vector3 EvalDeckNormal(float tz)
            {
                float dy_dtz = -slope - 4f * sag * (1f - 2f * tz);
                float dy_dz = dy_dtz / d;
                return new Vector3(0f, 1f, -dy_dz).normalized;
            }

            // 1. Catenary Sagging Roof Deck (Top and VR Underside Lining)
            for (int s = 0; s < stripes; s++)
            {
                Vector2 stripeUV = (s % 2 == 0) ? uvA : uvB;
                float x0 = -halfW + s * stripeW;
                float x1 = x0 + stripeW;

                for (int j = 0; j < depthSeg; j++)
                {
                    float tz0 = j / (float)depthSeg;
                    float tz1 = (j + 1) / (float)depthSeg;

                    Vector3 p00 = EvalDeck(x0, tz0);
                    Vector3 p10 = EvalDeck(x1, tz0);
                    Vector3 p11 = EvalDeck(x1, tz1);
                    Vector3 p01 = EvalDeck(x0, tz1);

                    Vector3 n0 = EvalDeckNormal(tz0);
                    Vector3 n1 = EvalDeckNormal(tz1);

                    // Top deck face (facing outward)
                    buffer.AddQuad(p00, p10, p11, p01, n0, n0, n1, n1, stripeUV);

                    // Underside lining offset by clothThickness along negative normal
                    Vector3 u00 = p00 - n0 * thickness;
                    Vector3 u10 = p10 - n0 * thickness;
                    Vector3 u11 = p11 - n1 * thickness;
                    Vector3 u01 = p01 - n1 * thickness;

                    buffer.AddQuad(u00, u01, u11, u10, -n0, -n1, -n1, -n0, stripeUV);
                }
            }

            // 2. Front Hanging Scalloped Valance Waves
            int subStepsPerScallop = 8;
            for (int s = 0; s < stripes; s++)
            {
                Vector2 stripeUV = (s % 2 == 0) ? uvA : uvB;
                float xStripeStart = -halfW + s * stripeW;

                for (int sc = 0; sc < sPerStripe; sc++)
                {
                    float scW = stripeW / sPerStripe;
                    float xScallopStart = xStripeStart + sc * scW;

                    for (int step = 0; step < subStepsPerScallop; step++)
                    {
                        float u0 = step / (float)subStepsPerScallop;
                        float u1 = (step + 1) / (float)subStepsPerScallop;

                        float xStep0 = xScallopStart + u0 * scW;
                        float xStep1 = xScallopStart + u1 * scW;

                        Vector3 topDeck0 = EvalDeck(xStep0, 1f);
                        Vector3 topDeck1 = EvalDeck(xStep1, 1f);

                        // Sinusoidal hanging wave profile
                        float dip0 = vDrop + sAmp * Mathf.Sin(u0 * Mathf.PI);
                        float dip1 = vDrop + sAmp * Mathf.Sin(u1 * Mathf.PI);

                        Vector3 bot0 = new Vector3(xStep0, topDeck0.y - dip0, halfD);
                        Vector3 bot1 = new Vector3(xStep1, topDeck1.y - dip1, halfD);

                        // Front-facing valance quads (+Z)
                        buffer.AddQuad(bot0, bot1, topDeck1, topDeck0, Vector3.forward, stripeUV);

                        // Rear-facing underside valance quads (-Z) offset by thickness
                        Vector3 bTop0 = new Vector3(topDeck0.x, topDeck0.y, halfD - thickness);
                        Vector3 bTop1 = new Vector3(topDeck1.x, topDeck1.y, halfD - thickness);
                        Vector3 bBot0 = new Vector3(bot0.x, bot0.y, halfD - thickness);
                        Vector3 bBot1 = new Vector3(bot1.x, bot1.y, halfD - thickness);

                        buffer.AddQuad(bBot1, bBot0, bTop0, bTop1, Vector3.back, stripeUV);

                        // Bottom enclosing rim edge quads
                        buffer.AddQuad(bot0, bBot0, bBot1, bot1, Vector3.down, stripeUV);
                    }
                }
            }

            // 3. Side Scalloped Valances (Left and Right eaves)
            if (config.includeSideValances)
            {
                int sideScallopCount = Mathf.Max(2, Mathf.RoundToInt(d / (stripeW * 0.8f)));
                int sideSubSteps = 6;

                // Left Eave (-X)
                Vector2 leftUV = uvA;
                for (int sc = 0; sc < sideScallopCount; sc++)
                {
                    float tzStart = sc / (float)sideScallopCount;
                    float tzEnd = (sc + 1) / (float)sideScallopCount;

                    for (int step = 0; step < sideSubSteps; step++)
                    {
                        float u0 = step / (float)sideSubSteps;
                        float u1 = (step + 1) / (float)sideSubSteps;
                        float tzStep0 = Mathf.Lerp(tzStart, tzEnd, u0);
                        float tzStep1 = Mathf.Lerp(tzStart, tzEnd, u1);

                        Vector3 topDeck0 = EvalDeck(-halfW, tzStep0);
                        Vector3 topDeck1 = EvalDeck(-halfW, tzStep1);

                        float dip0 = (vDrop * 0.75f) + sAmp * Mathf.Sin(u0 * Mathf.PI);
                        float dip1 = (vDrop * 0.75f) + sAmp * Mathf.Sin(u1 * Mathf.PI);

                        Vector3 bot0 = new Vector3(-halfW, topDeck0.y - dip0, topDeck0.z);
                        Vector3 bot1 = new Vector3(-halfW, topDeck1.y - dip1, topDeck1.z);

                        buffer.AddQuad(bot0, bot1, topDeck1, topDeck0, Vector3.left, leftUV);

                        // Inside face (+X)
                        Vector3 inTop0 = topDeck0 + Vector3.right * thickness;
                        Vector3 inTop1 = topDeck1 + Vector3.right * thickness;
                        Vector3 inBot0 = bot0 + Vector3.right * thickness;
                        Vector3 inBot1 = bot1 + Vector3.right * thickness;

                        buffer.AddQuad(inBot1, inBot0, inTop0, inTop1, Vector3.right, leftUV);
                        buffer.AddQuad(bot0, inBot0, inBot1, bot1, Vector3.down, leftUV);
                    }
                }

                // Right Eave (+X)
                Vector2 rightUV = (stripes % 2 == 0) ? uvB : uvA;
                for (int sc = 0; sc < sideScallopCount; sc++)
                {
                    float tzStart = sc / (float)sideScallopCount;
                    float tzEnd = (sc + 1) / (float)sideScallopCount;

                    for (int step = 0; step < sideSubSteps; step++)
                    {
                        float u0 = step / (float)sideSubSteps;
                        float u1 = (step + 1) / (float)sideSubSteps;
                        float tzStep0 = Mathf.Lerp(tzStart, tzEnd, u0);
                        float tzStep1 = Mathf.Lerp(tzStart, tzEnd, u1);

                        Vector3 topDeck0 = EvalDeck(halfW, tzStep0);
                        Vector3 topDeck1 = EvalDeck(halfW, tzStep1);

                        float dip0 = (vDrop * 0.75f) + sAmp * Mathf.Sin(u0 * Mathf.PI);
                        float dip1 = (vDrop * 0.75f) + sAmp * Mathf.Sin(u1 * Mathf.PI);

                        Vector3 bot0 = new Vector3(halfW, topDeck0.y - dip0, topDeck0.z);
                        Vector3 bot1 = new Vector3(halfW, topDeck1.y - dip1, topDeck1.z);

                        buffer.AddQuad(bot1, bot0, topDeck0, topDeck1, Vector3.right, rightUV);

                        // Inside face (-X)
                        Vector3 inTop0 = topDeck0 + Vector3.left * thickness;
                        Vector3 inTop1 = topDeck1 + Vector3.left * thickness;
                        Vector3 inBot0 = bot0 + Vector3.left * thickness;
                        Vector3 inBot1 = bot1 + Vector3.left * thickness;

                        buffer.AddQuad(inBot0, inBot1, inTop1, inTop0, Vector3.left, rightUV);
                        buffer.AddQuad(bot1, inBot1, inBot0, bot0, Vector3.down, rightUV);
                    }
                }
            }

            return buffer.Build($"ProceduralCanopy_{w:F1}x{d:F1}");
        }

        /// <summary>
        /// Generates a catenary sagging awning mesh directly parameterized for unit tests and simple setups.
        /// When sagDepth == 0, generates a flat awning (bounds.size.y == 0).
        /// </summary>
        public static Mesh CreateCanopyMesh(float width, float depth, float sagDepth, int stripes, int depthSegments, int swatchColA, int swatchRowA)
        {
            float w = Mathf.Max(0.1f, width);
            float d = Mathf.Max(0.1f, depth);
            float sag = Mathf.Max(0f, sagDepth);
            int sCount = Mathf.Max(2, stripes);
            int dSeg = Mathf.Max(2, depthSegments);

            Vector2 uvA = ProceduralMeshBuilder.GetSwatchUV(swatchColA, swatchRowA);
            Vector2 uvB = ProceduralMeshBuilder.GetSwatchUV((swatchColA + 1) % 16, swatchRowA);

            ProceduralMeshBuilder.MeshBuffer buffer = new ProceduralMeshBuilder.MeshBuffer();
            float halfW = w * 0.5f;
            float halfD = d * 0.5f;
            float stripeW = w / sCount;

            Vector3 EvalDeck(float x, float tz)
            {
                float z = -halfD + tz * d;
                float ySag = -4f * sag * tz * (1f - tz);
                return new Vector3(x, ySag, z);
            }

            Vector3 EvalNormal(float tz)
            {
                if (sag <= 0.0001f) return Vector3.up;
                float dy_dtz = -4f * sag * (1f - 2f * tz);
                float dy_dz = dy_dtz / d;
                return new Vector3(0f, 1f, -dy_dz).normalized;
            }

            for (int s = 0; s < sCount; s++)
            {
                Vector2 stripeUV = (s % 2 == 0) ? uvA : uvB;
                float x0 = -halfW + s * stripeW;
                float x1 = x0 + stripeW;

                for (int j = 0; j < dSeg; j++)
                {
                    float tz0 = j / (float)dSeg;
                    float tz1 = (j + 1) / (float)dSeg;

                    Vector3 p00 = EvalDeck(x0, tz0);
                    Vector3 p10 = EvalDeck(x1, tz0);
                    Vector3 p11 = EvalDeck(x1, tz1);
                    Vector3 p01 = EvalDeck(x0, tz1);

                    Vector3 n0 = EvalNormal(tz0);
                    Vector3 n1 = EvalNormal(tz1);

                    buffer.AddQuad(p00, p10, p11, p01, n0, n0, n1, n1, stripeUV);
                }
            }

            return buffer.Build($"CanopyMesh_{w:F1}x{d:F1}");
        }

        /// <summary>
        /// Generates a decorative scalloped valance mesh with sinusoidal waves along the width.
        /// When scallopAmplitude == 0, produces a straight clean valance edge.
        /// </summary>
        public static Mesh CreateScallopedValance(float width, float valanceDrop, int scallopCount, float scallopAmplitude, int swatchCol, int swatchRow)
        {
            float w = Mathf.Max(0.1f, width);
            float vDrop = Mathf.Max(0.01f, valanceDrop);
            int sCount = Mathf.Max(1, scallopCount);
            float sAmp = Mathf.Max(0f, scallopAmplitude);

            Vector2 uv = ProceduralMeshBuilder.GetSwatchUV(swatchCol, swatchRow);
            ProceduralMeshBuilder.MeshBuffer buffer = new ProceduralMeshBuilder.MeshBuffer();

            float halfW = w * 0.5f;
            float lobeW = w / sCount;
            int stepsPerLobe = (sAmp > 0.0001f) ? 8 : 1;

            for (int s = 0; s < sCount; s++)
            {
                float xStart = -halfW + s * lobeW;

                for (int step = 0; step < stepsPerLobe; step++)
                {
                    float u0 = step / (float)stepsPerLobe;
                    float u1 = (step + 1) / (float)stepsPerLobe;

                    float x0 = xStart + u0 * lobeW;
                    float x1 = xStart + u1 * lobeW;

                    float dip0 = vDrop + sAmp * Mathf.Sin(u0 * Mathf.PI);
                    float dip1 = vDrop + sAmp * Mathf.Sin(u1 * Mathf.PI);

                    Vector3 top0 = new Vector3(x0, 0f, 0f);
                    Vector3 top1 = new Vector3(x1, 0f, 0f);
                    Vector3 bot0 = new Vector3(x0, -dip0, 0f);
                    Vector3 bot1 = new Vector3(x1, -dip1, 0f);

                    buffer.AddQuad(bot0, bot1, top1, top0, Vector3.forward, uv);
                }
            }

            return buffer.Build($"ScallopedValance_{w:F1}");
        }

        #region Standard Stall Preset Builders

        /// <summary>
        /// Nook-style Produce & Fruit Stall Canopy preset (Red / Cream stripes).
        /// </summary>
        public static Mesh CreateProduceStallCanopy()
        {
            CanopyConfig config = CanopyConfig.CreateDefault();
            config.width = 2.2f;
            config.depth = 1.8f;
            config.slopeDrop = 0.35f;
            config.sagDepth = 0.10f;
            config.stripeCount = 8;
            config.swatchColA = 0; // Crimson Red
            config.swatchRowA = 4;
            config.swatchColB = 3; // Vanilla Cream
            config.swatchRowB = 0;
            return CreateCanopy(config);
        }

        /// <summary>
        /// Cozy Café & Bakery Stall Canopy preset (Forest Green / Cream stripes).
        /// </summary>
        public static Mesh CreateCafeStallCanopy()
        {
            CanopyConfig config = CanopyConfig.CreateDefault();
            config.width = 2.2f;
            config.depth = 1.8f;
            config.slopeDrop = 0.35f;
            config.sagDepth = 0.09f;
            config.stripeCount = 8;
            config.swatchColA = 2; // Forest Green
            config.swatchRowA = 4;
            config.swatchColB = 3; // Vanilla Cream
            config.swatchRowB = 0;
            return CreateCanopy(config);
        }

        /// <summary>
        /// DIY Crafting & Gardening Stall Canopy preset (Teal / Warm Peach stripes).
        /// </summary>
        public static Mesh CreateGardeningStallCanopy()
        {
            CanopyConfig config = CanopyConfig.CreateDefault();
            config.width = 2.4f;
            config.depth = 1.9f;
            config.slopeDrop = 0.38f;
            config.sagDepth = 0.11f;
            config.stripeCount = 8;
            config.swatchColA = 4; // Dark Teal
            config.swatchRowA = 4;
            config.swatchColB = 5; // Warm Peach
            config.swatchRowB = 1;
            return CreateCanopy(config);
        }

        #endregion
    }
}
