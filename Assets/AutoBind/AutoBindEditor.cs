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
        EditorApplication.update += OnUpdate;
    }

    private static void OnUpdate()
    {
        IsCanModify = true;
    }

    [MenuItem("GameObject/**添加绑定组件**", false, 0)]
    private static void AutoBind(MenuCommand menuCommand)
    {
        foreach (var go in Selection.gameObjects)
        {
            go.GetOrAddComponent<AutoBindCollection>();
        }

        EditorUtility.SetDirty(Selection.gameObjects[0]);
    }

    [MenuItem("GameObject/**取消绑定组件**", false, 0)]
    private static void RemoveAutoBind(MenuCommand menuCommand)
    {
        foreach (var go in Selection.gameObjects)
        {
            if (go.TryGetComponent<AutoBindCollection>(out var comp))
            {
                Object.DestroyImmediate(comp);
            }
        }
        EditorUtility.SetDirty(Selection.gameObjects[0]);
    }
}
#endif