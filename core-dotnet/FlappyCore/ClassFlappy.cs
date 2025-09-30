namespace FlappyCore;

public class ClassFlappy
{
    #region Toto
    public static string Toto()
    {
        return "toto";
    }

    #endregion

    #region Flappy
    private readonly double[] _obstacles = new double[100];

    public void GenerateObstaclesValues(int seed)
    {
        var rand = new Random(seed);

        for (int i = 0; i < 100; i++)
        {
            _obstacles[i] = rand.NextDouble();
        }
    }

    public double GetObstacle(int which)
    {
        return _obstacles[which];
    }

#endregion
}
