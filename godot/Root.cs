using Godot;
using System;
using FlappyCore;

public partial class Root : Node2D
{

	[Export] private float _speed = 1f;
	[Export] private int _width = 1000;
	[Export] private int _height = 10;
	[Export] private int _num_obsacles = 100;
	private float time = 1f;

	[Export] private Vector2 fenetre_jeu = new Vector2(400, 200);

	private Flappy _core_flappy;

	private Node2D _godot_bird;

	public override void _Ready()
	{
		base._Ready();

		GD.Print("Mon comp ready");
		GD.Print("Mon comp ready2");
		GD.Print(Flappy.Toto());

		_core_flappy = Flappy.CreateWithDimension(_width, _height, _num_obsacles);

		_core_flappy.GenerateObstaclesValues(new Random().Next());

		var canvas = GetNode<ColorRect>("%Canvas");
		canvas.Size = new Vector2(_width * 100, _height * 100);

		_godot_bird = GetNode<Node2D>("%Bird");

		{
			for (int i = 0; i < _num_obsacles; i++)
			{
				var bar = new ColorRect();
				double value = _core_flappy.GetObstacle(i);
				bar.Color = Colors.Red;
				bar.Size = new Vector2(8, (int)(100 * value));
				bar.Position = new Vector2(i * _width / 100, 0);
				canvas.AddChild(bar);
			}
		}
	}

	public override void _Process(double delta)
	{

		base._Process(delta);
		// GD.Print("Mon comp ready");
		// this.Translate();


		// convertir le Vector2 de Csharp vers celui de Godot
		// dans notre moteur le Y croit vers le haut
		// l'unite de godot par defaut est de 100 pixels par metre
		var pos = _core_flappy.GetBirdPosition();
		_godot_bird.Position = new Godot.Vector2(pos.X * 100, -pos.Y * 100);
	}

	// le delt est en secondes
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		// notre moteur fonctionne en secondes
		_core_flappy.Tick((float)delta);

	}

	public override void _Input(InputEvent @event)
	{
		base._Input(@event);

		if (Input.IsActionJustPressed("Flap"))
		{
			_core_flappy.Flap();
		}
    }
}
