using Godot;
using System;
using FlappyCore;

public partial class Root : Node2D
{
	[Export] private float _num_obstacles = 10;
	[Export] private float _gap = 30f;
	[Export] private float _obstacle_width = 10f;
	[Export] private float _max_y = 100f;
	[Export] private float _pipeExcess = 100f; // Dépassement des obstacles

	private FlappyEntry _core;
	private OutputData _output;

	private Node2D _bird;
	private Rect2[] _obstaclesBottom;
	private Rect2[] _obstaclesTop;

	private float _screenWidth;
	private float _screenHeight;

	private float birdOffsetX = 0f;
	private float unitsPerPixel = 0;

	// === UI ===
	private Control _menuPanel;
	private Control _gameOverPanel;

	private bool _isPlaying = false;

	private Label _score_label;

	public override void _Ready()
	{
		base._Ready();

		_menuPanel = GetNode<Control>("CanvasLayer/MainMenu");
		_gameOverPanel = GetNode<Control>("CanvasLayer/Gameover");

		_menuPanel.Visible = true;
		_gameOverPanel.Visible = false;

		_menuPanel.GetNode<Button>("Play").Pressed += OnPlayPressed;
		_menuPanel.GetNode<Button>("Quit").Pressed += OnQuitPressed;
		_gameOverPanel.GetNode<Button>("replay").Pressed += OnReplayPressed;
		_gameOverPanel.GetNode<Button>("Mainmenu").Pressed += OnMainMenuPressed;



		GD.Print("[Root] En attente de lancement...");
	}

	// ================= GAMEPLAY =================
	private void StartFlappy()
	{
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
		sprite.Scale = new Vector2(2f, 2f);
		sprite.Centered = true;
		sprite.Position = Vector2.Zero;

		_screenWidth = GetViewport().GetVisibleRect().Size.X;
		_screenHeight = GetViewport().GetVisibleRect().Size.Y;
		unitsPerPixel = _screenWidth / _max_y;

		_obstaclesBottom = new Rect2[_output.Obstacles.Length];
		_obstaclesTop = new Rect2[_output.Obstacles.Length];

		_isPlaying = true;
		_menuPanel.Visible = false;
		_gameOverPanel.Visible = false;

		_score_label = GetNode<Label>("%LabelScore");

		GD.Print("[Root] Jeu démarré !");
	}

	private void ResetFlappy()
	{
		GD.Print("[Root] Réinitialisation du jeu...");
		_core.Reset();
		_isPlaying = true;
		_menuPanel.Visible = false;
		_gameOverPanel.Visible = false;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (!_isPlaying || _core == null) return;

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

		if (_output.GameOver)
		{
			GD.Print("[Root] Game Over détecté !");
			_isPlaying = false;
			_gameOverPanel.Visible = true;
			return;
		}

		_bird.Position = new Vector2(100, MapY(_output.FlappyHeight));

		UpdateObstacles();
		DrawScore();
	}

	private void UpdateObstacles()
	{
		float obstacleWidthPixels = _obstacle_width / _max_y * _screenWidth;

		for (int i = 0; i < _output.Obstacles.Length; i++)
		{
			var o = _output.Obstacles[i];
			float xPos = birdOffsetX + (o.X - _output.FlappyX) * unitsPerPixel;

			// --- OBSTACLE BAS ---
			float bottomHeight = o.Y;
			float bottomHeightPixels = bottomHeight / _max_y * _screenHeight;
			float rectHeightBottom = bottomHeightPixels + _pipeExcess;
			float bottomTopY = _screenHeight - bottomHeightPixels - (_pipeExcess / 2f); // centrer l'excess
			_obstaclesBottom[i] = new Rect2(
				xPos,
				bottomTopY,
				obstacleWidthPixels,
				rectHeightBottom
			);

			// --- OBSTACLE HAUT ---
			float gap = _gap;
			float topHeight = _max_y - bottomHeight - gap;
			float topHeightPixels = topHeight / _max_y * _screenHeight;
			float rectHeightTop = topHeightPixels + _pipeExcess;
			float topBottomY = bottomTopY - (gap / _max_y * _screenHeight) - topHeightPixels - (_pipeExcess / 2f); // centrer l'excess
			_obstaclesTop[i] = new Rect2(
				xPos,
				topBottomY,
				obstacleWidthPixels,
				rectHeightTop
			);
		}

		QueueRedraw();
	}

	private void DrawScore()
	{
		// if (_isPlaying)
		// 	_score_label.Visible = false;
		// else
		// 	_score_label.Visible = true;
		
		_score_label.Text = "Score : " + _core.GetObstaclesPasses();
    }

	public override void _Draw()
	{
		base._Draw();
		if (!_isPlaying) return;

		for (int i = 0; i < _obstaclesBottom.Length; i++)
		{
			DrawRect(_obstaclesBottom[i], Colors.Green);
			DrawRect(_obstaclesTop[i], Colors.Green);
		}
		
		//float borderHeight = 0f; // hauteur des rects de délimitation

   		 // Rect en bas du monde
   	
		float borderHeight = 500f; // par exemple, étendue verticale

		//float borderHeight = 20f; // hauteur des bords en unités de jeu (ou pixels si tu veux fixe)

	// Rect en bas du monde (s'étend vers le haut)
		float bottomY = MapY(0); // position verticale du bas du monde
		Rect2 bottomBorder = new Rect2(-1000, bottomY, _screenWidth * 2, borderHeight);
		DrawRect(bottomBorder, Colors.Green);
		
		// Rect en haut du monde (s'étend vers le bas)
		float topY = _max_y; // position verticale du haut du monde
		Rect2 topBorder = new Rect2(-1000, -topY - borderHeight, _screenWidth *2, borderHeight);
		DrawRect(topBorder, Colors.Green);

		DrawScore();
	}

	private float MapY(float worldY)
	{
		return (_max_y - worldY) / _max_y * _screenHeight;
	}

	// ================= UI CALLBACKS =================
	private void OnPlayPressed() => StartFlappy();
	private void OnQuitPressed() => GetTree().Quit();
	private void OnReplayPressed() => ResetFlappy();
	private void OnMainMenuPressed()
	{
		_isPlaying = false;
		_menuPanel.Visible = true;
		_gameOverPanel.Visible = false;
	}
}