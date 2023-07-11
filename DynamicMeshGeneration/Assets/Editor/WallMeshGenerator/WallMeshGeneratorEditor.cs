using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WallMeshGenerator))]
public class WallMeshGeneratorEditor : Editor
{
    private bool buttonPressed;
    private const float handleSize = 0.05f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        WallMeshGenerator script = (WallMeshGenerator)target;
        GUILayout.Space(20f);

        buttonPressed = GUILayout.Toggle(buttonPressed, "Edit Mesh", "Button");
        if (GUILayout.Button(script.MeshFilter.sharedMesh == null ? "Generate New Mesh" : "Update Mesh"))
        {
            script.GenerateMesh();
        }

        GUI.enabled = script.MeshFilter.sharedMesh != null;
        if (GUILayout.Button("Delete Mesh"))
        {
            if (script.MeshFilter != null && script.MeshFilter.sharedMesh != null)
            {
                // Resetting all values if Mesh gets deleted
                var transform = script.transform;
                transform.position = Vector3.zero;
                transform.rotation = Quaternion.identity;
                script.Depth = 1f;
                script.Height = 2f;
                script.WidthRight = 1f;
                script.WidthLeft = -1f;
                script.rowCount = 5;
                script.columnCount = 5;
                script.TextureOffset = Vector3.zero;
                script.TextureScale = 0.25f;
                // Deletes Mesh
                script.MeshFilter.mesh = null;
            }
        }

        GUI.enabled = true;
    }

    private void OnSceneGUI()
    {
        WallMeshGenerator script = (WallMeshGenerator)target;
        // Nothing gets created or drawn if there is no Mesh, or the Edit Mesh Button isn't pressed
        if (!script.MeshFilter.sharedMesh || !buttonPressed)
            return;

        EditorGUI.BeginChangeCheck();

        float xScale = script.Depth / 2f;
        float yScale = script.Height / 2f;
        float zScaleRight = script.WidthRight / 2f;
        float zScaleLeft = script.WidthLeft / 2f;
        var transform = script.transform;
        Vector3 position = transform.position;
        Vector3 frontPos = new Vector3(xScale, 0, 0);
        Vector3 backPos = new Vector3(-xScale, 0, 0);
        Vector3 topPos = new Vector3(0, yScale, 0);
        Vector3 bottomPos = Vector3.zero;
        Vector3 rightPos = new Vector3(0, 0, zScaleRight) + topPos / 2f;
        Vector3 leftPos = new Vector3(0, 0, zScaleLeft) + topPos / 2f;

        // Create Free Movement Handles that look like squares on the center of each face of the mesh
        var rotation = transform.rotation;
        Handles.matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
        float topHandle = Handles
            .FreeMoveHandle(topPos, Quaternion.identity, handleSize, Vector3.zero, Handles.DotHandleCap).y;
        float rightHandle = Handles
            .FreeMoveHandle(rightPos, Quaternion.identity, handleSize, Vector3.zero, Handles.DotHandleCap).z;
        float leftHandle = Handles
            .FreeMoveHandle(leftPos, Quaternion.identity, handleSize, Vector3.zero, Handles.DotHandleCap).z;

        float width = (rightPos - leftPos).magnitude;
        float height = (topPos - bottomPos).magnitude;
        float depth = (frontPos - backPos).magnitude;
        var boundsSize = new Vector3(depth, height, width);
        // TODO: Description
        Vector3 localCenter = new(0, yScale, zScaleRight + zScaleLeft);
        Vector3 rotatedWoldCenter = position + rotation * localCenter * 0.5f;

        // Draw a Wire Cube with the dimensions of the Mesh's Face-To-OppositeFace
        Handles.matrix = Matrix4x4.TRS(rotatedWoldCenter, rotation, Vector3.one);
        Handles.DrawWireCube(Vector3.zero, boundsSize);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Scaled Scale at Point");

            // If the Handles weren't scaled below or higher than 0, the adjusted mesh gets created
            if (topHandle > 0 && rightHandle > 0 && leftHandle < 0)
            {
                script.Height = topHandle * 2f;
                script.WidthRight = rightHandle * 2f;
                script.WidthLeft = leftHandle * 2f;
                script.GenerateMesh();
            }
        }
    }
}