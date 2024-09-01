using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
[CustomEditor(typeof(AutoBindComponent))]
public class AutoBindComponentInspector : UnityEditor.Editor
{
    private class CodeGenerator
    {
        class FieldInfo
        {
            public string Flag;
            public string Name;
            public Type Type;
        }

        class GetterInfo
        {
            public string Flag;
            public string Name;
            public Type Type;
            public int Index;
        }

        private StringBuilder mStringBuilder = new StringBuilder();
        private List<FieldInfo> mFieldInfos = new List<FieldInfo>();
        private List<GetterInfo> mGetterInfos = new List<GetterInfo>();
        private string mNameSpace;
        private string mClassName;
        private int mTabCount;

        public void Clear()
        {
            mStringBuilder.Clear();
            mFieldInfos.Clear();
            mGetterInfos.Clear();
            mNameSpace = string.Empty;
            mClassName = string.Empty;
            mTabCount = 0;
        }

        public void SetClassName(string name)
        {
            mClassName = name;
        }

        public void SetNameSpace(string name)
        {
            mNameSpace = name;
        }

        public void AddField(string flag, Type type, string name)
        {
            mFieldInfos.Add(new FieldInfo()
            {
                Flag = flag,
                Type = type,
                Name = name,
            });
        }

        public void AddGetter(string flag, Type type, string name, int index)
        {
            mGetterInfos.Add(new GetterInfo()
            {
                Flag = flag,
                Type = type,
                Name = name,
                Index = index,
            });
        }

        private void AppendLine(string text)
        {
            if (text.Contains("}"))
                mTabCount--;
            for (var i = 0; i < mTabCount; i++)
            {
                mStringBuilder.Append("\t");
            }

            mStringBuilder.AppendLine(text);
            if (text.Contains("{"))
                mTabCount++;
        }

        public override string ToString()
        {
            mStringBuilder.Clear();

            var set = new HashSet<string>();
            foreach (var fieldInfo in mFieldInfos)
            {
                var namespaceStr = fieldInfo.Type.Namespace;
                if (!string.IsNullOrEmpty(namespaceStr) && !namespaceStr.Equals(mNameSpace))
                {
                    set.Add(namespaceStr);
                }
            }

            foreach (var getterInfo in mGetterInfos)
            {
                var namespaceStr = getterInfo.Type.Namespace;
                if (!string.IsNullOrEmpty(namespaceStr) && !namespaceStr.Equals(mNameSpace))
                {
                    set.Add(namespaceStr);
                }
            }

            foreach (var s in set)
            {
                AppendLine($"using {s};");
            }

            AppendLine("");

            if (!string.IsNullOrEmpty(mNameSpace))
            {
                AppendLine($"namespace {mNameSpace}");
                AppendLine("{");
            }

            AppendLine($"public partial class {mClassName}");
            AppendLine("{");

            var compName = $"{nameof(AutoBindComponent)}";
            AppendLine($"private {compName} _{compName};");
            AppendLine($"private {compName} m_{compName} => _{compName} ??= gameObject.GetComponent<{compName}>();");

            foreach (var fieldInfo in mFieldInfos)
            {
                AppendLine($"{fieldInfo.Flag} {fieldInfo.Type.Name} {fieldInfo.Name};");
            }

            foreach (var getterInfo in mGetterInfos)
            {
                AppendLine($"{getterInfo.Flag} {getterInfo.Type.Name} {getterInfo.Name} => ({getterInfo.Type.Name})m_{compName}.m_BindDatas[{getterInfo.Index}].BindComp;");
            }

            if (!string.IsNullOrEmpty(mNameSpace))
            {
                AppendLine("}");
            }

            AppendLine("}");

            return mStringBuilder.ToString();
        }
    }

    private AutoBindComponent m_Target;

    private SerializedProperty m_BindDatas;

    private List<GameObject> m_Collection = new List<GameObject>();

    private GUIStyle m_FoldoutBtnStyle;

    private static CodeGenerator m_CodeGenerator = new CodeGenerator();

    private Dictionary<int, bool> foldoutDict = new Dictionary<int, bool>();

    private static Dictionary<string, string> TypeNameMap = new Dictionary<string, string>()
    {
        { nameof(GameObject), "Go" },
        { nameof(Transform), "Trans" },
        { nameof(RectTransform), "RTrans" },
        { nameof(Image), "Img" },
        { nameof(Button), "Btn" },
    };

    private List<Object> m_Comps = new List<Object>();
    private List<Transform> m_NodeList = new();
    private List<AutoBindComponent.BindData> m_TempList = new();

    private void OnEnable()
    {
        m_Target = (AutoBindComponent)target;
        m_BindDatas = serializedObject.FindProperty("m_BindDatas");

        SetCollection();

        serializedObject.ApplyModifiedProperties();
    }

    public override void OnInspectorGUI()
    {
        // 自定义一个GUIStyle，用于让按钮看起来更像是一个普通的Label
        if (m_FoldoutBtnStyle == null)
        {
            m_FoldoutBtnStyle = new GUIStyle(GUI.skin.button);
            m_FoldoutBtnStyle.padding = new RectOffset(10, 0, 0, 0); // 移除内边距  
            m_FoldoutBtnStyle.margin = new RectOffset(0, 0, 0, 0);   // 移除外边距  
            m_FoldoutBtnStyle.normal.background = null;              // 移除背景图片  
            m_FoldoutBtnStyle.hover.background = null;               // 移除悬停背景图片  
            m_FoldoutBtnStyle.active.background = null;              // 移除激活背景图片  
            m_FoldoutBtnStyle.focused.background = null;             // 移除聚焦背景图片  
            m_FoldoutBtnStyle.onNormal.textColor = Color.black;      // 设置文字颜色
            m_FoldoutBtnStyle.alignment = TextAnchor.MiddleLeft;
        }

        serializedObject.Update();

        base.OnInspectorGUI();

        SetCollection();

        DrawButton();

        DrawRow();

        RemoveInvalid();

        var rowAdd = m_BindDatas.arraySize > m_Target.m_BindDatas.Count;

        serializedObject.ApplyModifiedProperties();

        if (rowAdd)
        {
            serializedObject.Update();

            SortBindData();

            serializedObject.ApplyModifiedProperties();
        }
    }

    private void RemoveInvalid()
    {
        for (var i = m_BindDatas.arraySize - 1; i >= 0; i--)
        {
            var nameProperty = m_BindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("Name");
            var compProperty = m_BindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("BindComp");
            if (compProperty.objectReferenceValue == null)
            {
                m_BindDatas.DeleteArrayElementAtIndex(i);
            }
        }
    }

    private void SetCollection()
    {
        m_Collection.Clear();

        void Iteration(Transform transform)
        {
            foreach (Transform child in transform)
            {
                Iteration(child);
                if (child.name.StartsWith("@"))
                {
                    m_Collection.Add(child.gameObject);
                }
            }
        }

        Iteration(m_Target.transform);
    }

    static string GetScriptPath(string scriptName)
    {
        var path = AssetDatabase.FindAssets(scriptName);
        foreach (var s in path)
        {
            var scriptPath = AssetDatabase.GUIDToAssetPath(s.Replace((@"/" + scriptName + ".cs"), ""));
            if (!Directory.Exists(scriptPath) && Path.GetFileNameWithoutExtension(scriptPath).Equals(scriptName))
            {
                return scriptPath;
            }
        }

        return string.Empty;
    }

    private void DrawButton()
    {
        // if (GUILayout.Button("清理"))
        // {
        //     serializedObject.Update();
        //     m_BindDatas.ClearArray();
        //     serializedObject.ApplyModifiedProperties();
        //     GenCode();
        // }

        if (GUILayout.Button("生成代码"))
        {
            GenCode();
        }

        if (GUILayout.Button("展开全部"))
        {
            var keys = foldoutDict.Keys.ToList();
            foreach (var key in keys)
            {
                foldoutDict[key] = true;
            }
        }

        if (GUILayout.Button("折叠全部"))
        {
            var keys = foldoutDict.Keys.ToList();
            foreach (var key in keys)
            {
                foldoutDict[key] = false;
            }
        }
    }

    private void GenCode()
    {
        if (m_Target.GeneraterComponent)
        {
            var type = m_Target.GeneraterComponent.GetType();
            m_CodeGenerator.Clear();
            m_CodeGenerator.SetNameSpace(type.Namespace);
            m_CodeGenerator.SetClassName(type.Name);

            // foreach (var bindData in m_Target.m_BindDatas)
            // {
            //     var compType = bindData.BindComp.GetType();
            //     m_CodeGenerator.AddField("private", compType, $"{bindData.Name}_{compType.Name}");
            // }

            for (var i = 0; i < m_Target.m_BindDatas.Count; i++)
            {
                var bindData = m_Target.m_BindDatas[i];
                var compType = bindData.BindComp.GetType();
                m_CodeGenerator.AddGetter("private", compType, bindData.VarName, i);
            }

            var codeText = m_CodeGenerator.ToString();
            var scriptPath = GetScriptPath(m_Target.GeneraterComponent.GetType().Name);
            var dir = scriptPath.Replace(Path.GetFileName(scriptPath), "");
            var savePath = $"{dir}/{Path.GetFileNameWithoutExtension(scriptPath)}.Auto.cs";

            var mainScriptText = File.ReadAllText(scriptPath);
            var match1 = Regex.Match(mainScriptText, $@"class[^.]+{type.Name}[^.]*:");
            var match2 = Regex.Match(mainScriptText, $@"partial[^.]+class[^.]+{type.Name}[^.]*:");
            if (match1.Success && !match2.Success)
            {
                mainScriptText = mainScriptText.Replace(match1.Value, $"partial class {type.Name} :");
                File.WriteAllText(scriptPath, mainScriptText);
            }

            File.WriteAllText(savePath, codeText);
            AssetDatabase.Refresh();
        }
    }

    private void DrawRow()
    {
        foreach (var node in m_Collection)
        {
            foldoutDict.TryAdd(node.GetHashCode(), false);
            EditorGUILayout.BeginHorizontal();

            var foldout = foldoutDict[node.GetHashCode()];
            var parentText = node.gameObject.Equals(m_Target.gameObject) ? string.Empty : $"{node.transform.parent.name} \\ ";

            if (GUILayout.Button($"{parentText}{node.name}", m_FoldoutBtnStyle, GUILayout.Width(200)))
            {
                foldout = !foldout;
            }

            GUI.enabled = false;
            EditorGUILayout.ObjectField(node.gameObject, typeof(Component), false);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            foldoutDict[node.GetHashCode()] = foldout;
            if (foldout)
            {
                // var comps = node.GetComponents<Component>().ToList();
                m_Comps.Clear();
                m_Comps.Add(node.gameObject);
                m_Comps.AddRange(node.GetComponents<Component>().ToList());
                for (var i = 0; i < m_Comps.Count; i++)
                {
                    var comp = m_Comps[i];
                    var type = comp.GetType();
                    if (m_Target.exclude.Contains(type))
                        continue;
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);

                    var exists = IsExists(comp);
                    // if (GUILayout.Button($"{type.Name}", m_FoldoutBtnStyle, GUILayout.Width(180)))
                    // {
                    //     exists = !exists;
                    //     if (exists) AddBindData(comp.name, comp);
                    //     else RemoveBindData(comp.name, comp);
                    // }

                    var value = EditorGUILayout.ToggleLeft(type.Name, exists, GUILayout.Width(180));
                    if (value != exists)
                    {
                        exists = !exists;
                        if (exists) AddBindData(comp, "");
                        else RemoveBindData(comp);
                    }

                    var item = m_Target.m_BindDatas.Find(v => v.BindComp.Equals(comp));
                    if (item != null)
                    {
                        var varName = EditorGUILayout.TextField(item.VarName);
                        if (!varName.Equals(item.VarName))
                        {
                            SetBindDataVarName(item.Name, item.BindComp, varName);
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }

    private void AddBindData(Object comp, string varName)
    {
        if (!IsExists(comp))
        {
            var index = m_BindDatas.arraySize;
            m_BindDatas.InsertArrayElementAtIndex(index);
            var element = m_BindDatas.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("Name").stringValue = comp.name;
            element.FindPropertyRelative("BindComp").objectReferenceValue = comp;

            var typeName = comp.GetType().Name;
            if (TypeNameMap.TryGetValue(typeName, out var typeMap))
            {
                if (comp.name.StartsWith(typeMap) || comp.name.EndsWith(typeMap))
                {
                    typeMap = "";
                }
            }
            else
            {
                if (comp.name.StartsWith(typeName) || comp.name.EndsWith(typeName))
                {
                    typeMap = "";
                }
                else
                {
                    typeMap = typeName;
                }
            }

            var varName2 = string.IsNullOrEmpty(typeMap) ? comp.name : $"{comp.name}_{typeMap}";
            var varName3 = string.IsNullOrEmpty(varName) ? varName2 : varName;
            varName3 = varName3.Replace(" ", "_").Replace("(", "").Replace(")", "");
            element.FindPropertyRelative("VarName").stringValue = varName3;
        }
    }

    private void SortBindData()
    {
        void AddChild(List<Transform> list, Transform parent)
        {
            list.Add(parent);
            for (var i = 0; i < parent.childCount; i++)
            {
                AddChild(list, parent.GetChild(i));
            }
        }

        m_NodeList.Clear();
        AddChild(m_NodeList, m_Target.transform);

        m_TempList.Clear();
        m_TempList.AddRange(m_Target.m_BindDatas);

        m_Target.m_BindDatas.Clear();
        m_BindDatas.ClearArray();

        for (var i = 0; i < m_NodeList.Count; i++)
        {
            var node = m_NodeList[i];
            var nodes = m_TempList.Where(v => v.BindComp.Equals(node.gameObject) || ((v.BindComp as Component)?.gameObject == node.gameObject)).ToList();
            if (nodes.Count > 0)
            {
                foreach (var data in nodes)
                {
                    AddBindData(data.BindComp, data.VarName);
                }
            }
        }
    }

    private void RemoveBindData(Object comp)
    {
        for (var i = m_BindDatas.arraySize - 1; i >= 0; i--)
        {
            var nameProperty = m_BindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("Name");
            var compProperty = m_BindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("BindComp");
            if (compProperty.objectReferenceValue.Equals(comp))
            {
                m_BindDatas.DeleteArrayElementAtIndex(i);
                break;
            }
        }
    }

    private void SetBindDataVarName(string name, Object comp, string propertyValue)
    {
        for (var i = m_BindDatas.arraySize - 1; i >= 0; i--)
        {
            var nameProperty = m_BindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("Name");
            var compProperty = m_BindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("BindComp");
            var varProperty = m_BindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("VarName");
            if (name.Equals(nameProperty.stringValue) && compProperty.objectReferenceValue.Equals(comp))
            {
                varProperty.stringValue = propertyValue;
                break;
            }
        }
    }

    private bool IsExists(Object comp)
    {
        return FindSerializedData(comp) != null;
    }

    private SerializedProperty FindSerializedData(Object comp)
    {
        for (var i = 0; i < m_BindDatas.arraySize; i++)
        {
            var nameProperty = m_BindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("Name");
            var compProperty = m_BindDatas.GetArrayElementAtIndex(i).FindPropertyRelative("BindComp");
            if (compProperty.objectReferenceValue.Equals(comp))
            {
                return m_BindDatas.GetArrayElementAtIndex(i);
            }
        }

        return null;
    }
}
#endif

public class AutoBindComponent : MonoBehaviour
{
    [Serializable]
    public class BindData
    {
        public string Name;
        public string VarName;
        public Object BindComp;

        public BindData()
        {
        }

        public BindData(string name, Object bindComp)
        {
            Name = name;
            BindComp = bindComp;
        }
    }

    [SerializeField, HideInInspector] public List<BindData> m_BindDatas = new List<BindData>();

    [SerializeField] public Component GeneraterComponent;

    [HideInInspector] public HashSet<Type> exclude = new HashSet<Type>()
    {
        typeof(AutoBindComponent),
        typeof(CanvasRenderer),
    };
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!GeneraterComponent)
        {
            var monoBehaviours = transform.GetComponents<MonoBehaviour>();
            if (monoBehaviours.Length > 0)
                GeneraterComponent = monoBehaviours[0];
        }
    }
#endif
}