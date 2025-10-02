using Godot;
using System;
using FlappyCore;

public partial class Root : Node2D
{

	[Export] private float _speed = 1f;
	[Export] private int _width = 10000;
	[Export] private int _height = 300;
	[Export] private int _num_obsacles = 100;
	private float time = 1f;

	[Export] private Vector2 fenetre_jeu = new Vector2(400, 200);

	private Flappy _flappy;

	public override void _Ready()
	{
		GD.Print("Mon comp ready");
		GD.Print("Mon comp ready2");
		GD.Print(Flappy.Toto());

		_flappy = Flappy.CreateWithDimension(_width, _height, _num_obsacles);

		_flappy.GenerateObstaclesValues(new Random().Next());

		var canvas = GetNode<ColorRect>("%Canvas");
		canvas.Size = new Vector2(_width, _height);

		{
			for (int i = 0; i < _num_obsacles; i++)
			{
				var bar = new ColorRect();
				double value = _flappy.GetObstacle(i);
				bar.Color = Colors.Red;
				bar.Size = new Vector2(8, (int)(100 * value));
				bar.Position = new Vector2(i * _width / 100, 0);
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
