namespace TableRW.Utils.Ex;
internal static class EnumerableEx {

    internal static void ForEach<T>(this IEnumerable<T> src, Action<T> action) {
        foreach (var item in src) { action(item); }
    }

    public static void ForEach<T>(this IEnumerable<T> src, Func<T, object?> action) {
        foreach (var item in src) { action(item); }
    }

    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> src) => new(src);
    
    // internal static IEnumerable<T> AddNotNull<T>(this IEnumerable<T> src, T? element) {
    //     if (element == null) { return src; }
    //
    //     return src.Concat(new[] { element });
    // }

    internal static IEnumerable<T> ExcludeNull<T>(this IEnumerable<T?> src) {
        return src.Where(x => x != null)!;
    }

    internal static IEnumerable<T> ReplaceLastElement<T>(this ICollection<T> src, T element) {
        return src.Take(src.Count - 1).Concat(new[] { element });
    }

}
