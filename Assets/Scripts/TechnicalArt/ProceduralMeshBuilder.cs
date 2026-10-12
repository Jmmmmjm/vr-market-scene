using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimalCrossing.TechnicalArt
{
    /// <summary>
    /// Core procedural mesh generation library for the Animal Crossing stylized VR scene.
    /// Generates chamfered cubes, beveled cylinders, and stylized geometric primitives
    /// with zero-bleed color atlas swatch UV mapping and smooth shading normals.
    /// </summary>
    public static class ProceduralMeshBuilder
    {
        public const float ATLAS_CELLS = 16f;

        /// <summary>
        /// Returns the exact cell center UV coordinate for a swatch in the 16x16 palette atlas.
        /// Collapsing UVs to the cell center (dUV/dx = 0) mathematically eliminates texture edge bleeding.
        /// </summary>
        public static Vector2 GetSwatchUV(int col, int row)
        {
            col = Mathf.Clamp(col, 0, 15);
            row = Mathf.Clamp(row, 0, 15);
            return new Vector2((col + 0.5f) / ATLAS_CELLS, (row + 0.5f) / ATLAS_CELLS);
        }

        #region Chamfered Beveled Cube Generator

        /// <summary>
        /// Generates a beveled cube from size Vector3, supporting zero-bevel sharp cube edge case.
        /// </summary>
        public static Mesh CreateBeveledCube(Vector3 size, float bevelRadius, int bevelSegments, int swatchCol, int swatchRow)
        {
            if (bevelRadius <= 0.0001f)
            {
                return CreateSharpCube(size.x, size.y, size.z, swatchCol, swatchRow);
            }
            return CreateChamferedCube(size.x, size.y, size.z, bevelRadius, swatchCol, swatchRow);
        }

        /// <summary>
        /// Generates a standard sharp cube with 24 vertices, 12 triangles, and flat normals.
        /// </summary>
        public static Mesh CreateSharpCube(float width, float height, float depth, int swatchCol, int swatchRow)
        {
            float w = Mathf.Max(0.001f, width);
            float h = Mathf.Max(0.001f, height);
            float d = Mathf.Max(0.001f, depth);

            float hw = w * 0.5f;
            float hh = h * 0.5f;
            float hd = d * 0.5f;

            Vector2 uv = GetSwatchUV(swatchCol, swatchRow);
            MeshBuffer buffer = new MeshBuffer();

            // Top (+Y)
            buffer.AddQuad(new Vector3(-hw, hh, -hd), new Vector3(hw, hh, -hd), new Vector3(hw, hh, hd), new Vector3(-hw, hh, hd), Vector3.up, uv);
            // Bottom (-Y)
            buffer.AddQuad(new Vector3(-hw, -hh, hd), new Vector3(hw, -hh, hd), new Vector3(hw, -hh, -hd), new Vector3(-hw, -hh, -hd), Vector3.down, uv);
            // Front (+Z)
            buffer.AddQuad(new Vector3(-hw, -hh, hd), new Vector3(hw, -hh, hd), new Vector3(hw, hh, hd), new Vector3(-hw, hh, hd), Vector3.forward, uv);
            // Back (-Z)
            buffer.AddQuad(new Vector3(hw, -hh, -hd), new Vector3(-hw, -hh, -hd), new Vector3(-hw, hh, -hd), new Vector3(hw, hh, -hd), Vector3.back, uv);
            // Right (+X)
            buffer.AddQuad(new Vector3(hw, -hh, hd), new Vector3(hw, -hh, -hd), new Vector3(hw, hh, -hd), new Vector3(hw, hh, hd), Vector3.right, uv);
            // Left (-X)
            buffer.AddQuad(new Vector3(-hw, -hh, -hd), new Vector3(-hw, -hh, hd), new Vector3(-hw, hh, hd), new Vector3(-hw, hh, -hd), Vector3.left, uv);

            return buffer.Build($"SharpCube_{w:F2}x{h:F2}x{d:F2}");
        }

        /// <summary>
        /// Generates a chamfered cube with 6 inset planar faces, 12 chamfer edge quads,
        /// and 8 corner caps with 45-degree smoothed normals.
        /// </summary>
        public static Mesh CreateChamferedCube(float width, float height, float depth, float bevelRadius, int swatchCol, int swatchRow)
        {
            return CreateTwoToneChamferedCube(width, height, depth, bevelRadius, swatchCol, swatchRow, swatchCol, swatchRow);
        }

        /// <summary>
        /// Generates a chamfered cube with distinct swatch colors for the main inset faces and the chamfered edges/corners.
        /// </summary>
        public static Mesh CreateTwoToneChamferedCube(float width, float height, float depth, float bevelRadius,
            int faceCol, int faceRow, int bevelCol, int bevelRow)
        {
            float w = Mathf.Max(0.001f, width);
            float h = Mathf.Max(0.001f, height);
            float d = Mathf.Max(0.001f, depth);

            float hw = w * 0.5f;
            float hh = h * 0.5f;
            float hd = d * 0.5f;

            float maxBevel = Mathf.Min(hw, hh, hd) * 0.999f;
            float b = Mathf.Clamp(bevelRadius, 0.0001f, maxBevel);

            float iw = hw - b;
            float ih = hh - b;
            float id = hd - b;

            Vector2 faceUV = GetSwatchUV(faceCol, faceRow);
            Vector2 bevelUV = GetSwatchUV(bevelCol, bevelRow);

            MeshBuffer buffer = new MeshBuffer();

            // 1. Six Inset Planar Faces (Flat normals)
            // Top (+Y)
            buffer.AddQuad(
                new Vector3(-iw, hh, -id), new Vector3(iw, hh, -id),
                new Vector3(iw, hh, id), new Vector3(-iw, hh, id),
                Vector3.up, faceUV);

            // Bottom (-Y)
            buffer.AddQuad(
                new Vector3(-iw, -hh, id), new Vector3(iw, -hh, id),
                new Vector3(iw, -hh, -id), new Vector3(-iw, -hh, -id),
                Vector3.down, faceUV);

            // Front (+Z)
            buffer.AddQuad(
                new Vector3(-iw, -ih, hd), new Vector3(iw, -ih, hd),
                new Vector3(iw, ih, hd), new Vector3(-iw, ih, hd),
                Vector3.forward, faceUV);

            // Back (-Z)
            buffer.AddQuad(
                new Vector3(iw, -ih, -hd), new Vector3(-iw, -ih, -hd),
                new Vector3(-iw, ih, -hd), new Vector3(iw, ih, -hd),
                Vector3.back, faceUV);

            // Right (+X)
            buffer.AddQuad(
                new Vector3(hw, -ih, id), new Vector3(hw, -ih, -id),
                new Vector3(hw, ih, -id), new Vector3(hw, ih, id),
                Vector3.right, faceUV);

            // Left (-X)
            buffer.AddQuad(
                new Vector3(-hw, -ih, -id), new Vector3(-hw, -ih, id),
                new Vector3(-hw, ih, id), new Vector3(-hw, ih, -id),
                Vector3.left, faceUV);

            // 2. Twelve Chamfer Edge Quads (45-degree smoothed normals)
            // 4 Edges along Z-axis
            Vector3 nTR = new Vector3(1f, 1f, 0f).normalized;
            buffer.AddQuad(
                new Vector3(iw, hh, -id), new Vector3(iw, hh, id),
                new Vector3(hw, ih, id), new Vector3(hw, ih, -id),
                nTR, bevelUV);

            Vector3 nTL = new Vector3(-1f, 1f, 0f).normalized;
            buffer.AddQuad(
                new Vector3(-hw, ih, -id), new Vector3(-hw, ih, id),
                new Vector3(-iw, hh, id), new Vector3(-iw, hh, -id),
                nTL, bevelUV);

            Vector3 nBR = new Vector3(1f, -1f, 0f).normalized;
            buffer.AddQuad(
                new Vector3(hw, -ih, -id), new Vector3(hw, -ih, id),
                new Vector3(iw, -hh, id), new Vector3(iw, -hh, -id),
                nBR, bevelUV);

            Vector3 nBL = new Vector3(-1f, -1f, 0f).normalized;
            buffer.AddQuad(
                new Vector3(-iw, -hh, -id), new Vector3(-iw, -hh, id),
                new Vector3(-hw, -ih, id), new Vector3(-hw, -ih, -id),
                nBL, bevelUV);

            // 4 Edges along X-axis
            Vector3 nTF = new Vector3(0f, 1f, 1f).normalized;
            buffer.AddQuad(
                new Vector3(-iw, hh, id), new Vector3(iw, hh, id),
                new Vector3(iw, ih, hd), new Vector3(-iw, ih, hd),
                nTF, bevelUV);

            Vector3 nTB = new Vector3(0f, 1f, -1f).normalized;
            buffer.AddQuad(
                new Vector3(iw, hh, -id), new Vector3(-iw, hh, -id),
                new Vector3(-iw, ih, -hd), new Vector3(iw, ih, -hd),
                nTB, bevelUV);

            Vector3 nBF = new Vector3(0f, -1f, 1f).normalized;
            buffer.AddQuad(
                new Vector3(-iw, -ih, hd), new Vector3(iw, -ih, hd),
                new Vector3(iw, -hh, id), new Vector3(-iw, -hh, id),
                nBF, bevelUV);

            Vector3 nBB = new Vector3(0f, -1f, -1f).normalized;
            buffer.AddQuad(
                new Vector3(iw, -ih, -hd), new Vector3(-iw, -ih, -hd),
                new Vector3(-iw, -hh, -id), new Vector3(iw, -hh, -id),
                nBB, bevelUV);

            // 4 Edges along Y-axis
            Vector3 nFR = new Vector3(1f, 0f, 1f).normalized;
            buffer.AddQuad(
                new Vector3(iw, -ih, hd), new Vector3(hw, -ih, id),
                new Vector3(hw, ih, id), new Vector3(iw, ih, hd),
                nFR, bevelUV);

            Vector3 nFL = new Vector3(-1f, 0f, 1f).normalized;
            buffer.AddQuad(
                new Vector3(-hw, -ih, id), new Vector3(-iw, -ih, hd),
                new Vector3(-iw, ih, hd), new Vector3(-hw, ih, id),
                nFL, bevelUV);

            Vector3 nBR2 = new Vector3(1f, 0f, -1f).normalized;
            buffer.AddQuad(
                new Vector3(hw, -ih, -id), new Vector3(iw, -ih, -hd),
                new Vector3(iw, ih, -hd), new Vector3(hw, ih, -id),
                nBR2, bevelUV);

            Vector3 nBL2 = new Vector3(-1f, 0f, -1f).normalized;
            buffer.AddQuad(
                new Vector3(-iw, -ih, -hd), new Vector3(-hw, -ih, -id),
                new Vector3(-hw, ih, -id), new Vector3(-iw, ih, -hd),
                nBL2, bevelUV);

            // 3. Eight Corner Caps (45-degree / spherical 3-way smoothed normals)
            int[] signs = { -1, 1 };
            foreach (int sx in signs)
            {
                foreach (int sy in signs)
                {
                    foreach (int sz in signs)
                    {
                        Vector3 vx = new Vector3(sx * hw, sy * ih, sz * id);
                        Vector3 vy = new Vector3(sx * iw, sy * hh, sz * id);
                        Vector3 vz = new Vector3(sx * iw, sy * ih, sz * hd);
                        Vector3 cornerNormal = new Vector3(sx, sy, sz).normalized;

                        buffer.AddTriangle(vx, vy, vz, cornerNormal, cornerNormal, cornerNormal, bevelUV);
                    }
                }
            }

            return buffer.Build($"ChamferedCube_{w:F2}x{h:F2}x{d:F2}");
        }

        #endregion

        #region Beveled Cylinder Generator

        /// <summary>
        /// Generates a beveled cylinder matching the (radius, height, filletRadius, segments, filletSegments, swatchCol, swatchRow) signature.
        /// </summary>
        public static Mesh CreateBeveledCylinder(float radius, float height, float filletRadius, int segments, int filletSegments, int swatchCol, int swatchRow)
        {
            return CreateTwoToneBeveledCylinder(radius, height, filletRadius, segments, swatchCol, swatchRow, swatchCol, swatchRow, filletSegments);
        }

        /// <summary>
        /// Generates a beveled cylinder with circular flat caps, curved fillet rings,
        /// and smooth cylindrical side normals with default fillet segments.
        /// </summary>
        public static Mesh CreateSimpleBeveledCylinder(float radius, float height, float filletRadius, int segments, int swatchCol, int swatchRow)
        {
            return CreateTwoToneBeveledCylinder(radius, height, filletRadius, segments, swatchCol, swatchRow, swatchCol, swatchRow, 2);
        }

        /// <summary>
        /// Generates a beveled cylinder with distinct swatches for the end caps and cylindrical sides/fillets.
        /// </summary>
        public static Mesh CreateTwoToneBeveledCylinder(float radius, float height, float filletRadius, int segments,
            int capCol, int capRow, int sideCol, int sideRow, int filletSegments = 2)
        {
            float R = Mathf.Max(0.001f, radius);
            float H = Mathf.Max(0.001f, height);
            int seg = Mathf.Max(3, segments);
            int fSeg = Mathf.Clamp(filletSegments, 1, 8);

            float halfH = H * 0.5f;
            float maxFillet = Mathf.Min(R * 0.999f, halfH * 0.999f);
            float r = Mathf.Clamp(filletRadius, 0.0001f, maxFillet);

            float rInner = R - r;
            float hBarrel = halfH - r;

            Vector2 capUV = GetSwatchUV(capCol, capRow);
            Vector2 sideUV = GetSwatchUV(sideCol, sideRow);

            MeshBuffer buffer = new MeshBuffer();

            // Precompute angles
            float[] cosAngles = new float[seg + 1];
            float[] sinAngles = new float[seg + 1];
            for (int i = 0; i <= seg; i++)
            {
                float angle = (i % seg) * (Mathf.PI * 2f / seg);
                cosAngles[i] = Mathf.Cos(angle);
                sinAngles[i] = Mathf.Sin(angle);
            }

            // 1. Top Flat Cap (+Y)
            Vector3 topCenter = new Vector3(0f, halfH, 0f);
            for (int i = 0; i < seg; i++)
            {
                Vector3 p0 = new Vector3(rInner * cosAngles[i], halfH, rInner * sinAngles[i]);
                Vector3 p1 = new Vector3(rInner * cosAngles[i + 1], halfH, rInner * sinAngles[i + 1]);
                buffer.AddTriangle(topCenter, p0, p1, Vector3.up, Vector3.up, Vector3.up, capUV);
            }

            // 2. Top Fillet Rings
            for (int f = 0; f < fSeg; f++)
            {
                float phi0 = (f / (float)fSeg) * (Mathf.PI * 0.5f);
                float phi1 = ((f + 1) / (float)fSeg) * (Mathf.PI * 0.5f);

                float rf0 = rInner + r * Mathf.Sin(phi0);
                float yf0 = hBarrel + r * Mathf.Cos(phi0);
                float rf1 = rInner + r * Mathf.Sin(phi1);
                float yf1 = hBarrel + r * Mathf.Cos(phi1);

                for (int i = 0; i < seg; i++)
                {
                    Vector3 v00 = new Vector3(rf0 * cosAngles[i], yf0, rf0 * sinAngles[i]);
                    Vector3 v01 = new Vector3(rf0 * cosAngles[i + 1], yf0, rf0 * sinAngles[i + 1]);
                    Vector3 v11 = new Vector3(rf1 * cosAngles[i + 1], yf1, rf1 * sinAngles[i + 1]);
                    Vector3 v10 = new Vector3(rf1 * cosAngles[i], yf1, rf1 * sinAngles[i]);

                    Vector3 n00 = new Vector3(Mathf.Sin(phi0) * cosAngles[i], Mathf.Cos(phi0), Mathf.Sin(phi0) * sinAngles[i]).normalized;
                    Vector3 n01 = new Vector3(Mathf.Sin(phi0) * cosAngles[i + 1], Mathf.Cos(phi0), Mathf.Sin(phi0) * sinAngles[i + 1]).normalized;
                    Vector3 n11 = new Vector3(Mathf.Sin(phi1) * cosAngles[i + 1], Mathf.Cos(phi1), Mathf.Sin(phi1) * sinAngles[i + 1]).normalized;
                    Vector3 n10 = new Vector3(Mathf.Sin(phi1) * cosAngles[i], Mathf.Cos(phi1), Mathf.Sin(phi1) * sinAngles[i]).normalized;

                    buffer.AddQuad(v00, v01, v11, v10, n00, n01, n11, n10, sideUV);
                }
            }

            // 3. Middle Cylindrical Barrel (Side walls)
            for (int i = 0; i < seg; i++)
            {
                Vector3 vTop0 = new Vector3(R * cosAngles[i], hBarrel, R * sinAngles[i]);
                Vector3 vTop1 = new Vector3(R * cosAngles[i + 1], hBarrel, R * sinAngles[i + 1]);
                Vector3 vBot1 = new Vector3(R * cosAngles[i + 1], -hBarrel, R * sinAngles[i + 1]);
                Vector3 vBot0 = new Vector3(R * cosAngles[i], -hBarrel, R * sinAngles[i]);

                Vector3 n0 = new Vector3(cosAngles[i], 0f, sinAngles[i]);
                Vector3 n1 = new Vector3(cosAngles[i + 1], 0f, sinAngles[i + 1]);

                buffer.AddQuad(vTop0, vTop1, vBot1, vBot0, n0, n1, n1, n0, sideUV);
            }

            // 4. Bottom Fillet Rings
            for (int f = 0; f < fSeg; f++)
            {
                float phi0 = (f / (float)fSeg) * (Mathf.PI * 0.5f);
                float phi1 = ((f + 1) / (float)fSeg) * (Mathf.PI * 0.5f);

                float rf0 = rInner + r * Mathf.Cos(phi0);
                float yf0 = -hBarrel - r * Mathf.Sin(phi0);
                float rf1 = rInner + r * Mathf.Cos(phi1);
                float yf1 = -hBarrel - r * Mathf.Sin(phi1);

                for (int i = 0; i < seg; i++)
                {
                    Vector3 v00 = new Vector3(rf0 * cosAngles[i], yf0, rf0 * sinAngles[i]);
                    Vector3 v01 = new Vector3(rf0 * cosAngles[i + 1], yf0, rf0 * sinAngles[i + 1]);
                    Vector3 v11 = new Vector3(rf1 * cosAngles[i + 1], yf1, rf1 * sinAngles[i + 1]);
                    Vector3 v10 = new Vector3(rf1 * cosAngles[i], yf1, rf1 * sinAngles[i]);

                    Vector3 n00 = new Vector3(Mathf.Cos(phi0) * cosAngles[i], -Mathf.Sin(phi0), Mathf.Cos(phi0) * sinAngles[i]).normalized;
                    Vector3 n01 = new Vector3(Mathf.Cos(phi0) * cosAngles[i + 1], -Mathf.Sin(phi0), Mathf.Cos(phi0) * sinAngles[i + 1]).normalized;
                    Vector3 n11 = new Vector3(Mathf.Cos(phi1) * cosAngles[i + 1], -Mathf.Sin(phi1), Mathf.Cos(phi1) * sinAngles[i + 1]).normalized;
                    Vector3 n10 = new Vector3(Mathf.Cos(phi1) * cosAngles[i], -Mathf.Sin(phi1), Mathf.Cos(phi1) * sinAngles[i]).normalized;

                    buffer.AddQuad(v00, v01, v11, v10, n00, n01, n11, n10, sideUV);
                }
            }

            // 5. Bottom Flat Cap (-Y)
            Vector3 botCenter = new Vector3(0f, -halfH, 0f);
            for (int i = 0; i < seg; i++)
            {
                Vector3 p0 = new Vector3(rInner * cosAngles[i], -halfH, rInner * sinAngles[i]);
                Vector3 p1 = new Vector3(rInner * cosAngles[i + 1], -halfH, rInner * sinAngles[i + 1]);
                buffer.AddTriangle(botCenter, p1, p0, Vector3.down, Vector3.down, Vector3.down, capUV);
            }

            return buffer.Build($"BeveledCylinder_r{R:F2}_h{H:F2}");
        }

        #endregion

        #region Stylized Compound Primitives

        /// <summary>
        /// Generates a stylized UV sphere with smooth spherical vertex normals for fruits, puffy trees, and props.
        /// </summary>
        public static Mesh CreateStylizedSphere(float radius, int latSegments, int lonSegments, int swatchCol, int swatchRow)
        {
            float R = Mathf.Max(0.001f, radius);
            int latSeg = Mathf.Max(4, latSegments);
            int lonSeg = Mathf.Max(4, lonSegments);
            Vector2 uv = GetSwatchUV(swatchCol, swatchRow);

            MeshBuffer buffer = new MeshBuffer();

            for (int lat = 0; lat < latSeg; lat++)
            {
                float theta0 = (lat / (float)latSeg) * Mathf.PI;
                float theta1 = ((lat + 1) / (float)latSeg) * Mathf.PI;

                float y0 = Mathf.Cos(theta0) * R;
                float r0 = Mathf.Sin(theta0) * R;
                float y1 = Mathf.Cos(theta1) * R;
                float r1 = Mathf.Sin(theta1) * R;

                for (int lon = 0; lon < lonSeg; lon++)
                {
                    float phi0 = (lon / (float)lonSeg) * Mathf.PI * 2f;
                    float phi1 = ((lon + 1) / (float)lonSeg) * Mathf.PI * 2f;

                    Vector3 v00 = new Vector3(Mathf.Cos(phi0) * r0, y0, Mathf.Sin(phi0) * r0);
                    Vector3 v01 = new Vector3(Mathf.Cos(phi1) * r0, y0, Mathf.Sin(phi1) * r0);
                    Vector3 v11 = new Vector3(Mathf.Cos(phi1) * r1, y1, Mathf.Sin(phi1) * r1);
                    Vector3 v10 = new Vector3(Mathf.Cos(phi0) * r1, y1, Mathf.Sin(phi0) * r1);

                    Vector3 n00 = v00.normalized;
                    Vector3 n01 = v01.normalized;
                    Vector3 n11 = v11.normalized;
                    Vector3 n10 = v10.normalized;

                    if (lat == 0)
                        buffer.AddTriangle(v00, v11, v10, n00, n11, n10, uv);
                    else if (lat == latSeg - 1)
                        buffer.AddTriangle(v00, v01, v10, n00, n01, n10, uv);
                    else
                        buffer.AddQuad(v00, v01, v11, v10, n00, n01, n11, n10, uv);
                }
            }

            return buffer.Build($"StylizedSphere_r{R:F2}");
        }

        /// <summary>
        /// Generates a stylized torus with toroidal smoothed normals for donuts, mug handles, and basket handles.
        /// </summary>
        public static Mesh CreateBeveledTorus(float majorRadius, float minorRadius, int majorSegments, int minorSegments, int swatchCol, int swatchRow)
        {
            float R = Mathf.Max(0.01f, majorRadius);
            float r = Mathf.Clamp(minorRadius, 0.001f, R * 0.9f);
            int majSeg = Mathf.Max(6, majorSegments);
            int minSeg = Mathf.Max(4, minorSegments);
            Vector2 uv = GetSwatchUV(swatchCol, swatchRow);

            MeshBuffer buffer = new MeshBuffer();

            for (int i = 0; i < majSeg; i++)
            {
                float u0 = (i / (float)majSeg) * Mathf.PI * 2f;
                float u1 = ((i + 1) / (float)majSeg) * Mathf.PI * 2f;

                Vector3 center0 = new Vector3(Mathf.Cos(u0) * R, 0f, Mathf.Sin(u0) * R);
                Vector3 center1 = new Vector3(Mathf.Cos(u1) * R, 0f, Mathf.Sin(u1) * R);
                Vector3 dir0 = new Vector3(Mathf.Cos(u0), 0f, Mathf.Sin(u0));
                Vector3 dir1 = new Vector3(Mathf.Cos(u1), 0f, Mathf.Sin(u1));

                for (int j = 0; j < minSeg; j++)
                {
                    float v0 = (j / (float)minSeg) * Mathf.PI * 2f;
                    float v1 = ((j + 1) / (float)minSeg) * Mathf.PI * 2f;

                    Vector3 normal00 = (dir0 * Mathf.Cos(v0) + Vector3.up * Mathf.Sin(v0)).normalized;
                    Vector3 normal01 = (dir0 * Mathf.Cos(v1) + Vector3.up * Mathf.Sin(v1)).normalized;
                    Vector3 normal11 = (dir1 * Mathf.Cos(v1) + Vector3.up * Mathf.Sin(v1)).normalized;
                    Vector3 normal10 = (dir1 * Mathf.Cos(v0) + Vector3.up * Mathf.Sin(v0)).normalized;

                    Vector3 p00 = center0 + normal00 * r;
                    Vector3 p01 = center0 + normal01 * r;
                    Vector3 p11 = center1 + normal11 * r;
                    Vector3 p10 = center1 + normal10 * r;

                    buffer.AddQuad(p00, p01, p11, p10, normal00, normal01, normal11, normal10, uv);
                }
            }

            return buffer.Build($"BeveledTorus_R{R:F2}_r{r:F2}");
        }

        #endregion

        #region Stylized Foliage and Tapered Cylinders

        public struct CanopySphereSpec
        {
            public Vector3 center;
            public float radius;
            public int swatchCol;
            public int swatchRow;
        }

        /// <summary>
        /// Generates a tapered beveled cylinder for tree trunks and street lamp posts.
        /// </summary>
        public static Mesh CreateTaperedBeveledCylinder(
            float baseRadius, float topRadius, float height, float filletRadius,
            int segments, int capCol, int capRow, int sideCol, int sideRow)
        {
            float Rb = Mathf.Max(0.001f, baseRadius);
            float Rt = Mathf.Max(0.001f, topRadius);
            float H = Mathf.Max(0.001f, height);
            int seg = Mathf.Max(3, segments);

            float halfH = H * 0.5f;
            Vector2 capUV = GetSwatchUV(capCol, capRow);
            Vector2 sideUV = GetSwatchUV(sideCol, sideRow);

            MeshBuffer buffer = new MeshBuffer();

            float[] cosA = new float[seg + 1];
            float[] sinA = new float[seg + 1];
            for (int i = 0; i <= seg; i++)
            {
                float angle = (i % seg) * (Mathf.PI * 2f / seg);
                cosA[i] = Mathf.Cos(angle);
                sinA[i] = Mathf.Sin(angle);
            }

            // Top Cap (+Y)
            Vector3 topCenter = new Vector3(0f, halfH, 0f);
            for (int i = 0; i < seg; i++)
            {
                Vector3 p0 = new Vector3(Rt * cosA[i], halfH, Rt * sinA[i]);
                Vector3 p1 = new Vector3(Rt * cosA[i + 1], halfH, Rt * sinA[i + 1]);
                buffer.AddTriangle(topCenter, p0, p1, Vector3.up, Vector3.up, Vector3.up, capUV);
            }

            // Cylindrical Tapered Side Quads
            float slopeY = (Rb - Rt) / H;
            for (int i = 0; i < seg; i++)
            {
                Vector3 vTop0 = new Vector3(Rt * cosA[i], halfH, Rt * sinA[i]);
                Vector3 vTop1 = new Vector3(Rt * cosA[i + 1], halfH, Rt * sinA[i + 1]);
                Vector3 vBot1 = new Vector3(Rb * cosA[i + 1], -halfH, Rb * sinA[i + 1]);
                Vector3 vBot0 = new Vector3(Rb * cosA[i], -halfH, Rb * sinA[i]);

                Vector3 n0 = new Vector3(cosA[i], slopeY, sinA[i]).normalized;
                Vector3 n1 = new Vector3(cosA[i + 1], slopeY, sinA[i + 1]).normalized;

                buffer.AddQuad(vTop0, vTop1, vBot1, vBot0, n0, n1, n1, n0, sideUV);
            }

            // Bottom Cap (-Y)
            Vector3 botCenter = new Vector3(0f, -halfH, 0f);
            for (int i = 0; i < seg; i++)
            {
                Vector3 p0 = new Vector3(Rb * cosA[i], -halfH, Rb * sinA[i]);
                Vector3 p1 = new Vector3(Rb * cosA[i + 1], -halfH, Rb * sinA[i + 1]);
                buffer.AddTriangle(botCenter, p1, p0, Vector3.down, Vector3.down, Vector3.down, capUV);
            }

            return buffer.Build($"TaperedCylinder_{Rb:F2}to{Rt:F2}_h{H:F2}");
        }

        /// <summary>
        /// Generates a puffy tree canopy cluster with precomputed softened/spherized normals.
        /// </summary>
        public static Mesh CreatePuffyTreeCanopyCluster(CanopySphereSpec[] spheres, Vector3 clusterCentroid, float normalSoftening = 0.75f)
        {
            MeshBuffer buffer = new MeshBuffer();

            foreach (var s in spheres)
            {
                int latSeg = 8;
                int lonSeg = 12;
                Vector2 uv = GetSwatchUV(s.swatchCol, s.swatchRow);

                for (int lat = 0; lat < latSeg; lat++)
                {
                    float theta0 = (lat / (float)latSeg) * Mathf.PI;
                    float theta1 = ((lat + 1) / (float)latSeg) * Mathf.PI;

                    float y0 = Mathf.Cos(theta0) * s.radius;
                    float r0 = Mathf.Sin(theta0) * s.radius;
                    float y1 = Mathf.Cos(theta1) * s.radius;
                    float r1 = Mathf.Sin(theta1) * s.radius;

                    for (int lon = 0; lon < lonSeg; lon++)
                    {
                        float phi0 = (lon / (float)lonSeg) * Mathf.PI * 2f;
                        float phi1 = ((lon + 1) / (float)lonSeg) * Mathf.PI * 2f;

                        Vector3 v00 = s.center + new Vector3(Mathf.Cos(phi0) * r0, y0, Mathf.Sin(phi0) * r0);
                        Vector3 v01 = s.center + new Vector3(Mathf.Cos(phi1) * r0, y0, Mathf.Sin(phi1) * r0);
                        Vector3 v11 = s.center + new Vector3(Mathf.Cos(phi1) * r1, y1, Mathf.Sin(phi1) * r1);
                        Vector3 v10 = s.center + new Vector3(Mathf.Cos(phi0) * r1, y1, Mathf.Sin(phi0) * r1);

                        // Blend local sphere normal with cluster-wide spherical normal
                        Vector3 nLocal00 = (v00 - s.center).normalized;
                        Vector3 nLocal01 = (v01 - s.center).normalized;
                        Vector3 nLocal11 = (v11 - s.center).normalized;
                        Vector3 nLocal10 = (v10 - s.center).normalized;

                        Vector3 nCluster00 = (v00 - clusterCentroid).normalized;
                        Vector3 nCluster01 = (v01 - clusterCentroid).normalized;
                        Vector3 nCluster11 = (v11 - clusterCentroid).normalized;
                        Vector3 nCluster10 = (v10 - clusterCentroid).normalized;

                        Vector3 n00 = Vector3.Lerp(nLocal00, nCluster00, normalSoftening).normalized;
                        Vector3 n01 = Vector3.Lerp(nLocal01, nCluster01, normalSoftening).normalized;
                        Vector3 n11 = Vector3.Lerp(nLocal11, nCluster11, normalSoftening).normalized;
                        Vector3 n10 = Vector3.Lerp(nLocal10, nCluster10, normalSoftening).normalized;

                        if (lat == 0)
                            buffer.AddTriangle(v00, v11, v10, n00, n11, n10, uv);
                        else if (lat == latSeg - 1)
                            buffer.AddTriangle(v00, v01, v10, n00, n01, n10, uv);
                        else
                            buffer.AddQuad(v00, v01, v11, v10, n00, n01, n11, n10, uv);
                    }
                }
            }

            return buffer.Build("PuffyTreeCanopyCluster");
        }

        #endregion

        #region Internal MeshBuffer Helper

        public class MeshBuffer
        {
            public List<Vector3> vertices = new List<Vector3>();
            public List<Vector3> normals = new List<Vector3>();
            public List<Vector2> uvs = new List<Vector2>();
            public List<int> triangles = new List<int>();

            public void AddTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 n0, Vector3 n1, Vector3 n2, Vector2 uv)
            {
                // Dynamic outward orientation enforcement
                Vector3 faceNormal = Vector3.Cross(v1 - v0, v2 - v0);
                Vector3 targetNormal = (n0 + n1 + n2) / 3f;
                if (Vector3.Dot(faceNormal, targetNormal) < 0f)
                {
                    Vector3 tempV = v1; v1 = v2; v2 = tempV;
                    Vector3 tempN = n1; n1 = n2; n2 = tempN;
                }

                int baseIdx = vertices.Count;
                vertices.Add(v0);
                vertices.Add(v1);
                vertices.Add(v2);

                normals.Add(n0.normalized);
                normals.Add(n1.normalized);
                normals.Add(n2.normalized);

                uvs.Add(uv);
                uvs.Add(uv);
                uvs.Add(uv);

                triangles.Add(baseIdx);
                triangles.Add(baseIdx + 1);
                triangles.Add(baseIdx + 2);
            }

            public void AddQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
                Vector3 n0, Vector3 n1, Vector3 n2, Vector3 n3, Vector2 uv)
            {
                AddTriangle(v0, v1, v2, n0, n1, n2, uv);
                AddTriangle(v0, v2, v3, n0, n2, n3, uv);
            }

            public void AddQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 normal, Vector2 uv)
            {
                AddQuad(v0, v1, v2, v3, normal, normal, normal, normal, uv);
            }

            public Mesh Build(string name)
            {
                Mesh mesh = new Mesh();
                mesh.name = name;
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                return mesh;
            }
        }

        #endregion
    }
}
