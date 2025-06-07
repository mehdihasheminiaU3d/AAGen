using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AAGen.Shared
{
    /// <summary>
    /// Represents a generic collection of UI components that can custonize the presentation using a bitmask.
    /// </summary>
    /// <remarks>
    /// It serves as a reusable UI to speed up the development of various tools related to the dependency graph.
    /// </remarks>
    // NOTE: appears the need for this class can be refactored or eliminated if UI toolkit was used.
    public class EditorUiGroup
    {
        #region Constants        
        private const int Space = 5;
        
        private const int FieldWidth = 800;
        
        private const int ButtonWidth = 350;
        
        private const int ButtonHeight = 25;

        private const int TextBoxHeight = 250;
        #endregion

        #region Types
        /// <summary>
        /// Represents a value indicating the method of drawing a field.
        /// </summary>
        [Flags]
        public enum UIVisibilityFlag
        {
            ShowIntField1 = 1 << 0,
            ShowStringField1 = 1 << 1,
            ShowHelpBox = 1 << 2,
            ShowOutput = 1 << 3,
            ShowFoldout = 1 << 4,
            ShowButton1 = 1 << 5,
            ShowObjectField1 = 1 << 6,
            ShowObjectField2 = 1 << 7,
        }
        #endregion

        #region Fields
        private Vector2 _textBoxScrollPosition;

        private bool _isUnfolded;

        //Delegates
        public Action ButtonAction;

        private GUIStyle _boldFoldoutStyle;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets a bitmask of values that indicate which fields are drawn in the UI.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the text that is outputted by
        /// </summary>
        public string OutputText { get; set; }
        
        public MessageType HelpMessageType { get; set; } = MessageType.Info;
        #endregion

        #region Methods
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
            // Draw an area of the UI that separates this area from other properties.
            GUILayout.Space(Space);

            // Draw this section with a vertical layout with a box style that encapsulates the items.
            using (var verticalScope = new GUILayout.VerticalScope(GUI.skin.box))
            {

                // If the group should draw a foldout, then draw the foldout,
                // otherwise assume there is no foldout to draw and it is not needed to draw the fields in the group.
                _isUnfolded = !UIVisibility.HasFlag(UIVisibilityFlag.ShowFoldout) ||
                           EditorGUILayout.Foldout(_isUnfolded, FoldoutLabel, _boldFoldoutStyle);

                // If the foldout is open or there is no foldout, then:
                if (_isUnfolded)
                {
                    // Draw this section with a horizontal layout.
                    using (var horizontalScope = new GUILayout.HorizontalScope())
                    {
                        // Draw this section with a vertical layout with a box style that encapsulates the items.
                        using (var verticalScope2 = new GUILayout.VerticalScope(GUI.skin.box))
                        {
                            // If the group should draw a help box, then:
                            if (UIVisibility.HasFlag(UIVisibilityFlag.ShowHelpBox))
                            {
                                // Draw the help box.
                                EditorGUILayout.HelpBox(HelpText, HelpMessageType);
                            }

                            // If the group should draw a object field, then:
                            if (UIVisibility.HasFlag(UIVisibilityFlag.ShowObjectField1))
                            {
                                // Draw the object field and capture the result of user control.
                                ObjectInput1 = EditorGUILayout.ObjectField(ObjectFieldLabel1, ObjectInput1, typeof(Object), false);
                            }

                            // If the group should draw a second object field, then:
                            if (UIVisibility.HasFlag(UIVisibilityFlag.ShowObjectField2))
                            {
                                // Draw the second object field and capture the result of user control.
                                ObjectInput2 = EditorGUILayout.ObjectField(ObjectFieldLabel2, ObjectInput2, typeof(Object), false);
                            }

                            // If the group should draw a integer field, then:
                            if (UIVisibility.HasFlag(UIVisibilityFlag.ShowIntField1))
                            {
                                // Draw the second int field and capture the result of user control.
                                IntegerInput1 = EditorGUILayout.IntField(IntegerFieldLabel1, IntegerInput1);
                            }

                            // If the group should draw a string field, then:
                            if (UIVisibility.HasFlag(UIVisibilityFlag.ShowStringField1))
                            {
                                // Draw the second string field and capture the result of user control.
                                StringInput1 = EditorGUILayout.TextField(StringFieldLabel1, StringInput1);
                            }
                        }
                    }

                    // Draw this section with a horizontal layout.
                    using (var horizontalScope = new GUILayout.HorizontalScope())
                    {
                        // If the group should draw a button, then:
                        if (UIVisibility.HasFlag(UIVisibilityFlag.ShowButton1))
                        {
                            // Draw the button and capture the result of user control. If the button was clicked, then:
                            if (GUILayout.Button(ButtonLabel, GUILayout.MaxWidth(ButtonWidth), GUILayout.Height(ButtonHeight)))
                            {
                                // Notify the subscriber that the button has been clicked.
                                ButtonAction?.Invoke();
                            }
                        }

                        GUILayout.FlexibleSpace();
                    }

                    // If the group should draw the output and the output text is valid, then:
                    if (UIVisibility.HasFlag(UIVisibilityFlag.ShowOutput) &&
                        !string.IsNullOrEmpty(OutputText))
                    {
                        // Draw the output text label.
                        GUILayout.Label(OutputLabel);

                        // Draw a scollable area, with every item in the sope a part of the scollable area.
                        using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(_textBoxScrollPosition, GUILayout.MaxHeight(TextBoxHeight)))
                        {
                            // Cache the position of the scrollable area to maintain the position.
                            _textBoxScrollPosition = scrollViewScope.scrollPosition;

                            // Draw a text area that presents the output text.
                            OutputText = EditorGUILayout.TextArea(OutputText, GUILayout.ExpandHeight(true));
                        }
                    }
                }
                else
                {
                    // Otherwise, the foldout is closed or otherwise the group should not be drawn.

                    // Set the output text to an invalid state.
                    OutputText = null;
                }
            }

            // Draw an area of the UI that separates this area from other properties.
            GUILayout.Space(Space);
        }
        #endregion
    }
}
