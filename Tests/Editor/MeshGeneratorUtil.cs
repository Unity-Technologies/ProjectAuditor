using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.EditorTests
{
    internal static class MeshGeneratorUtil
    {
        public static Mesh CreateTestMesh(string name, int triangleCount, bool markNoLongerReadable = false)
        {
            var vertices = new Vector3[triangleCount * 3];
            var triangles = new int[triangleCount * 3];

            CreateGeometry(vertices, triangles);

            var mesh = new Mesh();
            mesh.name = name;

            mesh.SetVertices(vertices);
            mesh.SetIndexBufferParams(triangles.Length, IndexFormat.UInt32);
            mesh.SetTriangles(triangles, 0);
            mesh.UploadMeshData(markNoLongerReadable);

            return mesh;
        }

        static void CreateGeometry(Vector3[] vertices, int[] triangles)
        {
            Assert.IsTrue(vertices.Length > 0);
            Assert.IsTrue(vertices.Length == triangles.Length);

            float scale = 0.001f;

            for (int i = 0; i < vertices.Length; i += 3)
            {
                vertices[i + 0] = Vector3.forward * i * scale;
                vertices[i + 1] = Vector3.forward * (i + 1) * scale;
                vertices[i + 2] = Vector3.forward * (i + 1) * scale + Vector3.up;

                triangles[i + 0] = i;
                triangles[i + 1] = i + 1;
                triangles[i + 2] = i + 2;
            }
        }
    }
}
