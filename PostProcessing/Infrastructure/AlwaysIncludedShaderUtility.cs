using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public static class AlwaysIncludedShaderUtility
{
    /// <summary>
    /// 將指定 shader 自動加入 ProjectSettings 的 Always Included Shaders 列表。
    /// 若已存在，則略過。
    /// </summary>
    public static void AddShaderToAlwaysIncluded(Shader shader)
    {
        if (shader == null)
        {
            Debug.LogError("AddShaderToAlwaysIncluded: shader is null.");
            return;
        }

        var graphicsSettings = GraphicsSettings.GetGraphicsSettings();
        var property = new SerializedObject(graphicsSettings)
            .FindProperty("m_AlwaysIncludedShaders");

        // 檢查是否已存在
        for (int i = 0; i < property.arraySize; i++)
        {
            var element = property.GetArrayElementAtIndex(i).objectReferenceValue;
            if (element == shader)
            {
                // Debug.Log($"Shader '{shader.name}' already in Always Included Shaders.");
                return;
            }
        }

        // 加入
        property.InsertArrayElementAtIndex(property.arraySize);
        property.GetArrayElementAtIndex(property.arraySize - 1).objectReferenceValue = shader;

        property.serializedObject.ApplyModifiedProperties();

        Debug.Log($"Shader '{shader.name}' added to Always Included Shaders.");
    }
}
