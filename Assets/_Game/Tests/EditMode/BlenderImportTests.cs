using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Worldtree.Tests
{
    /// <summary>Verifies the Blender export conventions (docs/BlenderPipeline.md) on the imported FBX files.</summary>
    public class BlenderImportTests
    {
        const string Dir = "Assets/_Game/Art/Models/";
        const float Tol = 0.01f;

        static GameObject Load(string file)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + file);
            Assert.NotNull(go, $"{Dir}{file} must import");
            return go;
        }

        static Vector3[] ModelSpaceVertices(Renderer r, Mesh mesh) => mesh.vertices.Select(v => r.transform.TransformPoint(v)).ToArray();

        static Bounds BoundsOf(Vector3[] pts)
        {
            var b = new Bounds(pts[0], Vector3.zero);
            foreach (var p in pts) b.Encapsulate(p);
            return b;
        }

        static void AssertIdentityTransforms(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>())
            {
                if (t.GetComponent<Renderer>() == null && t != root.transform && t.name != "Rig") continue; // bones are posed by design
                Assert.AreEqual(Vector3.one, t.localScale, $"{t.name}: scale");
                Assert.AreEqual(Vector3.zero, t.localPosition, $"{t.name}: position");
                // Known Blender quirk: the exporter's axis bake does not reach armature objects, so the "Rig" node arrives rotated
                // -90 degrees about X. Bones and bind poses compensate (see Humanoid_PosedInstance_*); meshes and the root stay identity.
                var expected = t.name == "Rig" ? Quaternion.Euler(-90f, 0f, 0f) : Quaternion.identity;
                Assert.Less(Quaternion.Angle(expected, t.localRotation), 0.1f, $"{t.name}: rotation (euler {t.localEulerAngles})");
            }
        }

        /// <summary>Flat-shaded blockout: each vertex normal must be unit length and equal the triangle's winding normal.</summary>
        static void AssertNormalsMatchWinding(Mesh mesh)
        {
            var n = mesh.normals;
            var v = mesh.vertices;
            Assert.AreEqual(v.Length, n.Length, "normals imported (not calculated away)");
            foreach (var normal in n) Assert.AreEqual(1f, normal.magnitude, 1e-3f, "unit normal");
            var tris = mesh.triangles;
            for (var i = 0; i < tris.Length; i += 3)
            {
                var geo = Vector3.Cross(v[tris[i + 1]] - v[tris[i]], v[tris[i + 2]] - v[tris[i]]).normalized;
                Assert.Greater(Vector3.Dot(geo, n[tris[i]]), 0.99f, $"triangle {i / 3}: vertex normal disagrees with winding (flipped or wrong handedness)");
            }
        }

        static void AssertUrpMaterial(Material m)
        {
            Assert.NotNull(m, "material slot is filled");
            Assert.IsTrue(m.shader.name.Contains("Universal Render Pipeline"), $"{m.name}: shader {m.shader.name} is not URP (would render pink)");
        }

        // ------------------------------------------------------------------ platform
        [Test]
        public void Platform_SizeOriginAndAxes()
        {
            var go = Load("SM_PlatformProxy.fbx");
            AssertIdentityTransforms(go);
            var mf = go.GetComponentInChildren<MeshFilter>();
            var b = BoundsOf(ModelSpaceVertices(mf.GetComponent<Renderer>(), mf.sharedMesh));
            Assert.AreEqual(new Vector3(4f, 0.5f, 2f), b.size, "Blender (x width, y depth, z up) must arrive as Unity (x, y up, z depth)");
            Assert.AreEqual(0f, b.max.y, Tol, "origin is the centre of the top face");
            Assert.AreEqual(0f, b.center.x, Tol);
            Assert.AreEqual(0f, b.center.z, Tol);
        }

        [Test]
        public void Platform_NormalsAreOutward()
        {
            var mf = Load("SM_PlatformProxy.fbx").GetComponentInChildren<MeshFilter>();
            var mesh = mf.sharedMesh;
            AssertNormalsMatchWinding(mesh);
            var centre = mesh.bounds.center;
            for (var i = 0; i < mesh.vertexCount; i++)
                Assert.Greater(Vector3.Dot(mesh.normals[i], mesh.vertices[i] - centre), 0f, $"vertex {i}: normal points into the box");
            Assert.AreEqual(Vector3.up, mesh.normals[mesh.triangles[mesh.GetSubMesh(1).indexStart]], "top face normal is +Y");
        }

        [Test]
        public void Platform_MaterialSlots()
        {
            var r = Load("SM_PlatformProxy.fbx").GetComponentInChildren<MeshRenderer>();
            Assert.AreEqual(new[] { "M_Graybox_Platform", "M_Graybox_PlatformTop" }, r.sharedMaterials.Select(m => m.name).ToArray());
            foreach (var m in r.sharedMaterials) AssertUrpMaterial(m);
            var mesh = r.GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(2, mesh.subMeshCount);
            Assert.AreEqual(6, mesh.GetSubMesh(1).indexCount, "top face = one quad on slot 1");
            Assert.AreEqual(30, mesh.GetSubMesh(0).indexCount, "five side/bottom quads on slot 0");
        }

        [Test]
        public void Materials_BaseColoursMatchBlender()
        {
            // Base colours authored in Blender/build_proxies.py (placeholder graybox colours).
            var expected = new System.Collections.Generic.Dictionary<string, Color>
            {
                ["M_Graybox_Body"] = new Color(0.55f, 0.58f, 0.62f),
                ["M_Graybox_Accent"] = new Color(0.90f, 0.45f, 0.10f),
                ["M_Graybox_Platform"] = new Color(0.35f, 0.40f, 0.35f),
                ["M_Graybox_PlatformTop"] = new Color(0.55f, 0.65f, 0.45f),
            };
            var materials = Load("SK_HumanoidProxy.fbx").GetComponentInChildren<Renderer>().sharedMaterials
                .Concat(Load("SM_PlatformProxy.fbx").GetComponentInChildren<Renderer>().sharedMaterials);
            foreach (var m in materials)
            {
                var c = m.GetColor("_BaseColor");
                var e = expected[m.name];
                Assert.AreEqual(e.r, c.r, 0.02f, $"{m.name} r (was {c})");
                Assert.AreEqual(e.g, c.g, 0.02f, $"{m.name} g (was {c})");
                Assert.AreEqual(e.b, c.b, 0.02f, $"{m.name} b (was {c})");
            }
        }

        // ------------------------------------------------------------------ humanoid
        static (SkinnedMeshRenderer r, Mesh m) Humanoid()
        {
            var r = Load("SK_HumanoidProxy.fbx").GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.NotNull(r, "humanoid imports as a SkinnedMeshRenderer");
            return (r, r.sharedMesh);
        }

        [Test]
        public void Humanoid_HeightFeetAndFacing()
        {
            var (r, m) = Humanoid();
            AssertIdentityTransforms(r.transform.root.gameObject);
            var pts = ModelSpaceVertices(r, m);
            var b = BoundsOf(pts);
            Assert.AreEqual(0f, b.min.y, Tol, "feet at y = 0");
            Assert.AreEqual(1.8f, b.max.y, 1.8f * 0.01f, "1.8 m tall (1 Blender unit = 1 m = 1 Unity unit)");
            Assert.AreEqual(0.59f, b.size.z, Tol, "arm span arrives on Z (depth)");
            var feet = pts.Where(p => p.y < 0.09f).ToArray();
            Assert.Greater(feet.Max(p => p.x), -feet.Min(p => p.x), "toes point to +X (character faces right)");
        }

        [Test]
        public void Humanoid_NormalsConsistent_AndHeadFacesUp()
        {
            var (_, m) = Humanoid();
            AssertNormalsMatchWinding(m);
            // Flat shading splits vertices per face, so the highest corner is shared by the top face and two side faces.
            var maxY = m.vertices.Max(p => p.y);
            var atTop = Enumerable.Range(0, m.vertexCount).Where(i => m.vertices[i].y > maxY - 1e-4f).ToArray();
            Assert.AreEqual(4, atTop.Count(i => m.normals[i] == Vector3.up), "head top face: four up-facing vertices");
            Assert.IsFalse(atTop.Any(i => m.normals[i].y < -0.5f), "nothing at the top faces down");
        }

        /// <summary>Instantiates the model and bakes the skinned pose: what the player will actually see.</summary>
        [Test]
        public void Humanoid_PosedInstance_IsUprightFacesRightAndOneAndAHalfMetresPerUnitCorrect()
        {
            var instance = (GameObject)Object.Instantiate(Load("SK_HumanoidProxy.fbx"));
            try
            {
                var r = instance.GetComponentInChildren<SkinnedMeshRenderer>();
                var baked = new Mesh();
                r.BakeMesh(baked);
                var pts = baked.vertices.Select(v => r.transform.TransformPoint(v)).ToArray();
                var b = BoundsOf(pts);
                Assert.AreEqual(0f, b.min.y, Tol, "posed feet at y = 0");
                Assert.AreEqual(1.8f, b.max.y, 0.02f, "posed height 1.8 m");
                Assert.AreEqual(0.59f, b.size.z, 0.02f, "depth on Z");
                var feet = pts.Where(p => p.y < 0.09f).ToArray();
                Assert.Greater(feet.Max(p => p.x), -feet.Min(p => p.x), "posed toes point to +X");
                Assert.AreEqual(1.56f, instance.GetComponentsInChildren<Transform>().First(t => t.name == "head").position.y, 0.02f, "head bone at 1.56 m");
                Object.DestroyImmediate(baked);
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void Humanoid_MaterialsAndBlockoutSlots()
        {
            var (r, m) = Humanoid();
            Assert.AreEqual(new[] { "M_Graybox_Body", "M_Graybox_Accent" }, r.sharedMaterials.Select(x => x.name).ToArray());
            foreach (var mat in r.sharedMaterials) AssertUrpMaterial(mat);
            Assert.AreEqual(2, m.subMeshCount);
        }

        [Test]
        public void Humanoid_RigNamesAndRoot()
        {
            var (r, _) = Humanoid();
            var expected = new[]
            {
                "root", "pelvis", "spine", "neck", "head",
                "upper_leg.L", "lower_leg.L", "foot.L", "upper_leg.R", "lower_leg.R", "foot.R",
                "upper_arm.L", "lower_arm.L", "upper_arm.R", "lower_arm.R",
            };
            CollectionAssert.AreEquivalent(expected, r.bones.Select(b => b.name).ToArray());
            Assert.AreEqual("root", r.rootBone.name);
            Assert.AreEqual(Vector3.zero, r.rootBone.position, "root bone at the feet origin");
            var byName = r.bones.ToDictionary(b => b.name);
            Assert.Greater(byName["foot.L"].position.z, 0f, "left side is the far side (+Z) when the character faces +X");
            Assert.Less(byName["foot.R"].position.z, 0f, "right side is the camera side (-Z)");
            var importer = (ModelImporter)AssetImporter.GetAtPath(Dir + "SK_HumanoidProxy.fbx");
            Assert.AreEqual(ModelImporterAnimationType.Generic, importer.animationType);
        }

        [Test]
        public void Humanoid_EveryVertexIsWeighted()
        {
            var (_, m) = Humanoid();
            var weights = m.GetBonesPerVertex();
            Assert.AreEqual(m.vertexCount, weights.Length);
            Assert.IsTrue(weights.All(c => c >= 1), "no unskinned vertex");
        }

        [Test]
        public void ImportSettings_FollowConvention()
        {
            foreach (var file in new[] { "SK_HumanoidProxy.fbx", "SM_PlatformProxy.fbx" })
            {
                var i = (ModelImporter)AssetImporter.GetAtPath(Dir + file);
                Assert.AreEqual(1f, i.globalScale, $"{file}: Scale Factor 1");
                Assert.AreEqual(ModelImporterNormals.Import, i.importNormals, $"{file}: normals come from Blender");
                Assert.IsFalse(i.importCameras || i.importLights, $"{file}: no cameras or lights");
            }
        }
    }
}
