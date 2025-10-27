using System;
using System.Numerics;

namespace FlappyCore
{
    public struct InputData
    {
        public bool JumpPressed;
        public float DeltaTime;
    }

    public struct ObstacleData
    {
        public float X;
        public float Y;
        public float lifeTime;
    }

    public struct OutputData
    {
        public float FlappyX;
        public float FlappyHeight;
        public float FlappyVerticalSpeed;
        public ObstacleData[] Obstacles;

        public OutputData(int obstacleCount)
        {
            FlappyX = 0f;
            FlappyHeight = 0f;
            FlappyVerticalSpeed = 0f;
            Obstacles = new ObstacleData[obstacleCount];
        }
    }

    public class FlappyEntry
    {
        private Flappy2 game;
        public float GameTime { get; private set; }

        // --- Initialisation ---
        public OutputData Init(
            int numObstacles = 20,
            float gravity = 9.81f,
            float obstacleSpeed = 15f,
            float birdRadius = 2f,
            int seed = 42)
        {
            game = new Flappy2(
                numObstacles,
                gravity,
                obstacleSpeed,
                birdRadius,
                seed
            );

            GameTime = 0f;
            return new OutputData(numObstacles);
        }

        // --- Mise à jour ---
        public void Update(in InputData input, ref OutputData output)
        {
            if (game == null)
                throw new InvalidOperationException("FlappyEntry.Update called before Init.");

            if (input.DeltaTime < 0f)
                throw new ArgumentOutOfRangeException("DeltaTime cannot be negative.");

            try
            {
                if (input.JumpPressed)
                    game.Flap();

                game.Tick(input.DeltaTime);
                GameTime += input.DeltaTime;

                // Position de l'oiseau
                var birdPos = game.BirdPosition;
                output.FlappyX = birdPos.X;
                output.FlappyHeight = birdPos.Y;
                output.FlappyVerticalSpeed = 0;

                // Synchroniser le tableau d’obstacles avec celui du jeu
                var obstacles = game.Obstacles;
                if (output.Obstacles == null || output.Obstacles.Length != obstacles.Length)
                    output.Obstacles = new ObstacleData[obstacles.Length];

                for (int i = 0; i < obstacles.Length; i++)
                {
                    var obs = obstacles[i];
                    output.Obstacles[i].X = obs.X;
                    output.Obstacles[i].Y = obs.Y;
                    output.Obstacles[i].lifeTime += input.DeltaTime;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("FlappyEntry.Update failed: " + ex.Message, ex);
            }
        }

        // --- Reset ---
        public void Reset()
        {
            game?.Reset();
            GameTime = 0f;
        }
    }
}
