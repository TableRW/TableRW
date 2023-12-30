
using System.Data;
using TableRW.Utils.Ex;

namespace TableRW.Read.DataTableEx;

public static class DataTableEx {
    public static List<TEntity> ReadToList<TEntity>(this DataTable tbl)
    where TEntity : new() {
        if (CacheReadFn<TEntity>.FnUseHeader == null) {
            var colNames = tbl.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToHashSet();
            var reader = new DataTblReader<TEntity>();
            var t_entity = typeof(TEntity);

            t_entity.GetProperties().Where(p => colNames.Contains(p.Name) && p.CanWrite)
                .Cast<MemberInfo>()
                .Concat(t_entity.GetFields().Where(f => colNames.Contains(f.Name) && !f.IsInitOnly))
                .Where(member => member.HasAttribute<IgnoreReadAttribute>() == false)
                .ForEach(member => reader.AddColumn(member, member.Name));

            var readLmd = reader.Lambda();
            CacheReadFn<TEntity>.FnUseHeader = readLmd.Compile();
        }
        return CacheReadFn<TEntity>.FnUseHeader(tbl);

    }

    public static List<TEntity> ReadToList<TEntity>(
        this DataTable tbl,
        int cacheKey,
        Func<DataTblReader<TEntity>, Func<DataTable, List<TEntity>>> buildRead
    ) where TEntity : new() {
        if (CacheReadFn<TEntity>.DicFn is var dic && !dic.TryGetValue(cacheKey, out var fn)) {
            dic[cacheKey] = fn = buildRead(new());
        }

        return fn(tbl);
    }

    public static List<TEntity> ReadToList<TEntity, TData>(
        this DataTable tbl,
        int cacheKey,
        Func<DataTblReader<TEntity, TData>, Func<DataTable, List<TEntity>>> buildRead
    ) where TEntity : new() {
        if (CacheReadFn<TEntity>.DicFn is var dic && !dic.TryGetValue(cacheKey, out var fn)) {
            dic[cacheKey] = fn = buildRead(new());
        }

        return fn(tbl);
    }

}

static class CacheReadFn<T> {
    internal static Func<DataTable, List<T>>? FnUseHeader;

    internal static Dictionary<int, Func<DataTable, List<T>>> DicFn = new();
}
