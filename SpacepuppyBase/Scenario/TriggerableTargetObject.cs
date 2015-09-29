﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using com.spacepuppy.Utils;

namespace com.spacepuppy.Scenario
{

    [System.Serializable()]
    public class TriggerableTargetObject
    {

        #region Fields

        [SerializeField()]
        private TargetSource _source;
        [SerializeField()]
        private UnityEngine.Object _target;

        #endregion

        #region CONSTRUCTOR

        public TriggerableTargetObject()
        {
        }

        public TriggerableTargetObject(TargetSource src)
        {
            _source = src;
        }

        #endregion

        #region Properties

        public TargetSource Source
        {
            get { return _source; }
        }

        #endregion

        #region Methods

        public T GetTarget<T>(object triggerArg, bool searchEntity = true) where T : class
        {
            return GetTargetFrom(typeof(T), this.ReduceTarget(triggerArg), searchEntity) as T;
        }

        public object GetTarget(System.Type tp, object triggerArg, bool searchEntity = true)
        {
            if (tp == null || !TypeUtil.IsType(tp, typeof(UnityEngine.Object))) throw new TypeArgumentMismatchException(tp, typeof(UnityEngine.Object), "tp");

            return GetTargetFrom(tp, this.ReduceTarget(triggerArg), searchEntity);
        }

        private UnityEngine.Object ReduceTarget(object triggerArg)
        {
            switch (_source)
            {
                case TargetSource.TriggerArg:
                    return (triggerArg is UnityEngine.Object) ? triggerArg as UnityEngine.Object : null;
                case TargetSource.Self:
                case TargetSource.Root:
                case TargetSource.Configurable:
                    return _target;
            }

            return null;
        }

        #endregion

        #region Utils

        public static T GetTargetFrom<T>(object targ, bool searchEntity) where T : class
        {
            return GetTargetFrom(typeof(T), targ, searchEntity) as T;
        }

        public static object GetTargetFrom(System.Type tp, object targ, bool searchEntity)
        {
            if (targ == null) return null;

            if (targ != null && TypeUtil.IsType(targ.GetType(), tp))
            {
                return targ;
            }
            else
            {
                if (searchEntity)
                {
                    if (tp == typeof(GameObject))
                    {
                        return GameObjectUtil.GetRootFromSource(targ);
                    }
                    else if (TypeUtil.IsType(tp, typeof(SPEntity)))
                    {
                        return SPEntity.GetEntityFromSource(targ);
                    }
                    else
                    {
                        return GameObjectUtil.GetGameObjectFromSource(targ).FindComponent(tp);
                    }
                }
                else
                {
                    if (tp == typeof(GameObject))
                    {
                        return GameObjectUtil.GetGameObjectFromSource(targ);
                    }
                    else if (TypeUtil.IsType(tp, typeof(SPEntity)))
                    {
                        return SPEntity.GetEntityFromSource(targ);
                    }
                    else
                    {
                        var go = GameObjectUtil.GetGameObjectFromSource(targ);
                        if (go != null)
                            return go.GetComponent(tp);
                        else
                            return null;
                    }
                }
            }
        }

        #endregion

        #region Special Types

        public enum TargetSource
        {
            TriggerArg = 0,
            Self = 1,
            Root = 2,
            Configurable = 3
        }

        public class ConfigAttribute : System.Attribute
        {

            public System.Type TargetType;

            public ConfigAttribute()
            {
                this.TargetType = typeof(GameObject);
            }

            public ConfigAttribute(System.Type targetType)
            {
                //if (targetType == null || 
                //    (!TypeUtil.IsType(targetType, typeof(UnityEngine.Object)) && !TypeUtil.IsType(targetType, typeof(IComponent)))) throw new TypeArgumentMismatchException(targetType, typeof(UnityEngine.Object), "targetType");
                if (targetType == null ||
                    (!TypeUtil.IsType(targetType, typeof(UnityEngine.Object)) && !targetType.IsInterface))
                    throw new TypeArgumentMismatchException(targetType, typeof(UnityEngine.Object), "targetType");

                this.TargetType = targetType;
            }

        }

        #endregion

    }
}
