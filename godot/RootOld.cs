using Godot;
using System;
using FlappyCore;

/**
  Script de l'ancien flappy qui dialoguait directement avec le core Flappy.cs
  Lancer la scene root_old.tscn pour le tester
*/
public partial class RootOld : Node2D
{
	public const int PIXELS_PER_M = 100; // Godot par defaut considere que 1m = 100 pixels

	[Export] private float _speed = 1f;
	[Export] private float _width = 1000;
	[Export] private float _height = 10.8f; // en mn 
	[Export] private int _num_obsacles = 6;
	[Export] private float _ecart_obstacles = 1.2f;
	[Export] private float _padding = 2;
	private float time = 1f;

	[Export] private Vector2 fenetre_jeu = new Vector2(400, 200);

	private Flappy _core_flappy;

	private Node2D _godot_bird;

	private bool _has_obstacle_been_passed = false;

	private int _obstacles_passes = 0;

	private ColorRect _canvas;

	public override void _Ready()
	{
		base._Ready();


		GD.Print("Mon comp ready");
		GD.Print("Mon comp ready2");
		//GD.Print(Flappy.Toto());

		_core_flappy = Flappy.CreateWithDimension(_width, _height, _num_obsacles, _ecart_obstacles, _padding, new Random().Next());

		//_core_flappy.GenerateObstaclesValues(10);

		_canvas = GetNode<ColorRect>("%Canvas");
		_canvas.GrowVertical = Control.GrowDirection.Begin;

		_canvas.Size = new Vector2(_width * PIXELS_PER_M, _height * PIXELS_PER_M);

		_godot_bird = GetNode<Node2D>("%Bird");

		_drawObstacles(_obstacles_passes, _canvas);

		_core_flappy.OnDeath += _onDeath;
	}

	public override void _Process(double delta)
	{

		base._Process(delta);

		// convertir le Vector2 de Csharp vers celui de Godot
		// dans notre moteur le Y croit vers le haut
		// l'unite de godot par defaut est de 100 pixels par metre
		var pos = _core_flappy.GetBirdPosition();
		_godot_bird.Position = new Godot.Vector2(
			pos.X * PIXELS_PER_M, (_height - pos.Y) * PIXELS_PER_M
		);
	}

	// le delta est en secondes
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		// notre moteur fonctionne en secondes
		_core_flappy.Tick((float)delta);

		if (_core_flappy.HasObstacleBeenPassed())
		{
			_dealWithObstaclePassed();
		}
	}

	public override void _Input(InputEvent @event)
	{
		base._Input(@event);

		if (Input.IsActionJustPressed("Flap"))
		{
			_core_flappy.Flap();
		}

		if (Input.IsActionJustPressed("Leave Game"))
		{
			GD.Print("leave");
			GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			GetTree().Quit(); // default behavior
		}
	}

	private void _dealWithObstaclePassed()
	{
		GD.Print("pass");
		_obstacles_passes++;
		_core_flappy.GenerateObstaclesValues(1);
		_drawObstacles(_obstacles_passes, _canvas);
	}

	private void _drawObstacles(int start, ColorRect canvas)
	{
		{
			var children = canvas.GetChildren();
			foreach (var c in children)
			{
				if (c is ColorRect)
					c.Free();
			}

			for (int i = 0; i < _num_obsacles; i++)
			{
				var bar = new ColorRect();
				float value = _core_flappy.GetObstacleHeight(i);
				bar.Color = Colors.Red;

				var h = _height * PIXELS_PER_M * value;
				bar.Size = new Vector2(8, h);
				bar.Position = new Vector2(
					_padding * PIXELS_PER_M
						+ (i + start) * _ecart_obstacles * PIXELS_PER_M,
					_height * PIXELS_PER_M - h);
				canvas.AddChild(bar);
			}
		}
	}

	private void _onDeath()
	{
		GD.Print("RIP");
		_obstacles_passes = 0;
		_drawObstacles(0, _canvas);
	}
}
