using System.Data;
using E = System.Linq.Expressions.Expression;

namespace TableRW.Read.I.DataTableEx;


public class DataTblReaderImpl<C> : TableReaderImpl<C> {

    static DataTblReaderImpl() {
        ReadSource<DataTable>.Impl(
            ReadSrcValueByIndex,
            (src, iRow) => iRow >= src.Rows.Count,
            (src, iRow, iCol) => src.Rows[iRow][iCol] is DBNull);
    }

    public static Expression ReadSrcValueByIndex(Expression ctx, Type valueType)
        => ConvertSrcValue(GetSrcValueByIndex(ctx), valueType);

    public static Expression ReadSrcValueByName(Expression ctx, Type valueType, string colName)
        => ConvertSrcValue(GetSrcValueByName(ctx, colName), valueType);

    public static Expression GetSrcValueByIndex(Expression ctx)
        => Utils.Expr.ExtractBody((ISource<DataTable> ctx) => ctx.Src.Rows[ctx.iRow][ctx.iCol], ctx);

    public static Expression GetSrcValueByName(Expression ctx, string colName)
        => Utils.Expr.ExtractBody((ISource<DataTable> ctx, string name) => ctx.Src.Rows[ctx.iRow][name], ctx, colName);


    public static Expression ConvertSrcValue(Expression srcValue, Type valueType) {
        if (valueType == typeof(object)) { return srcValue; }

        if (Nullable.GetUnderlyingType(valueType) != null) {
            var e_srcVal = E.Variable(typeof(object), "srcValue");
            var e_srcVal_assign = E.Assign(e_srcVal, srcValue);

            // srcValue is DBNull ? null : srcValue
            var e_convertDbNull = E.Condition(
                E.TypeIs(e_srcVal, typeof(DBNull)),
                E.Constant(null, valueType),
                E.Convert(e_srcVal, valueType));

            return E.Block(valueType, new[] { e_srcVal }, e_srcVal_assign, e_convertDbNull);
        } else {
            return E.Convert(srcValue, valueType);
        }
    }
}

