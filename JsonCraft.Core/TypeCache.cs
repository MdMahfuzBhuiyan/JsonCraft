namespace JsonCraft.Core;
using System.Collections.Concurrent;
using System.Reflection;
internal static class TypeCache
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> ReadableProperties = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> WritableProperties = new();

    public static PropertyInfo[] GetReadableProperties(Type type)
    {
        return ReadableProperties.GetOrAdd(type, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && p.GetIndexParameters().Length == 0).ToArray());
    }
    public static PropertyInfo[] GetWritableProperties(Type type)
    {
        return WritableProperties.GetOrAdd(type, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite && p.GetIndexParameters().Length == 0).ToArray());
    }
}