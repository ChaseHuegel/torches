namespace Library.ECS.Components;

public struct PositionComponent(float x, float y, float z) : IDataComponent
{
    public float X = x;
    public float Y = y;
    public float Z = z;
}