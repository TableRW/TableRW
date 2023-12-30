using System.Data;
using E = System.Linq.Expressions.Expression;

namespace TableRW.Write.I.DataTableEx;

public class DataTblWriterImpl<C> : TableWriterImpl<C> {

    static DataTblWriterImpl() {
        WriteSource<DataTable>.Impl(WriteSrcValue, InitRow);
    }

    public static Expression[] InitRow(Expression ctx) {
        var row = E.Property(ctx, "Row");
        var src = E.Property(ctx, "Src");

        return [
            // ctx.Row = ctx.Src.NewRow()
            E.Assign(row, E.Call(src, "NewRow", [])),
            // ctx.Src.Rows.Add(ctx.Row)
            E.Call(E.Property(src, "Rows"), "Add", [], row),
        ];
    }


    public static Expression WriteSrcValue(Expression ctx, Expression value) {
        var convertVal = value;
        if (value.Type.IsValueType) {
            convertVal = E.Convert(value, typeof(object));

            if (Nullable.GetUnderlyingType(value.Type) != null) {
                // (object)value ?? DBNull.Value
                convertVal = E.Coalesce(convertVal, E.Constant(DBNull.Value));
            }
        }

        var set = // ctx.Row[ctx.iCol] = (object)value
            E.Call(E.Property(ctx, "Row"),
            "set_Item", [], E.Property(ctx, "iCol"), convertVal);
        return set;
    }

}

