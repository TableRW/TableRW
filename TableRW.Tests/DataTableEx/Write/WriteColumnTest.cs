using System.Data;
using TableRW.Write;
using TableRW.Write.DataTableEx;

namespace TableRW.Tests.DataTableEx.Write;

public class DataTableWriterTest {
    record EntityA(int Int1, int Int2, int? NullableInt, string Str1, DateTime Date);

    List<EntityA> DataList = new() {
        new(1, 11, 1000, "aaa", new(2023, 1, 1)),
        new(3, 33, null, "ccc", new(2023, 3, 3)),
        new(7, 77, 7000, "bbb", new(2023, 7, 7)),
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
    public void AddColumns() {
        var writer = new DataTblWriter<EntityA>()
            .AddColumns((s, e) => s(e.Int1, s.Skip(1), e.NullableInt, e.Str1));

        var writeLmd = writer.Lambda();
        var writeFn = writeLmd.Compile();
        writeFn(Tbl, DataList);

        Assert.Equal(DataList.Count, Tbl.Rows.Count);

        var iCol = 0;
        Assert.Equal(DataList[0].Int1, Tbl.Rows[0][iCol++]);
        Assert.Equal(DBNull.Value, Tbl.Rows[0][iCol++]);
        Assert.Equal(DataList[0].NullableInt, Tbl.Rows[0][iCol++]);
        Assert.Equal(DataList[0].Str1, Tbl.Rows[0][iCol++]);

        iCol = 0;
        Assert.Equal(DataList[1].Int1, Tbl.Rows[1][iCol++]);
        Assert.Equal(DBNull.Value, Tbl.Rows[1][iCol++]);
        Assert.Equal(DBNull.Value, Tbl.Rows[1][iCol++]);
        Assert.Equal(DataList[1].Str1, Tbl.Rows[1][iCol++]);
    }


    [Fact]
    public void AddColumns_Compute() {
        var writer = new DataTblWriter<EntityA>()
            .AddColumns((s, e) => s(
                e.Int1, e.Int1 + e.Int2, e.NullableInt + 1000,
                $"{e.Str1} -- {e.Date.Month}",
                "AA " + DateTime.Now.Month
            ));

        var writeLmd = writer.Lambda();
        var writeFn = writeLmd.Compile();
        writeFn(Tbl, DataList);

        Assert.Equal(DataList.Count, Tbl.Rows.Count);


        var (row, col) = (0, 0);
        var e = DataList[row];
        Assert.Equal(e.Int1, Tbl.Rows[row][col++]);
        Assert.Equal(e.Int1 + e.Int2, Tbl.Rows[row][col++]);
        Assert.Equal(e.NullableInt + 1000, Tbl.Rows[row][col++]);
        Assert.Equal($"{e.Str1} -- {e.Date.Month}", Tbl.Rows[row][col++]);
        Assert.Equal("AA " + DateTime.Now.Month, Tbl.Rows[row][col++]);

        (row, col) = (1, 0);
        e = DataList[row];
        Assert.Equal(e.Int1, Tbl.Rows[row][col++]);
        Assert.Equal(e.Int1 + e.Int2, Tbl.Rows[row][col++]);
        Assert.Equal(DBNull.Value, Tbl.Rows[row][col++]);
        Assert.Equal($"{e.Str1} -- {e.Date.Month}", Tbl.Rows[row][col++]);
        Assert.Equal("AA " + DateTime.Now.Month, Tbl.Rows[row][col++]);

        (row, col) = (2, 0);
        e = DataList[row];
        Assert.Equal(e.Int1, Tbl.Rows[row][col++]);
        Assert.Equal(e.Int1 + e.Int2, Tbl.Rows[row][col++]);
        Assert.Equal(e.NullableInt + 1000, Tbl.Rows[row][col++]);
        Assert.Equal($"{e.Str1} -- {e.Date.Month}", Tbl.Rows[row][col++]);
        Assert.Equal("AA " + DateTime.Now.Month, Tbl.Rows[row][col++]);

    }


    [Fact]
    public void AddSkipColumn() {
        var writer = new DataTblWriter<EntityA>()
            .AddSkipColumn(1)
            .AddColumns((s, e) => s(e.Int1))
            .AddSkipColumn(1)
            .AddColumns((s, e) => s(e.Str1))
            .AddSkipColumn(1)
            .AddAction(it => Assert.Equal(4, it.iCol))
            .AddAction(it => it.Row[it.iCol] = "E")
            .AddColumn(it => it.Row[it.iCol] = it.Entity.Str1);

        var writeLmd = writer.Lambda();
        var fn = writeLmd.Compile();
        fn(Tbl, DataList);

        Assert.Equal(DataList.Count, Tbl.Rows.Count);

        for (var i = 0; i < DataList.Count; i++) {
            var col = 0;
            var e = DataList[i];
            Assert.Equal(DBNull.Value, Tbl.Rows[i][col++]);
            Assert.Equal(e.Int1, Tbl.Rows[i][col++]);
            Assert.Equal(DBNull.Value, Tbl.Rows[i][col++]);
            Assert.Equal(e.Str1, Tbl.Rows[i][col++]);
            Assert.Equal("E", Tbl.Rows[i][col++]);
            Assert.Equal(e.Str1, Tbl.Rows[i][col++]);
        }

    }

    [Fact]
    public void AddActionWrite() {
        var writer = new DataTblWriter<EntityA>()
            .AddAction(it => it.Row[it.iCol] = 1111)
            .AddSkipColumn(2)
            .AddColumns((s, e) => s(e.Int1, e.Str1))
            .AddAction(it => it.Row[it.iCol] += "-A")
            .AddColumn(it => it.Row[it.iCol] = it.Entity.Str1)
            .AddAction(it => Assert.Equal(4, it.iCol));

        var writeLmd = writer.Lambda();
        var fn = writeLmd.Compile();
        fn(Tbl, DataList);

        Assert.Equal(DataList.Count, Tbl.Rows.Count);

        for (var i = 0; i < DataList.Count; i++) {
            var col = 0;
            var e = DataList[i];
            Assert.Equal(1111, Tbl.Rows[i][col++]);
            Assert.Equal(DBNull.Value, Tbl.Rows[i][col++]);
            Assert.Equal(e.Int1, Tbl.Rows[i][col++]);
            Assert.Equal(e.Str1 + "-A", Tbl.Rows[i][col++]);
            Assert.Equal(e.Str1, Tbl.Rows[i][col++]);
        }
    }

    [Fact]
    public void SetStart() {
        Tbl.Rows.Add(1, 2, 3);
        var (startRow, startCol) = (1, 2);
        var writer = new DataTblWriter<EntityA>()
            .SetStart(startRow, startCol)
            .AddColumns((s, e) => s(e.Int1, e.Str1));

        var writeLmd = writer.Lambda();
        var fn = writeLmd.Compile();
        fn(Tbl, DataList);

        Assert.Equal(DataList.Count + startRow, Tbl.Rows.Count);

        var col = 0;
        Assert.Equal(1, Tbl.Rows[0][col++]);
        Assert.Equal(2, Tbl.Rows[0][col++]);
        Assert.Equal(3, Tbl.Rows[0][col++]);

        for (var i = 0; i < DataList.Count; i++) {
            col = 0;
            var e = DataList[i];
            i += startRow;
            Assert.Equal(DBNull.Value, Tbl.Rows[i][col++]);
            Assert.Equal(DBNull.Value, Tbl.Rows[i][col++]);
            Assert.Equal(e.Int1, Tbl.Rows[i][col++]);
            Assert.Equal(e.Str1, Tbl.Rows[i][col++]);
        }
    }


}
