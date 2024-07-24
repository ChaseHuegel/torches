using DryIoc;

namespace Chat.Server.Tests;

public abstract class TestBase
{
    protected Container Container { get; private set; }

    [SetUp]
    public void Setup()
    {
        Container = new Container();
        SetupContainer(Container);
        Container.ValidateAndThrow();
        OnSetup();
    }

    [TearDown]
    public void TearDown()
    {
        OnTearDown();
        Container.Dispose();
    }

    protected abstract void SetupContainer(Container container);
    protected abstract void OnSetup();
    protected abstract void OnTearDown();
}