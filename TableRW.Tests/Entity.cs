namespace TableRW.Tests;

//[DebuggerDisplay("Name = {Name}, Email = {Email}")]
//public class Account {
//    public string Name { get; set; }
//    public string Email { get; set; }
//    public bool Active { get; } // readonly, Ignore Write
//    public DateTime CreatedDate { get; set; }
//    public Guid? Guid { get; set; }

//    [IgnoreWrite]
//    public IList<string> Roles { get; set; }

//    public static DataTable GetDataTable() => new() {
//        Columns = {
//            { nameof(Name), typeof(string) },
//            { nameof(Email), typeof(string) },
//            { nameof(Active), typeof(bool) },
//            { nameof(CreatedDate), typeof(DateTime) },
//            { nameof(Guid), typeof(Guid) },
//            //{ nameof(Account.Roles), typeof(List<string>) },
//        }
//    };
//}


public class RecordA {

    public RecordA() {
        Random random = new();
        _IgnoreWriteValue = random.Next();
        this.ReadonlyField = _IgnoreWriteValue;
        this.IgnoreField = _IgnoreWriteValue;
        this.ReadonlyProperty = _IgnoreWriteValue;
        this.InitProperty = _IgnoreWriteValue;
        this.IgnoreProperty = _IgnoreWriteValue;


    }

    int _IgnoreWriteValue;

    public string? FieldStr;
    public int FieldInt;

    public readonly int ReadonlyField;
    [IgnoreRead]
    public int IgnoreField;

    public string? Str { get; set; }
    public int StructInt { get; set; }
    public int? NullableInt { get; set; }

    public int ReadonlyProperty { get; }
    [IgnoreRead]
    public int InitProperty { get; init; }
    [IgnoreRead]
    public int IgnoreProperty { get; set; }

    public void TestIgnoreWrite() {
        Assert.Equal(_IgnoreWriteValue, ReadonlyField);
        Assert.Equal(_IgnoreWriteValue, IgnoreField);
        Assert.Equal(_IgnoreWriteValue, ReadonlyProperty);
        Assert.Equal(_IgnoreWriteValue, InitProperty);
        Assert.Equal(_IgnoreWriteValue, IgnoreProperty);
    }

}
