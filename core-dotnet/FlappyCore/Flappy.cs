using System.Numerics;

namespace FlappyCore;


internal class FlyingBird
{
    public Vector2 position { get; set; }
    public Vector2 speed { get; set; }
    public Vector2 acceleration { get; set; }


    public void Tick()
    {
        speed += acceleration;
        position += speed;
    }
}

public class Flappy
{
    #region Toto
    public static string Toto()
    {
        return "toto";
    }

    #endregion

    #region Flappy
    private readonly double[] _obstacles = new double[100];
    private readonly Vector2 _world_dimensions;
    private readonly FlyingBird _bird;

    private Flappy(int height, int width)
    {
        _world_dimensions = new Vector2(height, width);
        _bird = new FlyingBird
        {
            acceleration = new Vector2(-9.81f, 0),
            speed = new Vector2(0, 10),
            position = new Vector2(Math.Min(width, 10), height / 2)
        };
    }

    public static Flappy CreateWithDimension(
        int height, int width,
        float posBirdY, float posBirdX
        )
    {
        return new Flappy(height, width);
    }

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
