using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class WallMeshGenerator : MonoBehaviour
{
    [Header("References")] public MeshFilter MeshFilter;

    [SerializeField] private Transform MeshTransform;

    [Space, Header("Mesh")] [Tooltip("= Front and Back Scale")]
    public float Depth = 0.5f;

    [Tooltip("Up Scale")] public float Height = 8f;

    [Tooltip("Right Scale")] public float WidthRight = 3;

    [Tooltip("Left Scale")] public float WidthLeft = -3;

    [Space, Header("Front and Back Face Subdivision")]
    public int rowCount = 5;

    public int columnCount = 4;

    [Space, Header("Texture")] [SerializeField]
    public Vector2 TextureOffset = Vector3.zero;

    [SerializeField] public float TextureScale = 0.25f;

    private Mesh mesh;
    private bool wallDirty;

    private void OnValidate()
    {
        if (Depth < 0.1f) Depth = 0.1f;
        if (Height < 0.1f) Height = 0.1f;
        if (WidthRight < 0.1f) WidthRight = 0.1f;
        if (WidthLeft > -0.1f) WidthLeft = -0.1f;
        wallDirty = true;
    }

    private void Update()
    {
        if (wallDirty)
        {
            wallDirty = false;
            GenerateMesh();
        }
    }

    public void GenerateMesh()
    {
        Setup();

        Vector3[] vertices = CreateVertices();
        int[] triangles = CreateTriangles();
        Vector2[] uv = CreateUV(vertices);
        Vector3[] normals = CreateNormals(vertices);
        UpdateMesh(vertices, triangles, uv, normals);
    }

    private void Setup()
    {
        mesh = new Mesh { name = "Wall" };
        MeshFilter.mesh = mesh;
        MeshTransform.localPosition = Vector3.zero;
        MeshTransform.rotation = Quaternion.identity;
        MeshTransform.localScale = Vector3.one;
    }

    private Vector3[] CreateVertices()
    {
        // Moved on y-axis, so pivot is at the bottom
        Vector3[] outerCubeVertices =
        {
            // top
            new(Depth, Height, WidthRight),
            new(Depth, Height, WidthLeft),
            new(-Depth, Height, WidthRight),
            new(-Depth, Height, WidthLeft),

            // bottom
            new(Depth, 0, WidthLeft),
            new(Depth, 0, WidthRight),
            new(-Depth, 0, WidthLeft),
            new(-Depth, 0, WidthRight),

            // right
            new(Depth, 0, WidthRight),
            new(Depth, Height, WidthRight),
            new(-Depth, 0, WidthRight),
            new(-Depth, Height, WidthRight),

            // left
            new(Depth, Height, WidthLeft),
            new(Depth, 0, WidthLeft),
            new(-Depth, Height, WidthLeft),
            new(-Depth, 0, WidthLeft)
        };

        List<Vector3> vertices = new List<Vector3>(outerCubeVertices);
        float width = (vertices[1] - vertices[0]).magnitude;
        float height = (vertices[5] - vertices[0]).magnitude;

        float quadWidth = width / columnCount;
        float quadHeight = height / rowCount;

        for (int i = 0; i < rowCount; i++)
        {
            for (int j = 0; j < columnCount; j++)
            {
                vertices.Add(new Vector3(Depth, quadHeight * i, WidthRight - quadWidth * j));
                vertices.Add(new Vector3(Depth, quadHeight * i, WidthRight - quadWidth * (j + 1)));
                vertices.Add(new Vector3(Depth, quadHeight * (i + 1), WidthRight - quadWidth * j));
                vertices.Add(new Vector3(Depth, quadHeight * (i + 1), WidthRight - quadWidth * (j + 1)));
            }
        }

        for (int i = 0; i < rowCount; i++)
        {
            for (int j = 0; j < columnCount; j++)
            {
                vertices.Add(new Vector3(-Depth, quadHeight * i, WidthLeft + quadWidth * j));
                vertices.Add(new Vector3(-Depth, quadHeight * i, WidthLeft + quadWidth * (j + 1)));
                vertices.Add(new Vector3(-Depth, quadHeight * (i + 1), WidthLeft + quadWidth * j));
                vertices.Add(new Vector3(-Depth, quadHeight * (i + 1), WidthLeft + quadWidth * (j + 1)));
            }
        }

        Vector3[] verticesArray = vertices.ToArray();
        RotateVertices(verticesArray);
        return verticesArray;
    }

    private void RotateVertices(Vector3[] vertices)
    {
        Vector3 rotationPoint = MeshFilter.sharedMesh.bounds.center;
        Quaternion rotation = this.transform.rotation;
        for (int i = 0; i < vertices.Length; i++)
        {
            // Take half, so the dimensions are displayed correctly
            vertices[i] /= 2;
            // Translate the vertex to the pivot point
            Vector3 vertex = vertices[i] - rotationPoint;
            // Rotate the vertex using the rotation quaternion
            vertex = rotation * vertex;
            // Translate the vertex back to its original position
            vertices[i] = vertex + rotationPoint;
        }
    }

    private int[] CreateTriangles()
    {
        int[] firstQuad =
        {
            // top
            0, 1, 2,
            1, 3, 2
        };

        List<int> triangles = new List<int>(firstQuad);
        int missingWallSides = 3;
        int sideCount = 2;
        for (int i = 0; i < rowCount * columnCount * sideCount + missingWallSides; i++)
        {
            for (int j = 0; j < firstQuad.Length; j++)
            {
                triangles.Add(triangles[j] + (i + 1) * 4);
            }
        }

        return triangles.ToArray();
    }

    private Vector2[] CreateUV(Vector3[] vertices)
    {
        float width = TextureOffset.x + (vertices[1] - vertices[0]).magnitude * TextureScale;
        float height = TextureOffset.y + (vertices[5] - vertices[0]).magnitude * TextureScale;
        float depthX = TextureOffset.x + (vertices[2] - vertices[0]).magnitude * TextureScale;
        float depthY = TextureOffset.y + (vertices[2] - vertices[0]).magnitude * TextureScale;

        Vector2[] outerCubeUV =
        {
            // top
            new(width, TextureOffset.y),
            new(TextureOffset.x, TextureOffset.y),
            new(width, depthY),
            new(TextureOffset.x, depthY),
            // bottom
            new(width, TextureOffset.y),
            new(TextureOffset.x, TextureOffset.y),
            new(width, depthY),
            new(TextureOffset.x, depthY),
            // right
            new(TextureOffset.x, TextureOffset.y),
            new(TextureOffset.x, height),
            new(depthX, TextureOffset.y),
            new(depthX, height),
            // left
            new(depthX, height),
            new(depthX, TextureOffset.y),
            new(TextureOffset.x, height),
            new(TextureOffset.x, TextureOffset.y),
        };

        List<Vector2> uv = new List<Vector2>(outerCubeUV);

        float quadWidth = (vertices[1] - vertices[0]).magnitude / columnCount;
        float quadHeight = (vertices[5] - vertices[0]).magnitude / rowCount;

        int sideCount = 2;
        for (int i = 0; i < sideCount; i++)
        {
            for (int j = 0; j < rowCount; j++)
            {
                for (int k = 0; k < columnCount; k++)
                {
                    uv.Add(new Vector2(TextureOffset.x + (1f - quadWidth * k) * TextureScale,
                        TextureOffset.y + quadHeight * j * TextureScale));
                    uv.Add(new Vector2(TextureOffset.x + (1f - quadWidth * (k + 1)) * TextureScale,
                        TextureOffset.y + quadHeight * j * TextureScale));
                    uv.Add(new Vector2(TextureOffset.x + (1f - quadWidth * k) * TextureScale,
                        TextureOffset.y + quadHeight * (j + 1) * TextureScale));
                    uv.Add(new Vector2(TextureOffset.x + (1f - quadWidth * (k + 1)) * TextureScale,
                        TextureOffset.y + quadHeight * (j + 1) * TextureScale));
                }
            }
        }

        return uv.ToArray();
    }

    private Vector3[] CreateNormals(Vector3[] vertices)
    {
        Vector3 topNormal = vertices[0] - vertices[5];
        Vector3 bottomNormal = vertices[5] - vertices[0];
        Vector3 rightNormal = vertices[0] - vertices[1];
        Vector3 leftNormal = vertices[1] - vertices[0];
        Vector3 frontNormal = vertices[0] - vertices[2];
        Vector3 backNormal = vertices[2] - vertices[0];
        Vector3[] outerCubeNormals = { topNormal, bottomNormal, rightNormal, leftNormal };

        var normals = new List<Vector3>(outerCubeNormals);
        var normalsBack = new List<Vector3>();
        for (int i = 0; i < rowCount; i++)
        {
            for (int j = 0; j < columnCount; j++)
            {
                normals.Add(frontNormal);
                normalsBack.Add(backNormal);
            }
        }

        normals.AddRange(normalsBack);

        List<Vector3> normalsMultiplied = new List<Vector3>();
        int quadVerticesCount = 4;
        foreach (Vector3 normal in normals)
        {
            for (int j = 0; j < quadVerticesCount; j++)
            {
                normalsMultiplied.Add(normal);
            }
        }

        return normalsMultiplied.ToArray();
    }

    private void UpdateMesh(Vector3[] vertices, int[] triangles, Vector2[] uv, Vector3[] normals)
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.normals = normals;
    }
}