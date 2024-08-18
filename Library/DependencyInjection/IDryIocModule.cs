namespace Library.DependencyInjection;

public interface IDryIocModule
{
    public void Load(IContainer container);
}