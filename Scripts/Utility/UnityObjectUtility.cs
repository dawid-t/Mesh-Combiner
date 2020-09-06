#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Ludiq
{
    public static class UnityObjectUtility
    {		
        public static UnityObject GetPrefabDefinition(this UnityObject uo)
        {
            return PrefabUtility.GetCorrespondingObjectFromSource(uo);
        }

        public static bool IsPrefabInstance(this UnityObject uo)
        {
            return GetPrefabDefinition(uo) != null;
        }

        public static bool IsPrefabDefinition(this UnityObject uo)
        {
            return GetPrefabDefinition(uo) == null && PrefabUtility.GetPrefabInstanceHandle(uo) != null;
        }

        public static bool IsConnectedPrefabInstance(this UnityObject go)
        {
            return IsPrefabInstance(go) && PrefabUtility.GetPrefabInstanceHandle(go) != null;
        }

        public static bool IsDisconnectedPrefabInstance(this UnityObject go)
        {
            return IsPrefabInstance(go) && PrefabUtility.GetPrefabInstanceHandle(go) == null;
        }

        public static bool IsSceneBound(this UnityObject uo)
        {
            return
                (uo is GameObject && !IsPrefabDefinition(uo)) ||
                (uo is Component && !IsPrefabDefinition(((Component)uo).gameObject));
        }
    }
}

#endif