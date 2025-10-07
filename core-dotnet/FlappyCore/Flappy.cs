using System.Numerics;

namespace FlappyCore;


internal class FlyingBird
{

    public Vector2 Position { get; set; }
    public Vector2 Speed { get; set; }
    public Vector2 Acceleration { get; set; }

    private float _flap_time = 0;
    public Vector2 VerticalAcceleration = new Vector2(0, 15);

    public void Tick(float delta_time)
    {
        var acc = Acceleration;
        //if(_flap_time > 0)
        //    acc
        // Console.WriteLine(speed);
        Speed += Acceleration * delta_time;
        Position += Speed * delta_time;
    }

    // ajoute une acceleration verticale pendant une seconde
    public void Flap()
    {
        _flap_time = 1;
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
            Acceleration = new Vector2(0, -9.81f),
            Speed = new Vector2(1.4f, 0),
            Position = new Vector2(Math.Min(width, 10), height / 2)
        };
    }

    public void Tick(float delta_time)
    {
        var posa = _bird.Position;
        _bird.Tick(delta_time);
        var posp = _bird.Position;
        bool collided = CheckCollision(posa, posp);
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
        return _bird.Position;
    }

    public void Flap()
    {
        _bird.Speed = new Vector2(_bird.Speed.X, _bird.Speed.Y + 7);
    }


    private bool CheckCollision(Vector2 posAvant, Vector2 posApres)
    {


        return true;
    }
    #endregion
}
