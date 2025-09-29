using Godot;
using System;

public partial class Root : Node3D
{

	[Export] private float _speed = 1f;
	private float time = 1f;

	public override void _Ready()
	{
		GD.Print("Mon comp ready");
		GD.Print("Mon comp ready2");
	}

	public override void _Process(double delta)
	{

		// this.Translate();
	}
}
