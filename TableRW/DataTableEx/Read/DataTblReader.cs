using TableRW.Read.I.DataTableEx;

namespace TableRW.Read.DataTableEx;


// There already exists a name `System.Data.DataTableReader`,
// which is prone to conflicts, so use `DataTblReader`
public class DataTblReader<TEntity> : DataTblReaderImpl<Context<TEntity>> { }

public class DataTblReader<TEntity, TData> : DataTblReaderImpl<Context<TEntity, TData>> { }
