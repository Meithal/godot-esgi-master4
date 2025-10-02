using Godot;
using System;
using FlappyCore;

public partial class Root : Node2D
{

	[Export] private float _speed = 1f;
	private float time = 1f;

	private Flappy _classFlappy = Flappy.CreateWithDimension(200, 1000, 100, 10);

	public override void _Ready()
	{
		GD.Print("Mon comp ready");
		GD.Print("Mon comp ready2");
		GD.Print(Flappy.Toto());

		_classFlappy.GenerateObstaclesValues(new Random().Next());

		var canvas = GetNode<ColorRect>("%Canvas");
		{
			for (int i = 0; i < 100; i++)
			{
				var bar = new ColorRect();
				double value = _classFlappy.GetObstacle(i);
				bar.Color = Colors.Red;
				bar.Size = new Vector2(8, (int)(100 * value));
				bar.Position = new Vector2(i * 10, 0);
				canvas.AddChild(bar);
			}
		}
	}

	public override void _Process(double delta)
	{
		// GD.Print("Mon comp ready");
		// this.Translate();
	}
}
