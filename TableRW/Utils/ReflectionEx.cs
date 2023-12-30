namespace TableRW.Utils.Ex;

public static class ReflectionEx {

    internal static bool EqualType(this MemberInfo left, MemberInfo? right)
    => (left.MetadataToken, left.Module) == (right?.MetadataToken, right?.Module);


    public static bool HasAttribute<T>(this MemberInfo member, bool inherit = true) where T : Attribute {
        return member.GetCustomAttributes(typeof(T), inherit).Any();
    }

    internal static IEnumerable<PropertyInfo> GetPropertiesOfAttribute<T>(this Type entity) where T : Attribute {
        return entity.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.HasAttribute<T>());
    }

    internal static IEnumerable<FieldInfo> GetFieldsOfAttribute<T>(this Type entity) where T : Attribute {
        return entity.GetFields(BindingFlags.Instance | BindingFlags.Public)
            .Where(f => f.HasAttribute<T>());
    }

    internal static PropertyInfo? GetInterfaceProp(this Type type, string interfaceName, string propName)
    => type.GetProperty(propName)
    ?? type.GetInterface(interfaceName)?.GetProperty(propName);


    internal static bool IsGenericTypeDefinitionOf(this Type type, Type definition) {
        return type.IsGenericType && type.GetGenericTypeDefinition() == definition;
    }

    //public static bool IsInitOnly(this PropertyInfo prop) {
    //    if (prop.CanWrite == false) { return false; }
    //    var setMethod = prop.GetSetMethod()!;
    //    return setMethod.ReturnParameter
    //        .GetRequiredCustomModifiers()
    //        .Contains(typeof(System.Runtime.CompilerServices.IsExternalInit));
    //}
}
