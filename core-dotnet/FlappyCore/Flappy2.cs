using System;
using System.Numerics;


/**
* Nouveau coeur ou on fait bouger les obstacles
 et ou le flappy reste immobile en X
*/
namespace FlappyCore
{
    internal class FlyingBird2
    {
        public Vector2 Position { get; set; }
        public Vector2 Speed { get; set; }
        public Vector2 Acceleration { get; set; }
        public float Radius { get; set; } // 🟢 Taille physique (rayon en unités monde)

        public void Tick(float deltaTime)
        {
            Speed += Acceleration * deltaTime;
            Position += new Vector2(0, Speed.Y * deltaTime); // X fixe
        }
    }

    public class Flappy2
    {
        // --- Constantes du monde ---
        private const float WORLD_WIDTH = 100f;
        private const float WORLD_HEIGHT = 100f;

        // --- Données internes ---
        private readonly Vector2[] _obstacles;
        private readonly int _numObstacles;
        private readonly float _obstacleSize;
        private readonly float _obstacleSpacing;
        private readonly float _obstacleSpeed;
        private readonly float _gravity;

        private readonly FlyingBird2 _bird;
        private readonly Random _rand;

        // --- Accès public ---
        public float Width => WORLD_WIDTH;
        public float Height => WORLD_HEIGHT;
        public Vector2[] Obstacles => _obstacles;
        public Vector2 BirdPosition => _bird.Position;
            public float BirdVerticalSpeed => _bird.Speed.Y;
        public float BirdRadius => _bird.Radius;

        public int ObstaclesPasses = 0;
        private readonly HashSet<Vector2> obstacles_seen = new();

        public bool IsGameOver { get; private set; } = false;

        // --- Constructeur ---
        public Flappy2(
            int numObstacles,
            float obstacleSize,
            float obstacleSpacing,
            float gravity,
            float obstacleSpeed,
            float birdRadius = 2f, // 🟢 Taille oiseau par défaut
            int seed = 42)
        {
            _numObstacles = numObstacles;
            _obstacleSize = obstacleSize;
            _obstacleSpacing = obstacleSpacing;
            _gravity = gravity;
            _obstacleSpeed = obstacleSpeed;

            _obstacles = new Vector2[_numObstacles];
            _rand = new Random(seed);

            // --- Oiseau statique ---
            _bird = new FlyingBird2
            {
                Position = new Vector2(20f, WORLD_HEIGHT / 2f),
                Speed = Vector2.Zero,
                Acceleration = new Vector2(0, -_gravity),
                Radius = birdRadius
            };

            InitializeObstacles();
        }

        // --- Initialisation des obstacles ---
        private void InitializeObstacles()
        {
            for (int i = 0; i < _numObstacles; i++)
            {
                float x = WORLD_WIDTH + i * _obstacleSpacing;
                float y = GenerateNextObstacleY();
                _obstacles[i] = new Vector2(x, y);
            }
        }

        private float GenerateNextObstacleY()
        {
            return (float)(_rand.NextDouble() * (WORLD_HEIGHT * 0.5f) + WORLD_HEIGHT * 0.25f);
        }

        // --- Tick principal ---
        public void Tick(float deltaTime)
        {
            _bird.Tick(deltaTime);

            // --- Clamp vertical ---
            if (_bird.Position.Y < _bird.Radius)
                _bird.Position = new Vector2(_bird.Position.X, _bird.Radius);
            else if (_bird.Position.Y > WORLD_HEIGHT - _bird.Radius)
                _bird.Position = new Vector2(_bird.Position.X, WORLD_HEIGHT - _bird.Radius);

            // --- Déplacement des obstacles ---
            for (int i = 0; i < _numObstacles; i++)
            {
                ref Vector2 obs = ref _obstacles[i];
                obs.X -= _obstacleSpeed * deltaTime;

                if (obs.X + _obstacleSize < 0f)
                {
                    float maxX = GetMaxObstacleX();
                    obs.X = maxX + _obstacleSpacing;
                    obs.Y = GenerateNextObstacleY();
                }

                //_obstacles[i] = obs;
            }

            if (CheckCollision())
            {
                IsGameOver = true;
            }
        }

        private float GetMaxObstacleX()
        {
            float max = float.MinValue;
            for (int i = 0; i < _numObstacles; i++)
                if (_obstacles[i].X > max)
                    max = _obstacles[i].X;
            return max;
        }

        // --- Interaction ---
        public void Flap()
        {
            _bird.Speed = new Vector2(0f, 50f);
        }

        public void Reset()
        {
            _bird.Position = new Vector2(20f, WORLD_HEIGHT / 2f);
            _bird.Speed = Vector2.Zero;
            InitializeObstacles();
            IsGameOver = false;
            ObstaclesPasses = 0;
            obstacles_seen.Clear();
        }

        // --- Collision ---
        public bool CheckCollision()
        {
            const float GAP_SIZE = 30f; // Espace libre entre le haut et le bas
            const float WORLD_HEIGHT = 100f; // Hauteur totale de la zone de jeu


            foreach (var obs in _obstacles)
            {
                // Distance horizontale entre l'oiseau et le centre du tuyau
                float dx = obs.X - _bird.Position.X;

                // Si trop loin, on ignore
                if (Math.Abs(dx) > _obstacleSize + _bird.Radius)
                    continue;

                if (obs.X < _bird.Position.X && !obstacles_seen.Contains(obs))
                {
                    Console.WriteLine(obs.GetHashCode());
                    obstacles_seen.Add(obs);
                    ObstaclesPasses++;
                }

                // Calcul des limites du trou
                float gapBottom = obs.Y;
                float gapTop = obs.Y + GAP_SIZE;

                // Position verticale de l’oiseau
                float birdY = _bird.Position.Y;

                // Collision si l’oiseau sort du trou
                if (birdY - _bird.Radius < gapBottom || birdY + _bird.Radius > gapTop)
                    return true;
            }

            // Collision avec le sol ou le plafond
            if (_bird.Position.Y - _bird.Radius < 0f || _bird.Position.Y + _bird.Radius > WORLD_HEIGHT)
                return true;

            return false;
        }


    }
}
