# TableRW ([English](../README.md) | 中文)
[![NuGet Version](https://img.shields.io/nuget/v/TableRW.svg?label=NuGet)](https://www.nuget.org/packages/TableRW)

对表格数据进行读取和写入的库，使用表达式树生成委托（Lambda），快速方便的读写数据到实体对象（Entity），多层实体的映射读写。

```
dotnet add package TableRW
```

## 其他相关库的实现
使用一样读写配置方法，也是表达式树生成委托，也添加了库相关的一些便捷方法。

|  Lib   |  Link  |  Version  |
|  ----  |  ----  |  -------  |
| Epplus | [TableRW.Epplus](https://github.com/TableRW/TableRW.Epplus) | [![NuGet Version](https://img.shields.io/nuget/v/TableRW.Epplus.svg?label=NuGet)](https://www.nuget.org/packages/TableRW.Epplus) |
| NPOI   | [TableRW.NPOI](https://github.com/TableRW/TableRW.NPOI) | [![NuGet Version](https://img.shields.io/nuget/v/TableRW.NPOI.svg?label=NuGet)](https://www.nuget.org/packages/TableRW.NPOI) |

## 从 `DataTable` 读取到 Entity

### 添加命名空间
``` cs
using TableRW.Read;   // Read method
using TableRW.Read.DataTableEx; // DataTable extension method
```

### 简单的读取（未缓存）
``` cs
public class Entity {
    public long Id { get; set; }
    public string Name;
    public string Tel; // it can be of a field
    public int? NullableInt { get; set; } // or a property
}

var reader = new DataTblReader<Entity>()
    .AddColumns((s, e) => s(e.Id, e.Name, e.Tel, e.NullableInt));

// 可以在 debug 查看到生成的表达式树
var readLmd = reader.Lambda(); // Expression<Func<DataTable, List<Entity>>>
var readFn = readLmd.Compile(); // Func<DataTable, List<Entity>>
var list = readFn(dataTable); // List<Entity>
```

### 含有子表的读取（未缓存）
``` cs
public class EntityA {
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime Date { get; set; }
    public List<EntityB> SubB { get; set; }
}
public class EntityB {
    public int Id { get; set; }
    public string Text { get; set; }
    public string Remark { get; set; }
}
var reader2 = new DataTblReader<EntityA>()
    .AddColumns((s, e) => s(s.RowKey(e.Id), e.Name, e.Date))
    .AddSubTable(e => e.SubList, (s, e) => s(e.Id, e.Text, e.Remark));

var readLmd = reader2.Lambda(); // Expression<Func<DataTable, List<EntityA>>>
var readFn = readLmd.Compile(); // Func<DataTable, List<EntityA>>

// table
// | 10  | name1 | 101  | text101 | remark101
// | 10  | name1 | 102  | text102 | remark102
// | 20  | name2 | 201  | text201 | remark201
var list = readFn(table); // List<EntityA>
_ = list.Count == 2;
_ = list[0].SubB.Count == 2;
_ = list[1].SubB.Count == 1;

```

<!-- [更多子表的读取]() -->

### 缓存生成的委托
上面的 `reader` 每次执行都要编译表达式树，实际上应该把生成的 `readFn` 进行缓存，之后的直接调用该委托。
``` cs
// 需要使用者自己新建这么一个类，管理 Cache
static class CacheReadFn<T> {
    internal static Func<DataTable, List<T>>? Fn;
}

// 简单使用的封装
static class CacheReadTbl {
    public static List<T> Read<T>(DataTable tbl, Action<DataTblReader<T>> buildRead) {
        if (CacheReadFn<T>.Fn == null) {
            var reader = new DataTblReader<T>();
            buildRead(reader);

            // 在 debug 时，可以查看生成的表达式树
            var readLmd = reader.Lambda();
            CacheReadFn<T>.Fn = readLmd.Compile();
        }
        return CacheReadFn<T>.Fn(tbl);
    }
}

var list = CacheReadTbl.Read<Entity>(table, reader => {
    reader.AddColumns((s, e) => s(e.Id, e.Name, e.Tel, e.NullableInt));
});
```

### 使用本库提供的缓存
本库也进行了一些简单的封装，方便使用者的调用：
``` cs
using TableRW.Read;
using TableRW.Read.DataTableEx; // DataTable extension method

void Example(DataTable tbl) {
    // 使用 DataTable 的列名作为属性映射，列名和属性名必须相同
    var list1 = tbl.ReadToList<Entity>(); // List<Entity>

    var list2 = tbl.ReadToList<Entity>(cacheKey: 0, reader => {
        // 自己处理属性和列的映射
        reader.AddColumns((s, e) => s(e.Id, e.Name, e.Tel, e.NullableInt));

        // 在 debug 时，可以查看生成的表达式树
        var lmd = reader.Lambda();
        return lmd.Compile();
    });
}
```

### 读取时的事件
``` cs
static void Example(DataTable tbl) {
var list2 = tbl.ReadToList<Entity>(cacheKey: 0, reader => {
    reader.AddColumns((s, e) =>
        s(e.Id, e.Name, e.Tel, e.NullableInt))
        .OnStartReadingTable(it => {
            // Row.Count >= 100 才会进行读取
            return it.Src.Rows.Count >= 100;
        })
        .OnStartReadingRow(it => {
            // 如果 row 的第0列是 DBNull 则跳过 row 的读取
            return it.SkipRow(it.Src.Rows[it.iRow][0] is DBNull);
        })
        .OnEndReadingRow(it => {
            // 如果读取到的 StructInt > 1000 则跳过此行的记录
            return it.SkipRow(it.Entity.StructInt > 1000);
        })
        .OnEndReadingTable(it => { });

    var lmd = reader.Lambda();
    return lmd.Compile();
});
}
```

### 调整生成的 Lambda
``` cs
var reader = new DataTblReader<Entity>()
    .AddColumns((s, e) => s(e.Id, e.Name, e.Tel, e.NullableInt));

// 在 debug 时，可以查看生成的表达式树
var lmd1 = reader.Lambda();
var fn1 = lmd1.Compile(); // Func<DataTable, List<Entity>>
fn1(table);


var lmd2 = reader.Lambda(f => f.StartRow());
var fn2 = lmd2.Compile(); // Func<DataTable, int, List<Entity>>
var startRow = 3; // 从第三行开始读
fn2(table, startRow);


var lmd3 = reader.Lambda(f => f.Start());
var fn3 = lmd3.Compile(); // Func<DataTable, int, int, List<Entity>>
(startRow, var startCol) = (3, 2); // 从第3行第2列开始读
fn3(table, startRow, startCol);

var lmd4 = reader.Lambda(f => f.ToDictionary(entity => entity.Id));
var fn4 = lmd4.Compile(); // Func<DataTable, Dictionary<long, Entity>>
// 返回一个以 entity.Id 为 key 的 Dictionary
var dic4 = fn4(table); // Dictionary<long, Entity>

// 多个配置组合
var lmd5 = reader.Lambda(f => f.StartRow().ToDictionary(entity => entity.Id));
var fn5 = lmd5.Compile(); // Func<DataTable, int, int, Dictionary<long, Entity>>
startRow = 2;
var dic5 = fn5(table, startRow);
```

### 更多读取的方式
``` cs
static void Example(DataTable tbl) {
var list = tbl.ReadToList<Entity>(cacheKey: 0, reader => {
    var x = reader
        // 设置开始读取的位置
        .SetStart(row: 3, column: 2)
        // 添加几列映射的读取
        .AddColumns((s, e) => s(e.Id, e.Name))
        // 跳过2列读取
        .AddSkipColumn(2)
        // 把这列的值转成 DateTime，然后再执行一个函数
        .AddColumnRead((DateTime val) => it => {
            if (val.Year < 2000) {
                // 如果 Year < 2000, 跳过此行的读取
                return it.SkipRow();
            }
            it.Entity.Year = val.Year;
            return null; // 没有行为要做
        })
        // 再添加几列读取
        .AddColumns((s, e) => s(e.Text1, e.Text2))
        // 执行一个 Action，这里没有读取数据列，可以对 entity 进行处理
        .AddActionRead(it => {
            it.Entity.Remark1 = it.Entity.Text1 + it.Entity.Text2;
            it.Entity.Remark2 = it.Entity.Id + " - " + it.Entity.Year;
        });


    var lmd = reader.Lambda();
    return lmd.Compile();
});
}
```

## 写入 `DataTable`

### 添加命名空间
``` cs
using TableRW.Write;
using TableRW.Write.DataTableEx;
```

### 简单的写入（未缓存）
``` cs
public class Entity {
    public long Id { get; set; }
    public string Name;
    public string Tel; // it can be of a field
    public int? NullableInt { get; set; } // or a property
}

var writer = new DataTblWriter<Entity>()
    .AddColumns((s, e) => s(e.Id, s.Skip(1), e.Name, e.Tel, e.NullableInt));

// 可以在 debug 查看到生成的表达式树
var writeLmd = writer.Lambda(); // Expression<Action<DataTable, IEnumerable<Entity>>>
var writeFn = writeLmd.Compile(); // Action<DataTable, IEnumerable<Entity>>
IEnumerable<Entity> data = new List<Entity>();
writeFn(dataTable, data);
```

### 缓存生成的委托
上面的 `writer` 每次执行都要编译表达式树，实际上应该把生成的 `writeFn` 进行缓存，之后的直接调用该委托。
``` cs
// 需要使用者自己新建这么一个类，管理 Cache
static class CacheWriteFn<T> {
    internal static Action<DataTable, IEnumerable<T>>? Fn;
}

// 简单使用的封装
static class CacheWriteTbl {
    public static void WriteFrom<T>(
        DataTable tbl, IEnumerable<TEntity> data, Action<DataTblWriter<T>> buildWrite
    ) {
        if (CacheWriteFn<T>.Fn == null) {
            var writer = new DataTblWriter<T>();
            buildWrite(writer);

            // 在 debug 时，可以查看生成的表达式树
            var writeLmd = writer.Lambda();
            CacheWriteFn<T>.Fn = writeLmd.Compile();
        }
        CacheWriteFn<T>.Fn(tbl);
    }
}

var list = new List<Entity>();
CacheWriteTbl.WriteFrom<Entity>(table, list, writer => {
    writer.AddColumns((s, e) => s(e.Id, e.Name, e.Tel, e.NullableInt));
});
```

### 使用本库提供的缓存
本库也进行了一些简单的封装，方便使用者的调用：
``` cs
using TableRW.Write;
using TableRW.Write.DataTableEx;

void Example(DataTable tbl, List<Entity> data) {
    tbl.WriteFrom(data, cacheKey: 0, writer => {
        writer.AddColumns((s, e) => s(e.Id, e.Name, e.Tel, e.NullableInt));

        // 在 debug 时，可以查看生成的表达式树
        var lmd = writer.Lambda();
        return lmd.Compile();
    );
    // tbl 已经被写入数据
}
```

### 写入时的事件
``` cs
static void Example(DataTable tbl, List<Entity> data) {

tbl.WriteFrom(data, cacheKey: 0, writer => {
    writer.AddColumns((s, e) =>
        s(e.Id, e.Name, e.Tel, e.NullableInt))
        .OnStartWritingTable(it => {
            it.Src.TableName = "set TableName";
        })
        .OnStartWritingRow(it => {
            it.Row[0] = "set column";
        })
        .OnEndWritingRow(it => {
            it.Row[it.iCol + 1] = "set column";
        })
        .OnEndWritingTable(it => { });

    var lmd = writer.Lambda();
    return lmd.Compile();
});
}
```

