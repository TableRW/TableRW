using System.Data;

namespace TableRW.Write.I.DataTableEx;

public class Context<TEntity>(DataTable src)
: I.Context<DataTable, DataRow, TEntity>(src) { }

public class Context<TEntity, TData>(DataTable src)
: I.Context<DataTable, DataRow, TEntity, TData>(src) { }
