using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AAGen
{
    /// <summary>
    /// A generic collection of UI components that can be customized using a flag system.
    /// It serves as a reusable UI to speed up the development of various tools related to the dependency graph.
    /// </summary>
    public class EditorUiGroup
    {
        [Flags]
        public enum UIVisibilityFlag
        {
            ShowIntField1 = 1 << 0,
            ShowStringField1 = 1 << 1,
            ShowHelpBox = 1 << 2,
            ShowOutput = 1 << 3,
            ShowFoldout = 1 << 4,
            ShowButton1 = 1 << 5,
            ShowObjectFiled1 = 1 << 6,
            ShowObjectFiled2 = 1 << 7,
        }

        public UIVisibilityFlag UIVisibility { get; set; } = 0;

        //labels
        public string FoldoutLabel { get; set; } = "Custom Control Group";
        public string IntegerFieldLabel1 { get; set; } = "Integer Input";
        public string StringFieldLabel1 { get; set; } = "String Input";
        public string ObjectFieldLabel1 { get; set; } = "Object 1";
        public string ObjectFieldLabel2 { get; set; } = "Object 2";
        public string HelpText { get; set; } = "Help Text";
        public string OutputLabel { get; set; } = "Output";
        public string ButtonLabel { get; set; } = "Execute";

        //Field values
        public int IntegerInput1 { get; private set; }
        public string StringInput1 { get; private set; }
        public Object ObjectInput1 { get; private set; }
        public Object ObjectInput2 { get; private set; }
        public string OutputText { get; set; }
        
        public MessageType HelpMessageType { get; set; } = MessageType.Info;
        
        private Vector2 _textBoxScrollPosition;
        private bool _isUnfolded;

        //Delegates
        public Action ButtonAction;

        //Styles
        private const string BoxStyleName = "box";
        private const int Space = 5;
        private const int FieldWidth = 800;
        private const int ButtonWidth = 350;
        private const int ButtonHeight = 25;
        private const int TextBoxHeight = 250;
        private GUIStyle _boldFoldoutStyle;

        public EditorUiGroup()
        {
            _boldFoldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold
            };

            UIVisibility = UIVisibilityFlag.ShowFoldout |
                           UIVisibilityFlag.ShowButton1 |
                           UIVisibilityFlag.ShowOutput;
        }

        public virtual void OnGUI()
        {
            GUILayout.Space(Space);
            GUILayout.BeginVertical(BoxStyleName);

            _isUnfolded = !UIVisibility.HasFlag(UIVisibilityFlag.ShowFoldout) ||
                       EditorGUILayout.Foldout(_isUnfolded, FoldoutLabel, _boldFoldoutStyle);
            
            if (_isUnfolded)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.BeginVertical(GUILayout.Width(FieldWidth));
                    {
                        if (UIVisibility.HasFlag(UIVisibilityFlag.ShowHelpBox))
                            EditorGUILayout.HelpBox(HelpText, HelpMessageType);
                        
                        if (UIVisibility.HasFlag(UIVisibilityFlag.ShowObjectFiled1))
                            ObjectInput1 = EditorGUILayout.ObjectField(ObjectFieldLabel1, ObjectInput1, typeof(Object), false);
                        
                        if (UIVisibility.HasFlag(UIVisibilityFlag.ShowObjectFiled2))
                            ObjectInput2 = EditorGUILayout.ObjectField(ObjectFieldLabel2, ObjectInput2, typeof(Object), false);

                        if (UIVisibility.HasFlag(UIVisibilityFlag.ShowIntField1))
                            IntegerInput1 = EditorGUILayout.IntField(IntegerFieldLabel1, IntegerInput1);
                            
                        if (UIVisibility.HasFlag(UIVisibilityFlag.ShowStringField1))
                            StringInput1 = EditorGUILayout.TextField(StringFieldLabel1, StringInput1);
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
                
                GUILayout.BeginHorizontal();
                {
                    if (UIVisibility.HasFlag(UIVisibilityFlag.ShowButton1))
                    {
                        if (GUILayout.Button(ButtonLabel, GUILayout.MaxWidth(ButtonWidth), GUILayout.Height(ButtonHeight)))
                        {
                            ButtonAction?.Invoke();
                        }
                    }
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
                
                if (UIVisibility.HasFlag(UIVisibilityFlag.ShowOutput) && !string.IsNullOrEmpty(OutputText))
                {
                    GUILayout.Label(OutputLabel);
                    
                    _textBoxScrollPosition = EditorGUILayout.BeginScrollView(_textBoxScrollPosition,  GUILayout.MaxHeight(TextBoxHeight));
                    OutputText = EditorGUILayout.TextArea(OutputText,  GUILayout.ExpandHeight(true));
                    EditorGUILayout.EndScrollView();
                }
            }
            else
            {
                OutputText = null;
            }

            GUILayout.EndVertical();
            GUILayout.Space(Space);
        }
    }
}
