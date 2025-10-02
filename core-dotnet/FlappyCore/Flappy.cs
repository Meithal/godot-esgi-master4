using System.Numerics;

namespace FlappyCore;


internal class FlyingBird
{
    public Vector2 position { get; set; }
    public Vector2 speed { get; set; }
    public Vector2 acceleration { get; set; }

    
    public void Tick(float delta_time)
    {
        // Console.WriteLine(speed);
        speed += acceleration * delta_time;
        position += speed * delta_time;
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
    private readonly double[] _obstacles;
    private readonly Vector2 _world_dimensions;
    private readonly int _num_obstacles;
    private readonly FlyingBird _bird;

    private Flappy(int height, int width, int num_obstacles)
    {
        _world_dimensions = new Vector2(height, width);
        _obstacles = new double[num_obstacles];
        _num_obstacles = num_obstacles;
        _bird = new FlyingBird
        {
            acceleration = new Vector2(0, -9.81f),
            speed = new Vector2(1.4f, 0),
            position = new Vector2(Math.Min(width, 10), height / 2)
        };
    }

    public void Tick(float delta_time)
    {
        _bird.Tick(delta_time);
    }

    public static Flappy CreateWithDimension(
        int width, int height, int num_obstacles
    )
    {
        return new Flappy(height, width, num_obstacles);
    }

    public void GenerateObstaclesValues(int seed)
    {
        var rand = new Random(seed);

        for (int i = 0; i < _num_obstacles; i++)
        {
            _obstacles[i] = rand.NextDouble();
        }
    }

    public double GetObstacle(int which)
    {
        return _obstacles[which];
    }

    public Vector2 GetBirdPosition()
    {
        return _bird.position;
    }

    public void Flap()
    {
        _bird.speed = new Vector2(_bird.speed.X, _bird.speed.Y + 10);
    }

    #endregion
}
