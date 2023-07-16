using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteAlways]
public class WallMeshGenerator : MonoBehaviour
{
    [Header("References")] public MeshFilter MeshFilter;

    [SerializeField] private Transform MeshTransform;


    [Space, Header("Mesh"), Tooltip("Up Scale"), HideInInspector]
    public float Height = 2f;

    [Tooltip("= Front and Back Scale"), HideInInspector]
    public float Depth = 1f;

    [Tooltip("Right Scale")] public float WidthRight = 1;

    [Tooltip("Left Scale")] public float WidthLeft = -1;


    [Space, Header("Texture"), HideInInspector]
    public Vector2 TextureOffset = Vector3.zero;


    [HideInInspector] public float TextureScale = 0.25f;

    [HideInInspector] public int RowCount = 1;

    [HideInInspector] public int columnCount = 2;

    // One segment is this long on texture scale 1 (0.84375f)
    [HideInInspector] public float WallSegmentHeight = 1f;


    private Mesh mesh;

    private bool isWallDirty;

    private int textureColumnCount = 4;

    private int textureRowCount = 3;

    private List<int> textureVariantsRight;

    private List<int> textureVariantsLeft;

    private int textureVariantSide;

    const int verticesPerQuad = 4;

    private void OnValidate()
    {
        if (WidthRight < 0.1f) WidthRight = 0.1f;
        if (WidthLeft > -0.1f) WidthLeft = -0.1f;
        isWallDirty = true;
    }

    private void Update()
    {
        if (isWallDirty)
        {
            isWallDirty = false;
            GenerateMesh();
        }
    }

    public void UpdateRandomTextureVariants()
    {
        if (textureVariantsRight == null || textureVariantsLeft == null)
            return;

        for (int i = 0; i < textureVariantsRight.Count; i++)
        {
            textureVariantsRight[i] = FindRandomTextureVariant();
        }

        for (int i = 0; i < textureVariantsLeft.Count; i++)
        {
            textureVariantsLeft[i] = FindRandomTextureVariant();
        }

        textureVariantSide = FindRandomTextureVariant();
    }

    public void GenerateMesh()
    {
        Setup();

        Vector3[] vertices = CreateVertices();
        int[] triangles = CreateTriangles();
        Vector2[] uv = CreateUV(vertices);
        Vector3[] normals = CreateNormals();
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
        float additiveRightExtension =
            (int)WidthRight % 2 == 0 ? WidthRight - (int)WidthRight : WidthRight - (int)WidthRight + 1;
        float additiveLeftExtension = (int)WidthLeft % 2 == 0
            ? Mathf.Abs(WidthLeft) - Mathf.Abs((int)WidthLeft)
            : Mathf.Abs(WidthLeft - (int)WidthLeft) + 1;
        int rightSegments = WidthRight / 2 >= 1 ? (int)WidthRight / 2 : 0;
        int leftSegments = Mathf.Abs(WidthLeft) / 2 >= 1 ? (int)Mathf.Abs(WidthLeft) / 2 : 0;
        int rightAdditiveSegment = additiveRightExtension > 0 ? 1 : 0;
        int leftAdditiveSegment = additiveLeftExtension > 0 ? 1 : 0;
        int prevColumnCount = columnCount;
        columnCount = rightAdditiveSegment + rightSegments + leftSegments + leftAdditiveSegment;
        float height = (_vertices[5] - _vertices[0]).magnitude;
        float quadHeight = height / RowCount;
        float prevUVLength;

        CalculateNewTextureVariants(prevColumnCount, rightSegments, rightAdditiveSegment, leftSegments,
            leftAdditiveSegment);


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
        float additiveRightExtension = (int)WidthLeft % 2 == 0
            ? Mathf.Abs(WidthLeft) - Mathf.Abs((int)WidthLeft)
            : Mathf.Abs(WidthLeft - (int)WidthLeft) + 1;
        float additiveLeftExtension =
            (int)WidthRight % 2 == 0 ? WidthRight - (int)WidthRight : WidthRight - (int)WidthRight + 1;
        int rightSegments = Mathf.Abs(WidthLeft) / 2 >= 1 ? (int)Mathf.Abs(WidthLeft) / 2 : 0;
        int leftSegments = WidthRight / 2 >= 1 ? (int)WidthRight / 2 : 0;
        int rightAdditiveSegment = additiveRightExtension > 0 ? 1 : 0;
        int leftAdditiveSegment = additiveLeftExtension > 0 ? 1 : 0;
        columnCount = rightAdditiveSegment + rightSegments + leftSegments + leftAdditiveSegment;
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

    public void ResetTextureVariants()
    {
        textureVariantsRight = null;
        textureVariantsLeft = null;
    }

    private void CalculateNewTextureVariants(int _prevColumnCount, int _rightSegments, int _rightAdditiveSegment,
        int _leftSegments, int _leftAdditiveSegment)
    {
        if (textureVariantsRight == null || textureVariantsLeft == null)
        {
            textureVariantsRight = new();
            textureVariantsLeft = new();
            textureVariantsRight.Add(FindRandomTextureVariant());
            textureVariantsLeft.Add(FindRandomTextureVariant());
            textureVariantSide = FindRandomTextureVariant();
        }
        else if (_prevColumnCount != columnCount)
        {
            if (_prevColumnCount < columnCount)
            {
                for (int i = 0; i < columnCount - _prevColumnCount; i++)
                {
                    if (_rightSegments + _rightAdditiveSegment > textureVariantsRight.Count)
                    {
                        textureVariantsRight.Insert(0, FindRandomTextureVariant());
                    }
                    else if (_leftSegments + _leftAdditiveSegment > textureVariantsLeft.Count)
                    {
                        textureVariantsLeft.Add(FindRandomTextureVariant());
                    }
                }
            }
            else
            {
                for (int i = 0; i < _prevColumnCount - columnCount; i++)
                {
                    if (_rightSegments + _rightAdditiveSegment < textureVariantsRight.Count)
                    {
                        textureVariantsRight.RemoveAt(0);
                    }
                    else if (_leftSegments + _leftAdditiveSegment < textureVariantsLeft.Count)
                    {
                        textureVariantsLeft.RemoveAt(textureVariantsLeft.Count - 1);
                    }
                }
            }
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
        for (int i = 0; i < RowCount * columnCount * subdividedWallSides + missingQuadWallSides; i++)
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
        float height = TextureOffset.y + Height / 2 * TextureScale;
        float depthX = TextureOffset.x + Depth * TextureScale;
        float depthXLeft = TextureOffset.x + (1 - Depth) * TextureScale;

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

        // front face
        for (int k = 0; k < columnCount; k++)
        {
            float distToNextVert;

            if (k < columnCount / 2)
            {
                distToNextVert = (vertices[outerCubeUV.Length + k * 4 + 1] - vertices[outerCubeUV.Length + k * 4])
                    .magnitude;
                uv.Add(new Vector2(distToNextVert, 0));
                uv.Add(new Vector2(0, 0));
                uv.Add(new Vector2(distToNextVert, TextureScale));
                uv.Add(new Vector2(0, TextureScale));
            }
            else
            {
                distToNextVert = 1 - (vertices[outerCubeUV.Length + k * 4 + 1] - vertices[outerCubeUV.Length + k * 4])
                    .magnitude;
                uv.Add(new Vector2(1, 0));
                uv.Add(new Vector2(distToNextVert, 0));
                uv.Add(new Vector2(1, TextureScale));
                uv.Add(new Vector2(distToNextVert, TextureScale));
            }
        }

        // back face
        for (int k = 0; k < columnCount; k++)
        {
            float distToNextVert;

            if (k < columnCount / 2)
            {
                distToNextVert = (vertices[outerCubeUV.Length + columnCount * 4 + k * 4 + 1] -
                                  vertices[outerCubeUV.Length + columnCount * 4 + k * 4]).magnitude;
                uv.Add(new Vector2(distToNextVert, 0));
                uv.Add(new Vector2(0, 0));
                uv.Add(new Vector2(distToNextVert, TextureScale));
                uv.Add(new Vector2(0, TextureScale));
            }
            else
            {
                distToNextVert = 1 - (vertices[outerCubeUV.Length + columnCount * 4 + k * 4 + 1] -
                                      vertices[outerCubeUV.Length + columnCount * 4 + k * 4]).magnitude;
                uv.Add(new Vector2(1, 0));
                uv.Add(new Vector2(distToNextVert, 0));
                uv.Add(new Vector2(1, TextureScale));
                uv.Add(new Vector2(distToNextVert, TextureScale));
            }
        }

        Vector2[] uvArray = uv.ToArray();
        ZoomUVIn(uvArray);
        SetTextureVariants(uvArray);

        return uvArray;
    }

    private void ZoomUVIn(Vector2[] _uv)
    {
        for (int i = 0; i < _uv.Length; i++)
        {
            _uv[i].x /= textureColumnCount;
            _uv[i].y /= textureRowCount;
        }
    }

    private void SetTextureVariants(Vector2[] _uv)
    {
        const int verticesTopAndBot = verticesPerQuad * 2;
        const int verticesFrontStartIndex = verticesTopAndBot * 2;

        // Set top and bottom face Last Texture Variant = Blank / Black
        for (int i = 0; i < verticesTopAndBot; i++)
        {
            _uv[i].x += (1f / (float)textureColumnCount) * (textureColumnCount - 1);
            _uv[i].y += (1f / (float)textureRowCount) * (textureRowCount - 1);
        }

        // Set right and left face Random Texture Variant
        for (int i = verticesTopAndBot; i < verticesTopAndBot * 2; i++)
        {
            _uv[i].x += (1f / (float)textureColumnCount) * GetColumnFromIndex(textureVariantSide);
            _uv[i].y += (1f / (float)textureRowCount) * GetRowFromIndex(textureVariantSide);
        }

        int backVerticesCount =
            textureVariantsRight.Count * verticesPerQuad + textureVariantsLeft.Count * verticesPerQuad;

        for (int i = verticesFrontStartIndex; i < _uv.Length - backVerticesCount; i += verticesPerQuad)
        {
            int randomVariantIndex;

            int currentSegmentIndex = (i - verticesFrontStartIndex) / verticesPerQuad;
            if (currentSegmentIndex < textureVariantsRight.Count)
            {
                randomVariantIndex = textureVariantsRight[currentSegmentIndex];
            }
            else
            {
                randomVariantIndex = textureVariantsLeft[currentSegmentIndex - textureVariantsRight.Count];
            }

            for (int j = 0; j < verticesPerQuad; j++)
            {
                _uv[i + j].x += (1f / (float)textureColumnCount) * GetColumnFromIndex(randomVariantIndex);
                _uv[i + j].y += (1f / (float)textureRowCount) * GetRowFromIndex(randomVariantIndex);
            }
        }

        for (int i = verticesFrontStartIndex + backVerticesCount; i < _uv.Length; i += verticesPerQuad)
        {
            int randomVariantIndex;

            int currentSegmentIndex = (i - (verticesTopAndBot * 2 + backVerticesCount)) / verticesPerQuad;
            if (currentSegmentIndex < textureVariantsLeft.Count)
            {
                randomVariantIndex = textureVariantsLeft[^(currentSegmentIndex + 1)];
            }
            else
            {
                randomVariantIndex = textureVariantsRight[^(currentSegmentIndex - textureVariantsLeft.Count + 1)];
            }

            for (int j = 0; j < verticesPerQuad; j++)
            {
                _uv[i + j].x += (1f / (float)textureColumnCount) * GetColumnFromIndex(randomVariantIndex);
                _uv[i + j].y += (1f / (float)textureRowCount) * GetRowFromIndex(randomVariantIndex);
            }
        }
    }

    private static int GetColumnFromIndex(int _index)
    {
        return _index % 4;
    }

    private static int GetRowFromIndex(int _index)
    {
        return _index / 4;
    }

    private int FindRandomTextureVariant()
    {
        int randomIndex = Random.Range(0, textureColumnCount * textureRowCount - 2);

        return randomIndex;
    }

    private Vector3[] CreateNormals()
    {
        Vector3 topNormal = MeshTransform.up;
        Vector3 bottomNormal = -MeshTransform.up;
        Vector3 rightNormal = MeshTransform.forward;
        Vector3 leftNormal = -MeshTransform.forward;
        Vector3 frontNormal = MeshTransform.right;
        Vector3 backNormal = -MeshTransform.right;

        Vector3[] outerCubeNormals = { topNormal, bottomNormal, rightNormal, leftNormal };

        var normals = new List<Vector3>(outerCubeNormals);
        var normalsBack = new List<Vector3>();
        for (int j = 0; j < columnCount; j++)
        {
            normals.Add(frontNormal);
            normalsBack.Add(backNormal);
        }

        normals.AddRange(normalsBack);

        List<Vector3> normalsMultiplied = new List<Vector3>();
        foreach (Vector3 normal in normals)
        {
            for (int j = 0; j < verticesPerQuad; j++)
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