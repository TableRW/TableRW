using System.Data;
using TableRW.Read.I;
using TableRW.Read.I.DataTableEx;

namespace TableRW.Read.DataTableEx;

public static class TableReaderEx {

    public static ITableReader<C> AddColumn<C>(
        this ITableReader<C, ISource<DataTable>> reader,
        MemberInfo prop,
        string columnName
    ) {
        var r = reader.IntoImpl();
        var propType = r.CheckMemberType(prop);
        var value = DataTblReaderImpl<C>.ReadSrcValueByName(r.Ctx.Context, propType, columnName);

        r.ReadSeq.AddColumnRead(prop, value);
        return r;
    }

    public static ITableReader<C> AddMapHeaderColumns<C, TEntity>(
       this ITableReader<C, IContext<DataTable, TEntity>> r,
       Expression<Func<DParamsConst<string>, DParams>> headerNames,
       Expression<Func<DParams, TEntity, DParams>> members
    ) {
        members.GetEntityMembers()
            .Zip(headerNames.GetParams(), r.AddColumn)
            .Count();

        return r.IntoImpl();
    }
}