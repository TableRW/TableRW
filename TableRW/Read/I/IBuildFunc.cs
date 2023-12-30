
namespace TableRW.Read.I;

public interface IBuildFunc<out C, out F, out _C> { }
public interface IBuildFunc<out C, out F> : IBuildFunc<C, F, C> { }
