#if !ENABLE_IL2CPP

using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Special class that tells the JIT to apply optimizations such as compiling with float32 instead
    /// of using doubles. This provides a huge performance boost to all methods in this package.
    /// <para>Shoutouts to DaZombieKiller for this genius discovery.</para>
    /// </summary>
    internal static class MonoCodegenHandler
    {
        [DllImport("__Internal")]
        private static extern unsafe void mono_jit_parse_options(int argc, byte** argv);

        [DllImport("__Internal")]
        private static extern unsafe void* mono_compile_method(void* method);

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        private static unsafe void Initialize()
        {
            // enable special jit compiler instructions:
            //
            // float32: use 32 bit float arithmetic if possible.
            // abcrem: array bound checks removal.
            //
            fixed (byte* arg1 = Encoding.UTF8.GetBytes("-O=float32,abcrem\0"))
                mono_jit_parse_options(1, &arg1);

            // find every single type in the current assembly:
            var assemblyTypes = typeof(MonoCodegenHandler).Assembly.GetTypes();
            for (int i = 0; i < assemblyTypes.Length; i++)
            {
                var assemblyType = assemblyTypes[i];

                // find every single method in the type:
                var methods = assemblyType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                for (int j = 0; j < methods.Length; j++)
                {
                    var method = methods[j];

                    if (method.IsGenericMethod) continue;

                    // tell jit to compile this method with our special flags:
                    mono_compile_method((void*)method.MethodHandle.Value);
                }
            }

            // disabling everything to leave other user code outside of this package alone.
            fixed (byte* arg2 = Encoding.UTF8.GetBytes("-O=-float32,-abcrem\0"))
                mono_jit_parse_options(1, &arg2);
        }
    }
}

#endif