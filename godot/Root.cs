using Godot;
using System;
using FlappyCore;

public partial class Root : Node2D
{
	[Export] private float _num_obstacles = 10;
	[Export] private float _gap = 30f;
	[Export] private float _obstacle_width = 10f;
	[Export] private float _max_y = 100f;

	private FlappyEntry _core;
	private OutputData _output;

	private Node2D _bird; // On garde l'image
	private Rect2[] _obstaclesBottom;
	private Rect2[] _obstaclesTop;

	private float _screenWidth;
	private float _screenHeight;
	
	private float birdOffsetX = 0f; // ou 0 si tu veux à gauche
	
	private float unitsPerPixel = 0;// étirement proportionnel

	public override void _Ready()
	{
		base._Ready();

		GD.Print("[Root] Initialisation FlappyEntry...");

		_core = new FlappyEntry();
		_output = _core.Init(
			numObstacles: (int)_num_obstacles,
			gravity: 100f,
			obstacleSpeed: 40f,
			birdRadius: 1f,
			seed: 42
		);

		_bird = GetNode<Node2D>("%Bird");
		var sprite = _bird.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		sprite.Scale = new Vector2(2f, 2f); // double la tailleur
		sprite.Centered = true;
		sprite.Position = Vector2.Zero;
		birdOffsetX = _screenWidth * 0.2f;

		_screenWidth = GetViewport().GetVisibleRect().Size.X;
		_screenHeight = GetViewport().GetVisibleRect().Size.Y;
		unitsPerPixel = _screenWidth / _max_y; 
		// Initialisation des Rect2 pour les obstacles
		_obstaclesBottom = new Rect2[_output.Obstacles.Length];
		_obstaclesTop = new Rect2[_output.Obstacles.Length];
		for (int i = 0; i < _output.Obstacles.Length; i++)
		{
			_obstaclesBottom[i] = new Rect2();
			_obstaclesTop[i] = new Rect2();
		}

		GD.Print("[Root] Initialisation terminée");
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		var input = new InputData
		{
			JumpPressed = Input.IsActionJustPressed("Flap"),
			DeltaTime = (float)delta
		};

		try
		{
			_core.Update(in input, ref _output);
		}
		catch (Exception ex)
		{
			GD.PrintErr("[Root] FlappyEntry.Update failed: " + ex.Message);
			return;
		}

		// Position de l'oiseau dans le monde
		_bird.Position = new Vector2(100, MapY(_output.FlappyHeight));

		UpdateObstacles();
	}

	private void UpdateObstacles()
	{
		float obstacleWidthPixels = _obstacle_width / _max_y * _screenWidth;

		for (int i = 0; i < _output.Obstacles.Length; i++)
		{
			var o = _output.Obstacles[i];

			// Position horizontale directement depuis le monde
			float xPos = birdOffsetX + (o.X - _output.FlappyX) * unitsPerPixel;


			// --- OBSTACLE BAS ---
			float bottomHeight = o.Y;
			float bottomTopY = _screenHeight - (bottomHeight / _max_y * _screenHeight);
			_obstaclesBottom[i] = new Rect2(
				xPos,
				bottomTopY,
				obstacleWidthPixels,
				bottomHeight / _max_y * _screenHeight
			);

			// --- OBSTACLE HAUT ---
			float gap = _gap;
			float topHeight = _max_y - bottomHeight - gap;
			float topBottomY = bottomTopY - (gap / _max_y * _screenHeight) - (topHeight / _max_y * _screenHeight);
			_obstaclesTop[i] = new Rect2(
				xPos,
				topBottomY,
				obstacleWidthPixels,
				topHeight / _max_y * _screenHeight
			);
		}

		QueueRedraw();
	}

	public override void _Draw()
	{
		base._Draw();

		for (int i = 0; i < _obstaclesBottom.Length; i++)
		{
			DrawRect(_obstaclesBottom[i], Colors.Green);
			DrawRect(_obstaclesTop[i], Colors.Green);
		}
	}

	private float MapX(float worldX)
	{
		return worldX / _max_y * _screenWidth; // étiré sur toute la largeur
	}

	private float MapY(float worldY)
	{
		return (_max_y - worldY) / _max_y * _screenHeight; // étiré sur toute la hauteur
	}

	public override void _Input(InputEvent @event)
	{
		base._Input(@event);

		if (Input.IsActionJustPressed("Leave Game"))
		{
			GD.Print("[Root] Fermeture demandée.");
			GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			GetTree().Quit();
		}
	}
}
