using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public class AutoBindEditor
{
    private static bool IsCanModify;

    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
    }

    [MenuItem("GameObject/**添加绑定组件**", false, 0)]
    private static void AutoBind(MenuCommand menuCommand)
    {
        var dirty = false;
        foreach (var go in Selection.gameObjects)
        {
            if (!go.name.StartsWith("@"))
            {
                go.name = $"@{go.name}";
                dirty = true;
            }
        }

        if (dirty)
            EditorUtility.SetDirty(Selection.gameObjects[0]);
    }

    [MenuItem("GameObject/**取消绑定组件**", false, 1)]
    private static void RemoveAutoBind(MenuCommand menuCommand)
    {
        var dirty = false;
        foreach (var go in Selection.gameObjects)
        {
            if (go.name.StartsWith("@"))
            {
                go.name = go.name.Substring(1, go.name.Length - 1);
                dirty = true;
            }
        }

        if (dirty)
            EditorUtility.SetDirty(Selection.gameObjects[0]);
    }
}
#endif