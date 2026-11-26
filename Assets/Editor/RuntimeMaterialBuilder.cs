// Writes Resources/RuntimeLit.mat (URP Lit) for objects created at runtime (Core.RuntimeMat).
using UnityEditor;
using UnityEngine;

public static class RuntimeMaterialBuilder
{
    [MenuItem("AETHVIRSE/Build Runtime Material")]
    public static void Build()
    {
        const string path = "Assets/Resources/RuntimeLit.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = Shader.Find("Universal Render Pipeline/Lit");
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Smoothness", 0.2f);
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        Debug.Log("AETHVIRSE: runtime material built");
    }
}
