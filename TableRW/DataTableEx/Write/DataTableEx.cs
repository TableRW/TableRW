
using System.Data;
using TableRW.Utils.Ex;

namespace TableRW.Write.DataTableEx;

public static class DataTableEx {

    public static void WriteFrom<TEntity>(
        this DataTable tbl,
        IEnumerable<TEntity> enumerable,
        int cacheKey,
        Func<DataTblWriter<TEntity>, Action<DataTable, IEnumerable<TEntity>>> buildWrite
    ) {
        if (CacheFn<TEntity>.DicFn is var dic && !dic.TryGetValue(cacheKey, out var fn)) {
            dic[cacheKey] = fn = buildWrite(new());
        }

        fn(tbl, enumerable);
    }

    public static void WriteFrom<TEntity, TData>(
        this DataTable tbl,
        IEnumerable<TEntity> enumerable,
        int cacheKey,
        Func<DataTblWriter<TEntity, TData>, Action<DataTable, IEnumerable<TEntity>>> buildWrite
    ) {
        if (CacheFn<TEntity>.DicFn is var dic && !dic.TryGetValue(cacheKey, out var fn)) {
            dic[cacheKey] = fn = buildWrite(new());
        }

        fn(tbl, enumerable);
    }

}

static class CacheFn<T> {
    internal static Dictionary<int, Action<DataTable, IEnumerable<T>>> DicFn = new();
}
