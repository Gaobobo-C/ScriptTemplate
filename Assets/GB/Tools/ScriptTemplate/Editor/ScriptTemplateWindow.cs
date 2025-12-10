// ============================================
// 版权：Copyright (c) 2025 GB. All rights reserved.
// 文件名称：ScriptTemplateWindow.cs
// 作者：GB
// 创建时间：2025-12-10 09:58:51
// 修改时间：
// 版本：1.0.0
// 描述：创建自定义脚本工具
// Github：https://github.com/Gaobobo-C/ScriptTemplate.git
// ============================================

using UnityEngine;
using UnityEditor;
using System.IO;
using System;

namespace GB.Tools.Editor
{
    /// <summary>
    ///  脚本模板
    /// </summary>
    public class ScriptTemplateWindow : EditorWindow
    {
        private string scriptName = "NewScript";
        private string description = "";
        private string author = "GB";
        private string namespaceName = "GB";
        private TemplateType templateType = TemplateType.MonoBehaviour;

        private enum TemplateType
        {
            MonoBehaviour,
            ScriptableObject,
            //EditorWindow,
            //CustomClass,
            //Interface
        }

        // 修改 TemplateConfigPath 为通过代码动态获取当前脚本所在目录下的 /TemplateConfigs/
        private string TemplateConfigPath
        {
            get
            {
                // 获取当前脚本对象
                MonoScript monoScript = MonoScript.FromScriptableObject(this);
                // 获取当前脚本的Asset路径
                string scriptPath = AssetDatabase.GetAssetPath(monoScript);
                // 获取脚本所在目录
                string scriptDir = Path.GetDirectoryName(scriptPath);
                // 组合路径：脚本所在目录 + /TemplateConfigs/
                string configPath = Path.Combine(scriptDir, "TemplateConfigs").Replace("\\", "/") + "/";

                // 确保路径以 Assets/ 开头
                if (!configPath.StartsWith("Assets/"))
                {
                    Debug.LogError("无法确定脚本路径，请确保 ScriptTemplateWindow.cs 在 Assets 文件夹内。");
                }

                return configPath;
            }
        }

        [MenuItem("Assets/Create/Custom C# Script", false, 80)]
        public static void ShowWindow()
        {
            GetWindow<ScriptTemplateWindow>("创建自定义脚本");
        }

        void OnGUI()
        {
            // 绘制标题
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("创建自定义脚本", EditorStyles.boldLabel, GUILayout.Height(30));
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 输入字段
            templateType = (TemplateType)EditorGUILayout.EnumPopup("模板类型", templateType);
            scriptName = EditorGUILayout.TextField("脚本名称", scriptName);
            author = EditorGUILayout.TextField("作者", author);
            namespaceName = EditorGUILayout.TextField("命名空间", namespaceName);
            description = EditorGUILayout.TextField("描述", description);

            // 显示模板预览按钮
            if (GUILayout.Button("预览模板"))
            {
                ShowTemplatePreview();
            }

            EditorGUILayout.Space(20);

            // 创建按钮
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
                if (GUILayout.Button("创建", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    if (CreateScript())
                    {
                        // 1. 创建成功后关闭主窗口
                        Close();
                        // 2. 关闭预览窗口
                        ClosePreviewWindow();
                    }
                }
                GUI.backgroundColor = Color.white;

                if (GUILayout.Button("取消", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    // 1. 关闭主窗口
                    Close();
                    // 2. 关闭预览窗口
                    ClosePreviewWindow();
                }
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 查找并关闭 TextPreviewWindow 实例
        /// </summary>
        private void ClosePreviewWindow()
        {
            // 获取所有 TextPreviewWindow 的实例
            TextPreviewWindow preview = (TextPreviewWindow)EditorWindow.GetWindow(typeof(TextPreviewWindow), false, "模板预览");
            if (preview != null)
            {
                preview.Close();
            }
        }

        bool CreateScript()
        {
            // 验证脚本名称
            if (!IsValidScriptName(scriptName))
            {
                EditorUtility.DisplayDialog("错误",
                    "脚本名称无效！\n请使用字母开头，只包含字母、数字和下划线。", "确定");
                return false;
            }

            // 1. 确定保存目录 (保持原有逻辑：基于当前选中项)
            string folderPath = "Assets";
            if (Selection.activeObject != null)
            {
                folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (File.Exists(folderPath))
                {
                    // 如果选中了文件，则保存到该文件所在的目录
                    folderPath = Path.GetDirectoryName(folderPath);
                }
                // 如果选中了文件夹，folderPath已经是文件夹路径
            }

            // 2. 自动构建最终文件路径 (替换 SaveFilePanelInProject)
            string fileName = scriptName + ".cs";
            string filePath = Path.Combine(folderPath, fileName).Replace("\\", "/");

            // 3. 检查文件是否已存在，避免覆盖
            if (File.Exists(filePath))
            {
                EditorUtility.DisplayDialog("错误",
                    $"脚本创建失败：文件已存在于 '{filePath}'。\n请更改脚本名称。", "确定");
                return false;
            }

            try
            {
                // 读取模板并替换变量
                string templateContent = LoadTemplate();
                string finalScript = ReplaceTemplateVariables(templateContent, filePath);

                // 写入文件
                File.WriteAllText(filePath, finalScript, System.Text.Encoding.UTF8);
                AssetDatabase.Refresh();

                // 选中新创建的脚本
                var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(filePath);
                if (scriptAsset != null)
                {
                    Selection.activeObject = scriptAsset;
                    EditorGUIUtility.PingObject(scriptAsset);

                    // 打开脚本进行编辑（可选）
                    // AssetDatabase.OpenAsset(scriptAsset);
                }

                Debug.Log($"脚本创建成功: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("创建失败",
                    $"创建脚本时出错:\n{ex.Message}", "确定");
                Debug.LogError($"创建脚本失败: {ex}");
                return false;
            }
        }

        string LoadTemplate()
        {
            // 根据模板类型选择文件
            string templateFileName = GetTemplateFileName();
            // 注意：这里使用 File.ReadAllText，路径需要是完整的绝对路径，
            // 但在 Unity Editor 中，Assets 路径通常可以直接使用。
            // 确保 TemplateConfigPath 已经返回了 Assets/xxx/TemplateConfigs/ 这样的路径
            string templatePath = Path.Combine(TemplateConfigPath, templateFileName).Replace("\\", "/");

            // 如果模板文件不存在，使用默认模板
            if (!File.Exists(templatePath))
            {
                Debug.LogWarning($"模板文件不存在: {templatePath}，使用默认模板");
                return GetDefaultTemplate();
            }

            try
            {
                return File.ReadAllText(templatePath, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogError($"读取模板文件失败: {ex}");
                return GetDefaultTemplate();
            }
        }

        string GetTemplateFileName()
        {
            switch (templateType)
            {
                case TemplateType.MonoBehaviour:
                    return "MonoBehaviourTemplate.txt";
                case TemplateType.ScriptableObject:
                    return "ScriptableObjectTemplate.txt";
                //case TemplateType.EditorWindow:
                //    return "EditorWindowTemplate.txt";
                //case TemplateType.CustomClass:
                //    return "ClassTemplate.txt";
                //case TemplateType.Interface:
                //    return "InterfaceTemplate.txt";
                default:
                    return "MonoBehaviourTemplate.txt";
            }
        }

        string GetDefaultTemplate()
        {
            return @"// ============================================
// 版权：Copyright (c) {{YEAR}} {{AUTHOR}}. All rights reserved.
// 文件名称：{{FILENAME}}
// 作者：{{AUTHOR}}
// 创建时间：{{CREATE_TIME}}
// 修改时间：
// 版本：1.0.0
// 描述：{{DESCRIPTION}}
// Github：{{GITHUB_URL}}
// ============================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace {{NAMESPACE}}
{
    public class {{CLASS_NAME}} : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }
}";
        }

        string ReplaceTemplateVariables(string template, string filePath)
        {
            string fileName = Path.GetFileName(filePath);

            // 替换所有变量
            return template
                .Replace("{{YEAR}}", DateTime.Now.Year.ToString())
                .Replace("{{AUTHOR}}", author)
                .Replace("{{FILENAME}}", fileName)
                .Replace("{{CREATE_TIME}}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                .Replace("{{DESCRIPTION}}", description)
                .Replace("{{GITHUB_URL}}", "")
                .Replace("{{NAMESPACE}}", namespaceName)
                .Replace("{{CLASS_NAME}}", scriptName)
                .Replace("{{FILEPATH}}", filePath.Replace("\\", "/"));
        }

        void ShowTemplatePreview()
        {
            try
            {
                string templateContent = LoadTemplate();
                string previewContent = ReplaceTemplateVariables(templateContent, scriptName + ".cs");

                // 创建预览窗口
                var previewWindow = GetWindow<TextPreviewWindow>("模板预览");
                previewWindow.SetContent(previewContent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"预览模板失败: {ex}");
            }
        }

        bool IsValidScriptName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (!char.IsLetter(name[0]) && name[0] != '_') return false;

            // 检查是否包含无效字符
            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;
            }

            // 检查是否为C#关键字
            string[] csharpKeywords = {
                "abstract", "as", "base", "bool", "break", "byte", "case", "catch",
                "char", "checked", "class", "const", "continue", "decimal", "default",
                "delegate", "do", "double", "else", "enum", "event", "explicit",
                "extern", "false", "finally", "fixed", "float", "for", "foreach",
                "goto", "if", "implicit", "in", "int", "interface", "internal",
                "is", "lock", "long", "namespace", "new", "null", "object",
                "operator", "out", "override", "params", "private", "protected",
                "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
                "sizeof", "stackalloc", "static", "string", "struct", "switch",
                "this", "throw", "true", "try", "typeof", "uint", "ulong",
                "unchecked", "unsafe", "ushort", "using", "virtual", "void",
                "volatile", "while"
            };

            if (Array.Exists(csharpKeywords, keyword => keyword == name.ToLower()))
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 文本预览窗口
    /// </summary>
    public class TextPreviewWindow : EditorWindow
    {
        private string content = "";
        private Vector2 scrollPosition;

        public void SetContent(string text)
        {
            content = text;
        }

        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            {
                EditorGUILayout.TextArea(content, GUILayout.ExpandHeight(true));
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("关闭"))
            {
                Close();
            }
        }
    }
}