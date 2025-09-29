namespace FlappyCore.Tests;

[TestClass]
public sealed class TestToto
{
    [TestMethod]
    public void TestTotoWorks()
    {

        Assert.AreEqual("toto", ClassFlappy.Toto());
    }
}
