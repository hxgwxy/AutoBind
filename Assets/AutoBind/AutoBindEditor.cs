using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public class AutoBindEditor
{
    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
    }

    [MenuItem("GameObject/AutoBind/生成绑定代码[Ctrl+`] %`", false, 0)]
    private static void GenerateCode(MenuCommand menuCommand)
    {
        var go = Selection.activeGameObject;
        if (go)
        {
            go.GetComponent<AutoBindComponent>()?.GenCode();
            go.GetComponentInParent<AutoBindComponent>()?.GenCode();
        }
    }

    [MenuItem("GameObject/AutoBind/绑定组件、取消绑定[`] _`", false, 1)]
    private static void AutoBind(MenuCommand menuCommand)
    {
        var dirty = false;
        foreach (var go in Selection.gameObjects)
        {
            if (!go.GetComponent<AutoBindComponent>())
            {
                if (!go.name.StartsWith("@"))
                {
                    go.name = $"@{go.name}";
                }
                else
                {
                    go.name = go.name.Substring(1, go.name.Length - 1);
                }

                dirty = true;
            }
        }

        if (dirty)
            EditorUtility.SetDirty(Selection.gameObjects[0]);
    }
}
#endif