namespace Library.ECS.Components;

public struct IdentifierComponent(string name) : IDataComponent
{
    public string? Name = name;
    public string? Tag;
}