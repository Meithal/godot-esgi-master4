namespace FlappyCore.Tests;

[TestClass]
public sealed class TestToto
{
    private readonly ClassFlappy _classFlappy = new ClassFlappy();

    [TestMethod]
    public void TestTotoWorks()
    {

        Assert.AreEqual("toto", ClassFlappy.Toto());
    }

    [TestMethod]
    public void TestObstacles()
    {
        {
            double accum = 0;
            for (int i = 0; i < 100; i++)
            {
                accum += _classFlappy.GetObstacle(i);
            }
            Assert.AreEqual(0, accum);
        }
        _classFlappy.GenerateObstaclesValues(new Random().Next());
        {
            double accum = 0;
            for (int i = 0; i < 100; i++)
            {
                accum += _classFlappy.GetObstacle(i);

            }
            Assert.AreNotEqual(0, accum);
        }
    }
}
