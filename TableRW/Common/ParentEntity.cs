namespace TableRW;

public struct ParentEntity<T>(T entity) {
    public T Entity { get; } = entity;
}

public struct ParentEntity<T, P>(T entity, P parent) {
    public T Entity { get; } = entity;
    public P Parent { get; } = parent;
}