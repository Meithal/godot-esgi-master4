using System;

using FlappyCore;

namespace FlappyCore;
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

    public ObstacleData()
    {
        X = 0f;
        Y = 0f;
        lifeTime = 0f;
    }
}

public struct OutputData
{
    public float FlappyHeight;
    public float FlappyVerticalSpeed;
    public ObstacleData[] Obstacles;

    public OutputData(int visibleObstacleCount)
    {
        FlappyHeight = 0f;
        FlappyVerticalSpeed = 0f;
        Obstacles = new ObstacleData[visibleObstacleCount];
    }
}

public class FlappyEntry
{
    public const int MAX_VISIBLE_OBSTACLES = 5;
    public const int TIMESCALE = 1;

    private readonly int _worldObstacleCount = 20;
    private float _width = 1.0f;
    private float _height = 1.0f;
    private float _padding = 0.1f;

    private Flappy game;
    public float GameTime { get; private set; }

    public float Width => _width;
    public float Height => _height;
    public float Padding => _padding;
    public int WorldObstacleCount => _worldObstacleCount;

    public OutputData Init()
    {
        game = Flappy.CreateWithDimension(_width, _height, _worldObstacleCount, _padding);
        game.GenerateObstaclesValues(seed: 42);
        GameTime = 0f;
        return new OutputData(MAX_VISIBLE_OBSTACLES);
    }

    public void Update(in InputData input, ref OutputData output)
    {
        if (input.JumpPressed)
            game.Flap();

        game.Tick(input.DeltaTime);
        GameTime += input.DeltaTime;

        var birdPos = game.GetBirdPosition();
        output.FlappyHeight = birdPos.Y;
        output.FlappyVerticalSpeed = 0f;

        float padding = Padding;
        float width = Width;
        int totalObs = WorldObstacleCount;
        float ecart = (width - 2 * padding) / totalObs;

        int firstVisibleIdx = Math.Max(0, (int)((birdPos.X - padding) / ecart) - 1);

        for (int i = 0; i < MAX_VISIBLE_OBSTACLES; i++)
        {
            int obsIdx = (firstVisibleIdx + i) % totalObs;

            float obstacleWorldX = padding + obsIdx * ecart;
            float obstacleY = game.GetObstacle(obsIdx);

            float screenX = obstacleWorldX - birdPos.X;
            float screenXNormalized = 0.5f + screenX / width;

            output.Obstacles[i].X = screenXNormalized;
            output.Obstacles[i].Y = obstacleY;
            output.Obstacles[i].lifeTime += input.DeltaTime;
        }
    }

    public void Reset()
    {
        GameTime = 0f;
        game = Flappy.CreateWithDimension(_width, _height, _worldObstacleCount, _padding);
        game.GenerateObstaclesValues(seed: 42);
    }
}
