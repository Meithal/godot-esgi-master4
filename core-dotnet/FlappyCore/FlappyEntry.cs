using System;

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

        public OutputData(int visibleObstacleCount)
        {
            FlappyX = 0f;
            FlappyHeight = 0f;
            FlappyVerticalSpeed = 0f;
            Obstacles = new ObstacleData[visibleObstacleCount];
        }
    }

    public class FlappyEntry
    {
        public const int MAX_VISIBLE_OBSTACLES = 20;

        private readonly int _worldObstacleCount = 20;
        private float _width = 1000f;
        private float _height = 10f;
        private float _padding = 2f;
        private Flappy game;

        public float GameTime { get; private set; }
        public float Width => _width;
        public float Height => _height;
        public float Padding => _padding;
        public int WorldObstacleCount => _worldObstacleCount;

        public OutputData Init(float width = 1000f, float height = 10f)
        {
            _width = width;
            _height = height;

            game = Flappy.CreateWithDimension(_width, _height, _worldObstacleCount, _padding);
            game.GenerateObstaclesValues(42);
            GameTime = 0f;
            return new OutputData(MAX_VISIBLE_OBSTACLES);
        }

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

                var birdPos = game.GetBirdPosition();
                output.FlappyX = birdPos.X;
                output.FlappyHeight = birdPos.Y;
                output.FlappyVerticalSpeed = 0f;

                float ecart = (Width - 2 * Padding) / WorldObstacleCount;
                for (int i = 0; i < MAX_VISIBLE_OBSTACLES; i++)
                {
                    int idx = i % WorldObstacleCount;
                    output.Obstacles[i].X = Padding + idx * ecart;
                    output.Obstacles[i].Y = game.GetObstacle(idx) * Height;
                    output.Obstacles[i].lifeTime += input.DeltaTime;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("FlappyEntry.Update failed: " + ex.Message, ex);
            }
        }

        public void Reset()
        {
            GameTime = 0f;
            game = Flappy.CreateWithDimension(_width, _height, _worldObstacleCount, _padding);
            game.GenerateObstaclesValues(42);
        }
    }
}
