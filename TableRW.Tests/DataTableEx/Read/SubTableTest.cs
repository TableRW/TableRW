using System.Data;
using TableRW.Read;
using TableRW.Read.DataTableEx;

namespace TableRW.Tests.DataTableEx.Read;


public class SubTableTest {
    class DataA {
        public int Id { get; set; }
        public string? Name { get; set; }
        public List<SubB> SubList { get; set; } = new();
    }

    class SubB {
        public int Id { get; set; }
        public string? Text { get; set; }
        public List<SubC> SubCList { get; set; } = new();
    }

    class SubC {
        public int Id { get; set; }
        public int NumC { get; set; }
    }

    static readonly DataTable TblLevel2 = new DataTable("2Level") {
        Columns = {
            { "Id", typeof(int) },
            { "Name", typeof(string) },
            { "SubBId", typeof(int) },
            { "SubBText", typeof(string) },
            { "SubCId", typeof(int) },
            { "SubCNum", typeof(int) },
        },
        Rows = {
            { 1000, "AA", 1, "Saa1" },
            { 1000, "AA", 2, "Saa2" },
            { 1000, "AA", 3, "Saa3" },

            { 2000, "BB", 1, "Sbb" },

            { 3000, "CC", 1, "Sccc" },
            { 3000, "CC", 2, "Sccc" },
        }
    };

    static void CheckRead_TblLevel2(List<DataA> list) {
        Assert.Equal(3, list.Count);

        Assert.Equal(1000, list[0].Id);
        Assert.Equal("AA", list[0].Name);
        Assert.Equal(3, list[0].SubList.Count);
        Assert.Equal(1, list[0].SubList[0].Id);
        Assert.Equal("Saa1", list[0].SubList[0].Text);

        Assert.Equal(2000, list[1].Id);
        Assert.Equal("BB", list[1].Name);
        Assert.Equal(1, list[1].SubList.Count);

        Assert.Equal(3000, list[2].Id);
        Assert.Equal("CC", list[2].Name);
        Assert.Equal(2, list[2].SubList.Count);
    }

    [Fact]
    public void Level2_AddSubTable() {
        var reader = new DataTblReader<DataA>()
            .AddColumns((s, e) => s(s.RowKey(e.Id), e.Name))
            .AddSubTable(e => e.SubList, (s, e) => s(e.Id, e.Text));

        var readLmd = reader.Lambda();
        var readFn = readLmd.Compile();
        var list = readFn(TblLevel2);
        CheckRead_TblLevel2(list);
    }

    //[Fact]
    //public void Level2_AddSubTable_SelectExpr() {
    //    var reader = new DataTblReader<DataA>()
    //        .AddColumns((s, e) => s.RowKey(e.Id)(e.Name)
    //        .SubTable(e.SubList, e => s(e.Id, e.Text)));
    //
    //    var readLmd = reader.Lambda();
    //    var readFn = readLmd.Compile();
    //    var list = readFn(TblLevel2);
    //    CheckRead_TblLevel2(list);
    //}

    [Fact]
    public void Level2_AddSubTable_WithData() {
        var reader = new DataTblReader<DataA, (string name, int count)>()
            .InitData(src => (src.TableName, src.Rows.Count))
            .AddActionRead(it => {
                Assert.Equal(TblLevel2.TableName, it.Data.name);
                Assert.Equal(TblLevel2.Rows.Count, it.Data.count);
            })
            .AddColumns((s, e) => s(s.RowKey(e.Id), e.Name))
            .AddSubTable(e => e.SubList, sub => sub
                .AddColumns((s, e) => s(e.Id, e.Text))
                .AddActionRead(it => {
                    DataA dataA = it.Parent.Entity;
                    Assert.NotEqual(0, dataA.Id);

                    Assert.Equal(TblLevel2.TableName, it.Data.name);
                    Assert.Equal(TblLevel2.Rows.Count, it.Data.count);
                }));

        var readLmd = reader.Lambda();
        var readFn = readLmd.Compile();
        var list = readFn(TblLevel2);
        CheckRead_TblLevel2(list);
    }

    static readonly DataTable TblLevel3 = new DataTable("3Level") {
        Columns = {
            { "Id", typeof(int) },
            { "Name", typeof(string) },
            { "SubBId", typeof(int) },
            { "SubBText", typeof(string) },
            { "SubCId", typeof(int) },
            { "SubCNum", typeof(int) },
        },
        Rows = {
            { 1000, "AA", 1, "Saa1" },

            { 1000, "AA", 2, "Saa2", 2001, 201 },
            { 1000, "AA", 2, "Saa2", 2002, 202 },
            { 1000, "AA", 2, "Saa2", 2003, 203 },

            { 1000, "AA", 3, "Saa3", 3001, 301 },
            { 1000, "AA", 3, "Saa3", 3002, 302 },

            { 2000, "BB", 1, "Sbb" },

            { 3000, "CC", 1, "Scc1" },
            { 3000, "CC", 2, "Scc2" },
        }
    };

    static void CheckRead_TblLevel3(List<DataA> list) {
        Assert.Equal(3, list.Count);

        Assert.Equal(1000, list[0].Id);
        Assert.Equal("AA", list[0].Name);

        Assert.Equal(3, list[0].SubList.Count);
        Assert.Equal(1, list[0].SubList[0].Id);
        Assert.Equal("Saa1", list[0].SubList[0].Text);
        Assert.Equal(0, list[0].SubList[0].SubCList.Count);
        Assert.Equal(2, list[0].SubList[2].SubCList.Count);

        var subcList2 = list[0].SubList[1].SubCList;
        Assert.Equal(3, subcList2.Count);
        Assert.Equal(2002, subcList2[1].Id);
        Assert.Equal(202, subcList2[1].NumC);

        var subcList3 = list[0].SubList[2].SubCList;
        Assert.Equal(2, subcList3.Count);
        Assert.Equal(3002, subcList3[1].Id);
        Assert.Equal(302, subcList3[1].NumC);

        Assert.Equal(2000, list[1].Id);
        Assert.Equal("BB", list[1].Name);
        Assert.Equal(1, list[1].SubList.Count);
        Assert.Equal(0, list[1].SubList[0].SubCList.Count);

        Assert.Equal(3000, list[2].Id);
        Assert.Equal("CC", list[2].Name);
        Assert.Equal(2, list[2].SubList.Count);
        Assert.Equal(0, list[2].SubList[0].SubCList.Count);
    }

    [Fact]
    public void Level3_AddSubTable() {
        var reader = new DataTblReader<DataA>()
            .AddColumns((s, e) => s(s.RowKey(e.Id), e.Name))
            .AddSubTable(e => e.SubList, sub => sub
                .AddColumns((s, e) => s(s.RowKey(e.Id), e.Text))
                .AddSubTable(e => e.SubCList, (s, e) => s(s.RowKey(e.Id), e.NumC))
            );

        var readLmd = reader.Lambda();
        var readFn = readLmd.Compile();
        var list = readFn(TblLevel3);
        CheckRead_TblLevel3(list);
    }

    [Fact]
    public void Level3_AddSubTable_Action() {
        var reader = new DataTblReader<DataA>()
            .AddColumns((s, e) => s(s.RowKey(e.Id), e.Name))
            .AddSubTable(e => e.SubList, sub => sub
                .AddColumns((s, e) => s(s.RowKey(e.Id), e.Text))
                .AddSubTable(e => e.SubCList, sub => sub
                    .AddColumns((s, e) => s(s.RowKey(e.Id), e.NumC)))
            );

        var readLmd = reader.Lambda();
        var readFn = readLmd.Compile();
        var list = readFn(TblLevel3);
        CheckRead_TblLevel3(list);
    }

    [Fact]
    public void Level3_AddSubTable_WithData_Member() {
        var reader = new DataTblReader<DataA, (string name, int count)>()
            .InitData(src => (src.TableName, src.Rows.Count))
            .AddColumns((s, e) => s(s.RowKey(e.Id), e.Name))
            .AddSubTable(e => e.SubList, sub => sub
                .AddColumns((s, e) => s(s.RowKey(e.Id), e.Text))
                .AddSubTable(e => e.SubCList, (s, e) => s(s.RowKey(e.Id), e.NumC))
            );

        var readLmd = reader.Lambda();
        var readFn = readLmd.Compile();
        var list = readFn(TblLevel3);
        CheckRead_TblLevel3(list);
    }

    [Fact]
    public void Level3_AddSubTable_WithData_Action() {
        var reader = new DataTblReader<DataA, (string name, int count)>()
            .InitData(src => (src.TableName, src.Rows.Count))
            .AddColumns((s, e) => s(s.RowKey(e.Id), e.Name))
            .AddActionRead(it => {
                Assert.Equal(TblLevel3.TableName, it.Data.name);
                Assert.Equal(TblLevel3.Rows.Count, it.Data.count);
            })
            .AddSubTable(e => e.SubList, sub => sub
                .AddColumns((s, e) => s(s.RowKey(e.Id), e.Text))
                .AddActionRead(it => {
                    DataA dataA = it.Parent.Entity;
                    Assert.NotEqual(0, dataA.Id);

                    Assert.Equal(TblLevel3.TableName, it.Data.name);
                    Assert.Equal(TblLevel3.Rows.Count, it.Data.count);
                })
                .AddSubTable(e => e.SubCList, sub => sub
                    .AddColumns((s, e) => s(s.RowKey(e.Id), e.NumC))
                    .AddActionRead(it => {
                        SubB subB = it.Parent.Entity;
                        Assert.NotEqual(0, subB.Id);

                        DataA dataA = it.Parent.Parent.Entity;
                        Assert.NotEqual(0, dataA.Id);

                        Assert.Equal(TblLevel3.TableName, it.Data.name);
                        Assert.Equal(TblLevel3.Rows.Count, it.Data.count);
                    }))
            );

        var readLmd = reader.Lambda();
        var readFn = readLmd.Compile();
        var list = readFn(TblLevel3);
        CheckRead_TblLevel3(list);
    }

    // 不打算实现，多层的表达式选择
    //[Fact]
    //public void Level3_AddSubTable_SelectExpr() {
    //    var reader = new DataTblReader<DataA>()
    //         .AddColumns((s, e) => s.RowKey(e.Id)(e.Name)
    //         .SubTable(e.SubList, e => s.RowKey(e.Id)(e.Text)
    //         .SubTable(e.SubCList, e => s.RowKey(e.Id)(e.NumC))
    //         ));
    //
    //    var readLmd = reader.Lambda();
    //    var readFn = readLmd.Compile();
    //    var list = readFn(TblLevel3);
    //    CheckRead_TblLevel3(list);
    //}

    [Fact]
    public void Level3_AddSubTable_Event() {
        var reader = new DataTblReader<DataA, List<int>>()
            .InitData(src => new(40))
            .AddColumns((s, e) => s(s.RowKey(e.Id), e.Name))
            .AddSubTable(e => e.SubList, sub => sub
                .AddColumns((s, e) => s(s.RowKey(e.Id), e.Text))
                .AddSubTable(e => e.SubCList, sub => sub
                    .OnStartReadingTable(it => {
                        it.Data.Add(it.Parent.Parent.Entity.Id);
                        return true;
                    })
                    .OnStartReadingRow(it => {
                        it.Data.Add(it.Parent.Parent.Entity.Id);
                    })
                    .AddColumns((s, e) => s(s.RowKey(e.Id), e.NumC))
                    .OnEndReadingRow(it => {
                        it.Data.Add(it.Entity.Id);
                    })
                    .OnEndReadingTable(it => {
                        it.Data.Add(it.Entity.Id);
                    }))
            );

        var readLmd = reader.Lambda(f => f.ReturnData());
        var readFn = readLmd.Compile();
        var (list, data) = readFn(TblLevel3);
        CheckRead_TblLevel3(list);
        Assert.Equal(data, [
            1000, // on start table
            1000, 2001,
            1000, 2002,
            1000, 2003,
            2003, // on end table
            1000, // on start table
            1000, 3001,
            1000, 3002,
            3002, // on end table
        ]);

    }
}
