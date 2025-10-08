using System;
using FlappyCore;

public struct InputData
{
    public bool JumpPressed;
    public float DeltaTime;
}

public struct ObstacleData
{
    public float X;
    public float Y;
}

public struct OutputData
{
    public float FlappyHeight;
    public float FlappyVerticalSpeed;
    public ObstacleData[] Obstacles;

    public OutputData(int obstacleCount)
    {
        FlappyHeight = 0f;
        FlappyVerticalSpeed = 0f;
        Obstacles = new ObstacleData[obstacleCount];
    }
}

public class FlappyGameBridge
{
    public const int MAX_OBSTACLES = 5;

    private Flappy game;
    public OutputData Init()
    {
        game = Flappy.
        return new OutputData(MAX_OBSTACLES);
    }

    public void Update(in InputData input, ref OutputData output)
    {

    }

    public void Reset()
    {
    }
}
