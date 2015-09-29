﻿using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;

using com.spacepuppy;
using com.spacepuppy.Collections;
using com.spacepuppy.Utils;

using com.spacepuppyeditor.Modifiers;

namespace com.spacepuppyeditor.Internal
{
    internal class MultiPropertyAttributePropertyHandler : UnityInternalPropertyHandler
    {

        #region Fields

        private System.Reflection.FieldInfo _fieldInfo;

        private PropertyDrawer _drawer;
        private List<PropertyModifier> _modifiers;

        #endregion

        #region CONSTRUCTOR
        
        public MultiPropertyAttributePropertyHandler(System.Reflection.FieldInfo fieldInfo, PropertyAttribute[] attribs)
        {
            if (fieldInfo == null) throw new System.ArgumentNullException("fieldInfo");
            if (attribs == null) throw new System.ArgumentNullException("attribs");
            _fieldInfo = fieldInfo;

            this.Init(attribs);
        }

        #endregion

        #region Properties

        public System.Reflection.FieldInfo Field { get { return _fieldInfo; } }

        #endregion

        #region Methods

        protected virtual void Init(PropertyAttribute[] attribs)
        {
            var fieldType = _fieldInfo.FieldType;
            if (fieldType.IsListType()) fieldType = fieldType.GetElementTypeOfListType();

            var fieldTypePropertyDrawerType = ScriptAttributeUtility.GetDrawerTypeForType(fieldType);
            if (fieldTypePropertyDrawerType != null && TypeUtil.IsType(fieldTypePropertyDrawerType, typeof(PropertyDrawer)))
            {
                _drawer = PropertyDrawerActivator.Create(fieldTypePropertyDrawerType, null, _fieldInfo);
                if (_drawer != null && _fieldInfo.FieldType.IsListType()) _drawer = new ArrayPropertyDrawer(_drawer);
            }


            foreach(var attrib in attribs)
            {
                this.HandleAttribute(attrib, _fieldInfo, fieldType);
            }
        }

        protected override void HandleAttribute(PropertyAttribute attribute, System.Reflection.FieldInfo field, System.Type propertyType)
        {
            if(attribute is PropertyModifierAttribute)
            {
                var mtp = ScriptAttributeUtility.GetDrawerTypeForType(attribute.GetType());
                if (TypeUtil.IsType(mtp, typeof(PropertyModifier)))
                {
                    var modifier = PropertyDrawerActivator.Create(mtp, attribute, field) as PropertyModifier;
                    modifier.Init(false);
                    if (_modifiers == null) _modifiers = new List<PropertyModifier>();
                    _modifiers.Add(modifier);
                }
            }
            else
            {
                base.HandleAttribute(attribute, field, propertyType);
                var drawer = this.InternalDrawer; //this retrieves the drawer that was selected by called 'base.HandleAttribute'
                this.AppendDrawer(drawer);
            }
        }

        protected void AppendDrawer(PropertyDrawer drawer)
        {
            if (_drawer == null)
            {
                //no drawer has been set before... lets see if we got one
                if (drawer != null)
                {
                    //we got a new drawer, set it
                    if (this.Field.FieldType.IsListType()) drawer = new ArrayPropertyDrawer(drawer);
                    _drawer = drawer;
                }
            }
            else if (drawer != _drawer)
            {
                //a new drawer was created, lets see what we need to do with it compared to the last one
                if (drawer is PropertyModifier)
                {
                    if (_modifiers == null) _modifiers = new List<PropertyModifier>();
                    _modifiers.Add(drawer as PropertyModifier);
                    this.InternalDrawer = _drawer;
                }
                else if (drawer is IArrayHandlingPropertyDrawer)
                {
                    //got an array drawer, this overrides previous drawers
                    if (_drawer is IArrayHandlingPropertyDrawer)
                    {
                        var temp = _drawer as IArrayHandlingPropertyDrawer;
                        _drawer = drawer;
                        (_drawer as IArrayHandlingPropertyDrawer).InternalDrawer = temp.InternalDrawer;
                    }
                    else if (_drawer != null)
                    {
                        var temp = _drawer;
                        _drawer = drawer;
                        (_drawer as IArrayHandlingPropertyDrawer).InternalDrawer = temp;
                    }
                    else
                    {
                        _drawer = drawer;
                    }
                }
                else if (_drawer is IArrayHandlingPropertyDrawer)
                {
                    //got an internal drawer for the existing array drawer
                    (_drawer as IArrayHandlingPropertyDrawer).InternalDrawer = drawer;
                    this.InternalDrawer = _drawer;
                }
                else
                {
                    //we got a new drawer, set it
                    if (this.Field.FieldType.IsListType())
                    {
                        _drawer = new ArrayPropertyDrawer(drawer);
                        this.InternalDrawer = _drawer;
                    }
                    else
                    {
                        _drawer = drawer;
                    }
                }
            }
        }





        public override bool OnGUI(Rect position, SerializedProperty property, GUIContent label, bool includeChildren)
        {
            if (_modifiers != null)
            {
                for (int i = 0; i < _modifiers.Count; i++)
                {
                    _modifiers[i].OnBeforeGUI(property);
                }
            }

            bool result = base.OnGUI(position, property, label, includeChildren);
            PropertyHandlerValidationUtility.AddAsHandled(property, this);

            if (_modifiers != null)
            {
                for (int i = 0; i < _modifiers.Count; i++)
                {
                    _modifiers[i].OnPostGUI(property);
                }
            }

            return result;
        }

        public override bool OnGUILayout(SerializedProperty property, GUIContent label, bool includeChildren, GUILayoutOption[] options)
        {
            if (_modifiers != null)
            {
                for (int i = 0; i < _modifiers.Count; i++)
                {
                    _modifiers[i].OnBeforeGUI(property);
                }
            }

            var result = base.OnGUILayout(property, label, includeChildren, options);
            PropertyHandlerValidationUtility.AddAsHandled(property, this);

            if (_modifiers != null)
            {
                for (int i = 0; i < _modifiers.Count; i++)
                {
                    _modifiers[i].OnPostGUI(property);
                }
            }

            return result;
        }

        public override void OnValidate(SerializedProperty property)
        {
            if (_modifiers != null)
            {
                for (int i = 0; i < _modifiers.Count; i++)
                {
                    _modifiers[i].OnValidate(property);
                }
            }
        }

        #endregion




        #region Special Types

        private class ArrayPropertyDrawer : PropertyDrawer, IArrayHandlingPropertyDrawer
        {

            #region Fields

            private PropertyDrawer _drawer;

            #endregion

            #region CONSTRUCTOR

            public ArrayPropertyDrawer(PropertyDrawer drawer)
            {
                _drawer = drawer;
            }

            #endregion

            #region PropertyDrawer Interface

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                if (label == null) label = EditorHelper.TempContent(property.displayName);

                if (!property.isArray)
                {
                    return _drawer.GetPropertyHeight(property, label);
                }
                else
                {
                    float h = SPEditorGUI.GetSinglePropertyHeight(property, label);
                    if (!property.isExpanded) return h;

                    h += EditorGUIUtility.singleLineHeight + 2f;

                    for (int i = 0; i < property.arraySize; i++)
                    {
                        var pchild = property.GetArrayElementAtIndex(i);
                        h += _drawer.GetPropertyHeight(pchild, EditorHelper.TempContent(pchild.displayName)) + 2f;
                    }
                    return h;
                }
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                if (label == null) label = EditorHelper.TempContent(property.displayName);

                if (!property.isArray)
                {
                    _drawer.OnGUI(position, property, label);
                }
                else
                {
                    if (property.isExpanded)
                    {
                        var rect = new Rect(position.xMin, position.yMin, position.width, EditorGUIUtility.singleLineHeight);
                        property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label);

                        EditorGUI.indentLevel++;
                        rect = new Rect(rect.xMin, rect.yMax + 2f, rect.width, EditorGUIUtility.singleLineHeight);
                        property.arraySize = Mathf.Max(0, EditorGUI.IntField(rect, "Size", property.arraySize));

                        var lbl = EditorHelper.TempContent("");
                        for (int i = 0; i < property.arraySize; i++)
                        {
                            var pchild = property.GetArrayElementAtIndex(i);
                            lbl.text = pchild.displayName;
                            var h = _drawer.GetPropertyHeight(pchild, lbl);
                            rect = new Rect(rect.xMin, rect.yMax + 2f, rect.width, h);
                            _drawer.OnGUI(rect, pchild, lbl);
                        }

                        EditorGUI.indentLevel--;
                    }
                    else
                    {
                        property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);
                    }
                }
            }

            #endregion

            #region IArrayHandlingPropertyDrawer Interface

            public PropertyDrawer InternalDrawer
            {
                get
                {
                    return _drawer;
                }
                set
                {
                    if (value != null) _drawer = value;
                }
            }

            #endregion

        }

        #endregion

    }

}
