using System.Data;
using TableRW.Write;
using TableRW.Write.DataTableEx;

namespace TableRW.Tests.DataTableEx.Write;
public class EventTest {

    record EntityA(int Int1, int Int2, int? NullableInt, string Str1, DateTime Date);

    List<EntityA> DataList = new() {
        new(1, 11, 1000, "aaa", new(2021, 1, 11)),
        new(3, 33, null, "ccc", new(2022, 3, 13)),
        new(7, 77, 7000, "bbb", new(2023, 7, 17)),
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
    public void StartWritingTable() {
        var writer = new DataTblWriter<EntityA, int>()
            .SetStart(0, 1)
            .OnStartWritingTable(it => {
                Assert.Equal(1, it.iCol);
                it.Data = it.Src.Columns.Count;
            })
            .AddColumn(it => it.Row[it.iCol] = it.Data)
            .AddColumns((s, e) => s(e.Int1, e.Str1));

        var writeLmd = writer.Lambda();
        var writeFn = writeLmd.Compile();
        writeFn(Tbl, DataList);

        Assert.Equal(DataList.Count, Tbl.Rows.Count);

        for (var i = 0; i < DataList.Count; i++) {
            var col = 0;
            var e = DataList[i];
            Assert.Equal(DBNull.Value, Tbl.Rows[i][col++]);
            Assert.Equal(Tbl.Columns.Count, Tbl.Rows[i][col++]);
            Assert.Equal(e.Int1, Tbl.Rows[i][col++]);
            Assert.Equal(e.Str1, Tbl.Rows[i][col++]);
        }
    }

    [Fact]
    public void StartWritingRow() {
        var writer = new DataTblWriter<EntityA>()
            .SetStart(0, 1)
            .OnStartWritingRow(it => it.Row[it.iCol - 1] = 222)
            .AddColumns((s, e) => s(e.Int1, e.Int2, e.Str1));

        var writeLmd = writer.Lambda();
        var writeFn = writeLmd.Compile();
        writeFn(Tbl, DataList);

        Assert.Equal(DataList.Count, Tbl.Rows.Count);

        for (var i = 0; i < DataList.Count; i++) {
            var col = 0;
            var e = DataList[i];
            Assert.Equal(222, Tbl.Rows[i][col++]);
            Assert.Equal(e.Int1, Tbl.Rows[i][col++]);
            Assert.Equal(e.Int2, Tbl.Rows[i][col++]);
            Assert.Equal(e.Str1, Tbl.Rows[i][col++]);
        }
    }

    [Fact]
    public void EndWritingRow() {
        var writer = new DataTblWriter<EntityA>()
            .AddColumns((s, e) => s(e.Int1, e.Int2, e.Date.Day))
            .OnEndWritingRow(it => it.Row[it.iCol] = 222);

        var writeLmd = writer.Lambda();
        var writeFn = writeLmd.Compile();
        writeFn(Tbl, DataList);

        Assert.Equal(DataList.Count, Tbl.Rows.Count);

        for (var i = 0; i < DataList.Count; i++) {
            var col = 0;
            var e = DataList[i];
            Assert.Equal(e.Int1, Tbl.Rows[i][col++]);
            Assert.Equal(e.Int2, Tbl.Rows[i][col++]);
            Assert.Equal(222, Tbl.Rows[i][col++]);
        }
    }

    [Fact]
    public void EndWritingTable() {
        var writer = new DataTblWriter<EntityA>()
            .AddColumns((s, e) => s(e.Int1, e.Int2, e.Date.Day))
            .OnEndWritingTable(it => it.Row[it.iCol + 1] = "333");

        var writeLmd = writer.Lambda();
        var writeFn = writeLmd.Compile();
        writeFn(Tbl, DataList);

        Assert.Equal(DataList.Count, Tbl.Rows.Count);

        for (var i = 0; i < DataList.Count; i++) {
            var col = 0;
            var e = DataList[i];
            Assert.Equal(e.Int1, Tbl.Rows[i][col++]);
            Assert.Equal(e.Int2, Tbl.Rows[i][col++]);
            Assert.Equal(e.Date.Day, Tbl.Rows[i][col++]);
        }
        Assert.Equal("333", Tbl.Rows[DataList.Count - 1][3]);
    }
}
