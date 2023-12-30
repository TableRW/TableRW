using TableRW.Write.I.DataTableEx;

namespace TableRW.Write.DataTableEx;

public class DataTblWriter<TEntity>
: DataTblWriterImpl<Context<TEntity>> { }

public class DataTblWriter<TEntity, TData>
: DataTblWriterImpl<Context<TEntity, TData>> { }
