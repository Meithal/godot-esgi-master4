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
    private readonly float[] _obstacles;
    private readonly float _width;
    private readonly float _height;
    private readonly int _num_obstacles;
    private readonly float _padding; // l espace avant de commencer a dessiner les obstacles
    private readonly FlyingBird _bird;
    private readonly float _ecart_obstacles;

    private Flappy(float height, float width, int num_obstacles, float padding)
    {
        _height = height;
        _width = width;
        _obstacles = new float[num_obstacles];
        _num_obstacles = num_obstacles;
        _padding = padding;
        _ecart_obstacles = (_width - 2 * _padding) / _num_obstacles;
        _bird = new FlyingBird
        {
            Acceleration = new Vector2(0, -9.81f),
            Speed = new Vector2(1.4f, 0),
            Position = new Vector2(Math.Min(width, 0.1f), height / 2)
        };
    }

    private void ResetBird()
    {
        _bird.Speed = new Vector2(1.4f, 0);
        _bird.Position = new Vector2(Math.Min(_width, 0.1f), _height / 2);
    }

    public void Tick(float delta_time)
    {
        var posa = _bird.Position;
        _bird.Tick(delta_time);
        var posp = _bird.Position;
        bool collided = CheckCollision(posa, posp);
        if (collided)
            ResetBird();
    }

    public static Flappy CreateWithDimension(
        float width, float height, int num_obstacles, float padding
    )
    {
        return new Flappy(height, width, num_obstacles, padding);
    }

    public void GenerateObstaclesValues(int seed)
    {
        var rand = new Random(seed);

        for (int i = 0; i < _num_obstacles; i++)
        {
            _obstacles[i] = (float)rand.NextDouble();
        }
    }

    public float GetObstacle(int which)
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
        if (posApres.Y < 0)
            return true;
        if (posApres.Y > _height)
            return true;

        int nextObstacle = (int)((_bird.Position.X - _padding) / _ecart_obstacles);

        if (
            posAvant.X <= _padding + nextObstacle * _ecart_obstacles
            && posApres.X > _padding + nextObstacle * _ecart_obstacles
            && posApres.Y < _obstacles[nextObstacle] * _height
        )
            return true;

        return false;
    }
    #endregion
}
