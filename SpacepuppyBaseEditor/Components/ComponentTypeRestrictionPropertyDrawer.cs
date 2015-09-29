﻿using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

using com.spacepuppy;
using com.spacepuppy.Utils;

namespace com.spacepuppyeditor.Components
{

    [CustomPropertyDrawer(typeof(ComponentTypeRestrictionAttribute))]
    public class ComponentTypeRestrictionPropertyDrawer : PropertyDrawer
    {

        #region Fields

        //private Dictionary<string, System.Type> _overrideBaseTypeDict = new Dictionary<string, System.Type>();

        private SelectableComponentPropertyDrawer _selectComponentDrawer;

        #endregion

        #region Utils

        private bool ValidateFieldType()
        {
            bool isArray = this.fieldInfo.FieldType.IsListType();
            var fieldType = (isArray) ? this.fieldInfo.FieldType.GetElementTypeOfListType() : this.fieldInfo.FieldType;
            if (!TypeUtil.IsType(fieldType, typeof(Component))) return false;

            var attrib = this.attribute as ComponentTypeRestrictionAttribute;
            return attrib.InheritsFromType == null ||
                attrib.InheritsFromType.IsInterface ||
                TypeUtil.IsType(attrib.InheritsFromType, fieldType);
            //return attrib.InheritsFromType == null ||
            //    TypeUtil.IsType(attrib.InheritsFromType, fieldType) ||
            //    TypeUtil.IsType(attrib.InheritsFromType, typeof(IComponent));
        }

        #endregion

        #region Drawer Overrides

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!this.ValidateFieldType())
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            //get base type
            var attrib = this.attribute as ComponentTypeRestrictionAttribute;
            var inheritsFromType = attrib.InheritsFromType ?? typeof(Component);

            bool isArray = this.fieldInfo.FieldType.IsListType();
            var fieldType = (isArray) ? this.fieldInfo.FieldType.GetElementTypeOfListType() : this.fieldInfo.FieldType;

            if (attrib.HideTypeDropDown)
            {
                //draw object field
                property.objectReferenceValue = SPEditorGUI.ComponentField(position, label, property.objectReferenceValue as Component, inheritsFromType, true, fieldType);
            }
            else
            {
                //draw complex field
                if(_selectComponentDrawer == null)
                {
                    _selectComponentDrawer = new SelectableComponentPropertyDrawer();
                }

                _selectComponentDrawer.RestrictionType = inheritsFromType;
                _selectComponentDrawer.ShowXButton = true;

                _selectComponentDrawer.OnGUI(position, property, label);
            }

            EditorGUI.EndProperty();
        }
        

        /*

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (this.ValidateFieldType())
            {
                var attrib = this.attribute as ComponentTypeRestrictionAttribute;

                if (!attrib.HideTypeDropDown && property.isExpanded)
                {
                    return 3f * EditorGUIUtility.singleLineHeight + 2f;
                }
                else
                {
                    return EditorGUIUtility.singleLineHeight;
                }
            }
            else
            {
                return EditorGUIUtility.singleLineHeight;
            }
        }


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if(!this.ValidateFieldType())
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            //get base type
            var attrib = this.attribute as ComponentTypeRestrictionAttribute;
            var inheritsFromType = attrib.InheritsFromType ?? typeof(Component);
            
            bool isArray = this.fieldInfo.FieldType.IsListType();
            var fieldType = (isArray) ? this.fieldInfo.FieldType.GetElementTypeOfListType() : this.fieldInfo.FieldType;

            if (attrib.HideTypeDropDown)
            {
                //draw object field
                property.objectReferenceValue = SPEditorGUI.ComponentField(position, label, property.objectReferenceValue as Component, inheritsFromType, true, fieldType);
                property.serializedObject.ApplyModifiedProperties();
            }
            else
            {
                //draw
                var h = EditorGUIUtility.singleLineHeight;
                var r0 = new Rect(position.xMin, position.yMin, position.width, h);
                bool isExpanded = property.isExpanded;
                property.isExpanded = EditorGUI.Foldout(r0, isExpanded, this.GetHeaderLabel(property, label, inheritsFromType));

                if (isExpanded)
                {
                    EditorGUI.indentLevel += 1;

                    var r1 = new Rect(position.xMin, position.yMin + h + 1f, position.width, h);
                    var r2 = new Rect(position.xMin, position.yMin + (2f * h) + 2f, position.width, h);

                    //draw component type selector
                    if (!_overrideBaseTypeDict.ContainsKey(label.text) || _overrideBaseTypeDict[label.text] == null)
                    {
                        if (property.objectReferenceValue != null)
                            _overrideBaseTypeDict[label.text] = property.objectReferenceValue.GetType();
                        else
                            _overrideBaseTypeDict[label.text] = inheritsFromType;
                    }
                    _overrideBaseTypeDict[label.text] = SPEditorGUI.TypeDropDown(r1, new GUIContent("Restrict Type"), inheritsFromType, _overrideBaseTypeDict[label.text], true, true, inheritsFromType, null, (this.attribute as ComponentTypeRestrictionAttribute).MenuListingStyle);
                    inheritsFromType = _overrideBaseTypeDict[label.text];

                    //draw object field
                    var refLabel = new GUIContent("Reference");
                    property.objectReferenceValue = SPEditorGUI.ComponentField(r2, refLabel, property.objectReferenceValue as Component, inheritsFromType, true, fieldType);
                    property.serializedObject.ApplyModifiedProperties();

                    EditorGUI.indentLevel -= 1;
                }
            }

            EditorGUI.EndProperty();
        }
        
        private GUIContent GetHeaderLabel(SerializedProperty property, GUIContent label, System.Type inheritsFromType)
        {
            string suffix = (property.objectReferenceValue != null) ? "(" + property.objectReferenceValue.GetType().Name + ")" : "(" + inheritsFromType.Name + ")";
            string tooltip = inheritsFromType.Name;
            if (property.objectReferenceValue != null) tooltip += " - " + suffix;

            var tooltipAttrib = this.fieldInfo.GetCustomAttributes(typeof(TooltipAttribute), false).FirstOrDefault() as TooltipAttribute;
            if (tooltipAttrib != null) tooltip += "\n\n" + tooltipAttrib.tooltip;

            return new GUIContent(label.text + " " + suffix, tooltip);
        }

        */

        #endregion


    }
}
