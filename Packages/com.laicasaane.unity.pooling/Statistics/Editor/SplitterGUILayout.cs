using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace System.Pooling.Statistics.Editor
{
        // reflection call of UnityEditor.SplitterGUILayout
    internal static class SplitterGUILayout
    {
        static readonly BindingFlags s_flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        static readonly Lazy<Type> s_splitterStateType = new Lazy<Type>(() =>
        {
            var type = typeof(EditorWindow).Assembly.GetTypes().First(x => x.FullName == "UnityEditor.SplitterState");
            return type;
        });

        static readonly Lazy<ConstructorInfo> s_splitterStateCtor = new Lazy<ConstructorInfo>(() =>
        {
            var type = s_splitterStateType.Value;
            return type.GetConstructor(s_flags, null, new Type[] { typeof(float[]), typeof(int[]), typeof(int[]) }, null);
        });

        static readonly Lazy<Type> s_splitterGUILayoutType = new Lazy<Type>(() =>
        {
            var type = typeof(EditorWindow).Assembly.GetTypes().First(x => x.FullName == "UnityEditor.SplitterGUILayout");
            return type;
        });

        static readonly Lazy<MethodInfo> s_beginVerticalSplit = new Lazy<MethodInfo>(() =>
        {
            var type = s_splitterGUILayoutType.Value;
            return type.GetMethod("BeginVerticalSplit", s_flags, null, new Type[] { s_splitterStateType.Value, typeof(GUILayoutOption[]) }, null);
        });

        static readonly Lazy<MethodInfo> s_endVerticalSplit = new Lazy<MethodInfo>(() =>
        {
            var type = s_splitterGUILayoutType.Value;
            return type.GetMethod("EndVerticalSplit", s_flags, null, Type.EmptyTypes, null);
        });

        public static object CreateSplitterState(float[] relativeSizes, int[] minSizes, int[] maxSizes)
        {
            return s_splitterStateCtor.Value.Invoke(new object[] { relativeSizes, minSizes, maxSizes });
        }

        public static void BeginVerticalSplit(object splitterState, params GUILayoutOption[] options)
        {
            s_beginVerticalSplit.Value.Invoke(null, new object[] { splitterState, options });
        }

        public static void EndVerticalSplit()
        {
            s_endVerticalSplit.Value.Invoke(null, Type.EmptyTypes);
        }
    }
}