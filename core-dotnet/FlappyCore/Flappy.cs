using System.Numerics;

namespace FlappyCore;


internal class FlyingBird
{

    public Vector2 Position { get; set; }
    public Vector2 Speed { get; set; }
    public Vector2 Acceleration { get; set; }

    public void Tick(float delta_time)
    {
        Speed += Acceleration * delta_time;
        Position += Speed * delta_time;
    }
}

public class Flappy
{
    #region Flappy

    public event Action OnDeath;

    private readonly float[] _obstacles;
    private readonly float _width;
    private readonly float _height;
    private readonly int _num_obstacles;
    private readonly float _padding; // l espace avant de commencer a dessiner les obstacles
    private readonly FlyingBird _bird;
    private readonly float _ecart_obstacles;
    private Random _rand;
    private readonly int _initial_seed;

    private int _obstacle_cursor = 0;

    private bool _has_obstacle_been_passed = false;

    private Flappy(float height, float width, int num_obstacles, float ecart_obstacle, float padding, int seed)
    {
        _height = height;
        _width = width;
        _obstacles = new float[num_obstacles];
        _num_obstacles = num_obstacles;
        _padding = padding;
        _ecart_obstacles = ecart_obstacle;
        _bird = new FlyingBird
        {
            Acceleration = new Vector2(0, -9.81f),
            Speed = new Vector2(1.6f, 0),
            Position = new Vector2(Math.Min(width, 0.1f), height / 2)
        };

        _rand = new Random(seed);
        _initial_seed = seed;
    }

    private void ResetBird()
    {
        _rand = new Random(_initial_seed);
        _bird.Speed = new Vector2(1.4f, 0);
        _bird.Position = new Vector2(Math.Min(_width, 0.1f), _height / 2);
        _obstacle_cursor = 0;
        OnDeath?.Invoke();
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

    /**
     * Methode instanciant un terrain de jeu
     * la hauteur et la largeur sont en metres. Le flappy fait enviror un metre de haut.
     * le padding est la distance avant que les premiers obstacles arrivent 
     * et l'espace qu'il y a apres le dernier obstacle.
     */
    public static Flappy CreateWithDimension(
        float width, float height, int num_obstacles, float ecart_obstacle, float padding, int seed
    )
    {
        return new Flappy(height, width, num_obstacles, ecart_obstacle, padding, seed);
    }

    public void GenerateObstaclesValues(int quantity)
    {
        for (int i = 0; i < quantity; i++)
        {
            _obstacles[(i + _obstacle_cursor)%_num_obstacles] = (float)_rand.NextDouble();
        }

        _obstacle_cursor += quantity;
        _obstacle_cursor %= _num_obstacles;
    }

    public float GetObstacle(int which)
    {
        return _obstacles[(which + _obstacle_cursor) % _num_obstacles];
    }

    public Vector2 GetBirdPosition()
    {
        return _bird.Position;
    }

    public void Flap()
    {
        _bird.Speed = new Vector2(_bird.Speed.X, _bird.Speed.Y + 7);
    }

    public bool HasObstacleBeenPassed()
    {
        return _has_obstacle_been_passed;
    }

    private bool CheckCollision(Vector2 posAvant, Vector2 posApres)
    {
        if (posApres.Y < 0)
            return true;
        if (posApres.Y > _height)
            return true;

        int nextObstacle = (int)((_bird.Position.X - _padding) / _ecart_obstacles);

        var has_obstacle_been_passed = posAvant.X <= _padding + nextObstacle * _ecart_obstacles
            && posApres.X > _padding + nextObstacle * _ecart_obstacles;

        if (
            has_obstacle_been_passed
            && posApres.Y < _obstacles[(nextObstacle + _obstacle_cursor) % _num_obstacles] * _height
        )
        {
            //_obstacle_cursor++;
            return true;
        }

        _has_obstacle_been_passed = has_obstacle_been_passed; // on ne veut pas considerer un obstacle passé si on meurt

        return false;
    }
    #endregion
}
