using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteAlways]
public class WallMeshGenerator : MonoBehaviour
{
    [Header("References")] public MeshFilter MeshFilter;

    [SerializeField] private Transform MeshTransform;


    [Space, Header("Mesh"), Tooltip("Up Scale")]
    public float Height = 2f;

    [Tooltip("= Front and Back Scale")] public float Depth = 1f;

    [Tooltip("Right Scale")] public float WidthRight = 1;

    [Tooltip("Left Scale")] public float WidthLeft = -1;


    [Space, Header("Texture"), SerializeField]
    public Vector2 TextureOffset = Vector3.zero;

    [SerializeField] public float TextureScale = 0.25f;


    [HideInInspector] public int RowCount = 1;

    [HideInInspector] public int ColumnCount = 1;

    // One segment is this long on texture scale 1 (0.84375f)
    [HideInInspector] public float WallSegmentHeight = 1f;


    private Mesh mesh;

    private bool wallDirty;

    private int textureColumnCount = 4;

    private int textureRowCount = 3;


    private void OnValidate()
    {
        if (Depth < 0.1f) Depth = 0.1f;
        if (Height < 0.1f) Height = 0.1f;
        else if (Height > 2f) Height = 2f;
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
        Vector3[] cubeVertices = CreateCubeVertices();
        Vector3[] subdividedCubeVertices = SubdivideCubeFrontAndBack(cubeVertices);
        RotateVertices(subdividedCubeVertices);
        return subdividedCubeVertices;
    }

    private Vector3[] CreateCubeVertices()
    {
        // Coordinates are moved on y-axis, so pivot is at the bottom
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

        return outerCubeVertices;
    }

    private Vector3[] SubdivideCubeFrontAndBack(Vector3[] _cubeVertices)
    {
        // Add Vertices to subdivide the front and back face of the mesh for vertex painting
        List<Vector3> vertices = new List<Vector3>(_cubeVertices);
        
        SubdivideCubeFront(vertices);
        SubdivideCubeBack(vertices);

        Vector3[] verticesArray = vertices.ToArray();
        return verticesArray;
    }

    private void SubdivideCubeFront(List<Vector3> _vertices)
    {
        float additiveRightExtension = (int)WidthRight % 2 == 0 ? WidthRight - (int)WidthRight : WidthRight - (int)WidthRight + 1;
        float additiveLeftExtension = (int)WidthLeft % 2 == 0 ? Mathf.Abs(WidthLeft) - Mathf.Abs((int)WidthLeft) : Mathf.Abs(WidthLeft - (int)WidthLeft) + 1;
        int rightSegments = WidthRight / 2 >= 1 ? (int)WidthRight / 2 : 0;
        int leftSegments = Mathf.Abs(WidthLeft) / 2 >= 1 ? (int)Mathf.Abs(WidthLeft) / 2 : 0;
        int rightAdditiveSegment = additiveRightExtension > 0 ? 1 : 0;
        int leftAdditiveSegment = additiveLeftExtension > 0 ? 1 : 0;
        ColumnCount = rightAdditiveSegment + rightSegments + leftSegments + leftAdditiveSegment;
        float height = (_vertices[5] - _vertices[0]).magnitude;
        float quadHeight = height / RowCount;
        float prevUVLength;

        if (additiveRightExtension > 0)
        {
            _vertices.Add(new Vector3(Depth, 0, WidthRight));
            _vertices.Add(new Vector3(Depth, 0, WidthRight - additiveRightExtension));
            _vertices.Add(new Vector3(Depth, quadHeight, WidthRight));
            _vertices.Add(new Vector3(Depth, quadHeight, WidthRight - additiveRightExtension));
        }

        prevUVLength = WidthRight - additiveRightExtension;

        int allExtensions = rightSegments + leftSegments;
        for (int i = 0; i < allExtensions; i++)
        {
            _vertices.Add(new Vector3(Depth, 0, prevUVLength));
            _vertices.Add(new Vector3(Depth, 0, prevUVLength - 2));
            _vertices.Add(new Vector3(Depth, quadHeight, prevUVLength));
            _vertices.Add(new Vector3(Depth, quadHeight, prevUVLength - 2));
            prevUVLength -= 2;
        }

        if (additiveLeftExtension > 0)
        {
            _vertices.Add(new Vector3(Depth, 0, prevUVLength));
            _vertices.Add(new Vector3(Depth, 0, prevUVLength - additiveLeftExtension));
            _vertices.Add(new Vector3(Depth, quadHeight, prevUVLength));
            _vertices.Add(new Vector3(Depth, quadHeight, prevUVLength - additiveLeftExtension));
            prevUVLength -= additiveLeftExtension;
        }
    }
    
     private void SubdivideCubeBack(List<Vector3> _vertices)
    {
        float additiveRightExtension = (int)WidthLeft % 2 == 0 ? Mathf.Abs(WidthLeft) - Mathf.Abs((int)WidthLeft) : Mathf.Abs(WidthLeft - (int)WidthLeft) + 1;
        float additiveLeftExtension = (int)WidthRight % 2 == 0 ? WidthRight - (int)WidthRight : WidthRight - (int)WidthRight + 1;
        int rightSegments = Mathf.Abs(WidthLeft) / 2 >= 1 ? (int)Mathf.Abs(WidthLeft) / 2 : 0;
        int leftSegments = WidthRight / 2 >= 1 ? (int)WidthRight / 2 : 0;
        int rightAdditiveSegment = additiveRightExtension > 0 ? 1 : 0;
        int leftAdditiveSegment = additiveLeftExtension > 0 ? 1 : 0;
        ColumnCount = rightAdditiveSegment + rightSegments + leftSegments + leftAdditiveSegment;
        float height = (_vertices[5] - _vertices[0]).magnitude;
        float quadHeight = height / RowCount;
        float prevUVLength;

        if (additiveRightExtension > 0)
        {
            _vertices.Add(new Vector3(-Depth, 0, WidthLeft));
            _vertices.Add(new Vector3(-Depth, 0, WidthLeft + additiveRightExtension));
            _vertices.Add(new Vector3(-Depth, quadHeight, WidthLeft));
            _vertices.Add(new Vector3(-Depth, quadHeight, WidthLeft + additiveRightExtension));
        }

        prevUVLength = WidthLeft + additiveRightExtension;

        int allExtensions = rightSegments + leftSegments;
        for (int i = 0; i < allExtensions; i++)
        {
            _vertices.Add(new Vector3(-Depth, 0, prevUVLength));
            _vertices.Add(new Vector3(-Depth, 0, prevUVLength + 2));
            _vertices.Add(new Vector3(-Depth, quadHeight, prevUVLength));
            _vertices.Add(new Vector3(-Depth, quadHeight, prevUVLength + 2));
            prevUVLength += 2;
        }

        if (additiveLeftExtension > 0)
        {
            _vertices.Add(new Vector3(-Depth, 0, prevUVLength));
            _vertices.Add(new Vector3(-Depth, 0, prevUVLength + additiveLeftExtension));
            _vertices.Add(new Vector3(-Depth, quadHeight, prevUVLength));
            _vertices.Add(new Vector3(-Depth, quadHeight, prevUVLength + additiveLeftExtension));
            prevUVLength += additiveLeftExtension;
        }
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
        // "missing" because the top quad was already created
        const int missingQuadWallSides = 3;
        // the front and back face of the cube contain more triangles because of the subdivision for vertex paint
        const int subdividedWallSides = 2;
        for (int i = 0; i < RowCount * ColumnCount * subdividedWallSides + missingQuadWallSides; i++)
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
        // float widthLeft = TextureOffset.x + (WidthLeft / 2 + 0.5f) * TextureScale;
        // float widthRight = TextureOffset.x + (WidthRight / 2 + 0.5f) * TextureScale;
        float height = TextureOffset.y + Height / 2 * TextureScale;
        float depthX = TextureOffset.x + Depth * TextureScale;
        float depthXLeft = TextureOffset.x + (1 - Depth) * TextureScale;
        // float depthY = TextureOffset.y + Depth * TextureScale;


        Vector2[] outerCubeUV =
        {
            // top
            new(TextureScale, TextureOffset.y),
            new(TextureOffset.x, TextureOffset.y),
            new(TextureScale, TextureScale),
            new(TextureOffset.x, TextureScale),
            // bottom
            new(TextureOffset.x, TextureOffset.y),
            new(TextureScale, TextureOffset.y),
            new(TextureOffset.x, TextureScale),
            new(TextureScale, TextureScale),
            // right
            new(TextureOffset.x, TextureOffset.y),
            new(TextureOffset.x, height),
            new(depthX, TextureOffset.y),
            new(depthX, height),
            // left
            new(TextureScale + TextureOffset.x, height),
            new(TextureScale + TextureOffset.x, TextureOffset.y),
            new(depthXLeft, height),
            new(depthXLeft, TextureOffset.y),
        };

        List<Vector2> uv = new List<Vector2>(outerCubeUV);
        // widthLeft = TextureOffset.x + (WidthLeft / 2 - 0.5f) * TextureScale;
        // float quadWidth = (vertices[1] - vertices[0]).magnitude / ColumnCount;
        // float quadHeight = (vertices[5] - vertices[0]).magnitude / RowCount;

        // front face
        for (int j = 0; j < RowCount; j++)
        {
            for (int k = 0; k < ColumnCount; k++)
            {
                uv.Add(new Vector2(0, 0));
                uv.Add(new Vector2(TextureScale, 0));
                uv.Add(new Vector2(0, TextureScale));
                uv.Add(new Vector2(TextureScale, TextureScale));
            }
        }

        // back face
        for (int j = 0; j < RowCount; j++)
        {
            for (int k = 0; k < ColumnCount; k++)
            {
                uv.Add(new Vector2(0, 0));
                uv.Add(new Vector2(TextureScale, 0));
                uv.Add(new Vector2(0, TextureScale));
                uv.Add(new Vector2(TextureScale, TextureScale));
            }
        }

        // // front face
        // for (int j = 0; j < RowCount; j++)
        // {
        //     for (int k = 0; k < ColumnCount; k++)
        //     {
        //         uv.Add(new Vector2(widthRight - quadWidth * k * TextureScale,
        //             TextureOffset.y + quadHeight * j * TextureScale));
        //         uv.Add(new Vector2(widthRight - quadWidth * (k + 1) * TextureScale,
        //             TextureOffset.y + quadHeight * j * TextureScale));
        //         uv.Add(new Vector2(widthRight - quadWidth * k * TextureScale,
        //             TextureOffset.y + quadHeight * (j + 1) * TextureScale));
        //         uv.Add(new Vector2(widthRight - quadWidth * (k + 1) * TextureScale,
        //             TextureOffset.y + quadHeight * (j + 1) * TextureScale));
        //     }
        // }
        //
        // // back face
        // for (int j = 0; j < RowCount; j++)
        // {
        //     for (int k = 0; k < ColumnCount; k++)
        //     {
        //         uv.Add(new Vector2(Mathf.Abs(widthLeft) - quadWidth * k * TextureScale,
        //             TextureOffset.y + quadHeight * j * TextureScale));
        //         uv.Add(new Vector2(Mathf.Abs(widthLeft) - quadWidth * (k + 1) * TextureScale,
        //             TextureOffset.y + quadHeight * j * TextureScale));
        //         uv.Add(new Vector2(Mathf.Abs(widthLeft) - quadWidth * k * TextureScale,
        //             TextureOffset.y + quadHeight * (j + 1) * TextureScale));
        //         uv.Add(new Vector2(Mathf.Abs(widthLeft) - quadWidth * (k + 1) * TextureScale,
        //             TextureOffset.y + quadHeight * (j + 1) * TextureScale));
        //     }
        // }

        Vector2[] uvArray = uv.ToArray();
        RandomizeTextureVariants(uvArray);

        return uvArray;
    }

    private void RandomizeTextureVariants(Vector2[] _uv)
    {
        for (int i = 0; i < _uv.Length; i++)
        {
            // Zoom in on bottom left variant
            _uv[i].x /= textureColumnCount;
            _uv[i].y /= textureRowCount;
        }

        const int verticesPerQuad = 4;
        const int verticesTopAndBot = verticesPerQuad * 2;

        for (int i = 0; i < verticesTopAndBot; i++)
        {
            _uv[i].x += (1f / (float)textureColumnCount) * (textureColumnCount - 1);
            _uv[i].y += (1f / (float)textureRowCount) * (textureRowCount);
        }

        for (int i = 8; i < _uv.Length; i += verticesPerQuad)
        {
            int randomX;
            int randomY;
            PreventFirstAndLastVariant(out randomX, out randomY);

            for (int j = 0; j < verticesPerQuad; j++)
            {
                _uv[i + j].x += (1f / (float)textureColumnCount) * randomX;
                _uv[i + j].y += (1f / (float)textureRowCount) * randomY;
            }
        }
    }

    private void PreventFirstAndLastVariant(out int _randomX, out int _randomY)
    {
        // Randomize
        _randomX = Random.Range(0, textureColumnCount);
        _randomY = Random.Range(0, textureRowCount);

        // Prevent the first and last texture variant (pillar and blank)
        bool checkFirst = CheckFirstVariant(_randomX, _randomY);
        bool checkLast = CheckLastVariant(_randomX, _randomY);


        while (checkFirst || checkLast)
        {
            _randomX = Random.Range(0, textureColumnCount);
            checkFirst = CheckFirstVariant(_randomX, _randomY);
            checkLast = CheckLastVariant(_randomX, _randomY);
        }
    }

    private bool CheckFirstVariant(int _randomX, int _randomY)
    {
        return _randomX == 0 && _randomY == textureRowCount - 1;
    }

    private bool CheckLastVariant(int _randomX, int _randomY)
    {
        return _randomX == textureColumnCount - 1 && _randomY == 0;
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
        for (int i = 0; i < RowCount; i++)
        {
            for (int j = 0; j < ColumnCount; j++)
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