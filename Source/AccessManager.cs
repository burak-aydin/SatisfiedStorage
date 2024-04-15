using JetBrains.Annotations;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace SatisfiedStorage
{
    public static class AccessManager
    {
        public static Func<T, P> GetPropertyGetter<T, P>([NotNull] string propertyName)
        {
            MethodInfo mi = typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true);
            DynamicMethod dm = new DynamicMethod(String.Empty, typeof(P), new[] { typeof(T) }, typeof(T));
            var ilGen = dm.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Callvirt, mi);
            ilGen.Emit(OpCodes.Ret);

            return (Func<T, P>)dm.CreateDelegate(typeof(Func<T, P>));
        }

        public static Func<P> GetPropertyGetter<P>([NotNull] string propertyName, [NotNull] Type ownerType)
        {
            MethodInfo mi = ownerType.GetProperty(propertyName, BindingFlags.Static | BindingFlags.NonPublic).GetGetMethod(true);
            DynamicMethod dm = new DynamicMethod(String.Empty, typeof(P), Type.EmptyTypes, ownerType);
            var ilGen = dm.GetILGenerator();
            ilGen.Emit(OpCodes.Callvirt, mi);
            ilGen.Emit(OpCodes.Ret);

            return (Func<P>)dm.CreateDelegate(typeof(Func<P>));
        }

        public static Func<T, F> GetFieldGetter<T, F>([NotNull] string fieldName)
        {
            FieldInfo fi = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            DynamicMethod dm = new DynamicMethod(String.Empty, typeof(F), new[] { typeof(T) }, typeof(T));
            var ilGen = dm.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, fi);
            ilGen.Emit(OpCodes.Ret);

            return (Func<T, F>)dm.CreateDelegate(typeof(Func<T, F>));
        }

        public static Func<F> GetFieldGetter<F>([NotNull] string fieldName, Type ownerType)
        {
            FieldInfo fi = ownerType.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
            DynamicMethod dm = new DynamicMethod(String.Empty, typeof(F), Type.EmptyTypes, ownerType);
            var ilGen = dm.GetILGenerator();
            ilGen.Emit(OpCodes.Ldfld, fi);
            ilGen.Emit(OpCodes.Ret);

            return (Func<F>)dm.CreateDelegate(typeof(Func<F>));
        }
    }
}