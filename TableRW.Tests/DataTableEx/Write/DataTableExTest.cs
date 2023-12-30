using System.Data;
using TableRW.Write;
using TableRW.Write.DataTableEx;

namespace TableRW.Tests.DataTableEx.Write;

public class DataTableExTest {

    record EntityA(int Int1, int Int2, int? NullableInt, string Str1, DateTime Date);

    List<EntityA> DataList = new() {
        new(10, 11, 1000, "aaa", new(2023, 1, 1)),
        new(30, 33, 3000, "ccc", new(2023, 3, 3)),
        new(70, 77, 7000, "bbb", new(2023, 7, 7)),
    };

    DataTable Tbl = new() {
        Columns = {
            { "A", typeof(int) },
            { "B", typeof(int) },
            { "C", typeof(int) },
            { "D", typeof(string) },
            { "E", typeof(string) },
            { "F", typeof(string) },
        },
    };

    [Fact]
    public void WriteFrom() {
        Tbl.WriteFrom(DataList, cacheKey: 0, writer => {
            writer.AddColumns((s, e) =>
                s(e.Int1, s.Skip(1), e.NullableInt, e.Str1));

            var lmd = writer.Lambda();
            return lmd.Compile();
        });

        Assert.Equal(DataList.Count, Tbl.Rows.Count);
        for (var i = 0; i < DataList.Count; i++) {
            var col = 0;
            var e = DataList[i];
            Assert.Equal(e.Int1, Tbl.Rows[i][col++]);
            Assert.Equal(DBNull.Value, Tbl.Rows[i][col++]);
            Assert.Equal(e.NullableInt, Tbl.Rows[i][col++]);
            Assert.Equal(e.Str1, Tbl.Rows[i][col++]);
            Assert.Equal(DBNull.Value, Tbl.Rows[i][col++]);
        }
    }

    [Fact]
    public void WriteFrom_AnotherKey() {
        Tbl.WriteFrom(DataList, cacheKey: 1, writer => {
            writer.AddColumns((s, e) =>
                s(e.Int1, s.Skip(1), e.NullableInt, e.Str1));

            var lmd = writer.Lambda();
            return lmd.Compile();
        });
        Tbl.Rows.Clear();

        // 使用另一种方式，和上面的缓存不同
        Tbl.WriteFrom(DataList, cacheKey: 2, writer => {
            writer.AddColumns((s, e) =>
                s(e.Int2, e.Int1, s.Skip(2), e.Str1));

            var lmd = writer.Lambda();
            return lmd.Compile();
        });

        Assert.Equal(DataList.Count, Tbl.Rows.Count);
        for (var i = 0; i < DataList.Count; i++) {
            var col = 0;
            var e = DataList[i];
            Assert.Equal(e.Int2, Tbl.Rows[i][col++]);
            Assert.Equal(e.Int1, Tbl.Rows[i][col++]);
            Assert.Equal(DBNull.Value, Tbl.Rows[i][col++]);
            Assert.Equal(DBNull.Value, Tbl.Rows[i][col++]);
            Assert.Equal(e.Str1, Tbl.Rows[i][col++]);
            Assert.Equal(DBNull.Value, Tbl.Rows[i][col++]);
        }
    }

    [Fact]
    public void WriteFrom_WithData() {
        Tbl.WriteFrom<EntityA, (int, string)>(DataList, cacheKey: 3, writer => {
            writer
                .InitData(src => (src.Columns.Count, src.Columns[0].ColumnName))
                .AddColumn(it => it.Row[it.iCol] = it.Data.Item1)
                .AddColumns((s, e) => s(e.Int1, e.Int2, e.Str1))
                .AddColumn(it => it.Row[it.iCol] = it.Data.Item2);

            var lmd = writer.Lambda();
            return lmd.Compile();
        });

        Assert.Equal(DataList.Count, Tbl.Rows.Count);
        for (var i = 0; i < DataList.Count; i++) {
            var col = 0;
            var e = DataList[i];
            Assert.Equal(Tbl.Columns.Count, Tbl.Rows[i][col++]);
            Assert.Equal(e.Int1, Tbl.Rows[i][col++]);
            Assert.Equal(e.Int2, Tbl.Rows[i][col++]);
            Assert.Equal(e.Str1, Tbl.Rows[i][col++]);
            Assert.Equal(Tbl.Columns[0].ColumnName, Tbl.Rows[i][col++]);
        }
    }

}
