using System.Data;

namespace TableRW.Read.I.DataTableEx;

public class Context<TEntity>(DataTable src)
: I.Context<DataTable, TEntity>(src) { }

public class Context<TEntity, TData>(DataTable src)
: I.Context<DataTable, TEntity, TData>(src) { }