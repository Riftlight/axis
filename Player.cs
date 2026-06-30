using Godot;

public partial class Player : CharacterBody2D
{
	[Export] public float GravityStrength = 1400f; // per second
	[Export] public float MaxSpeed = 1000f;
	[Export] public Vector2 BodySize = new Vector2(28, 28);

	private Vector2 _gravityDir = Vector2.Down;

	public override void _Ready()
	{
		UpDirection = -_gravityDir;
	}

	public override void _PhysicsProcess(double delta)
	{
		Velocity += _gravityDir * GravityStrength * (float)delta;
		Velocity = Velocity.LimitLength(MaxSpeed);
		MoveAndSlide();
	}

	public override void _Process(double delta)
	{
		QueueRedraw(); // todo
	}

	public override void _Input(InputEvent @event)
	{
		// todo bad
		if (@event is InputEventKey eventKey && eventKey.Pressed && !eventKey.Echo)
			if (eventKey.PhysicalKeycode == Key.Space)
				FlipGravity();
	}

	private void FlipGravity()
	{
		Vector2 newDir = GetTargetGravityDir();
		if (newDir == _gravityDir) return;

		_gravityDir = newDir;
		UpDirection = _gravityDir;
	}

	private Vector2 GetTargetGravityDir()
	{
		Vector2 toMouse = GetGlobalMousePosition() - GlobalPosition;

		if (Mathf.Abs(toMouse.X) > Mathf.Abs(toMouse.Y))
			return toMouse.X > 0.0f ? Vector2.Right : Vector2.Left;
		
		return toMouse.Y > 0.0f ? Vector2.Down : Vector2.Up;
	}

	public override void _Draw()
	{
		// player body
		DrawRect(new Rect2(-BodySize / 2.0f, BodySize), new Color(0.3f, 0.75f, 1f));

		// gravity arrow
		Vector2 dir = GetTargetGravityDir();
		Vector2 start = dir * (BodySize.Length() / 2.0f);
		Vector2 tip = dir * 50f;
		Color arrowColor = new Color(1f, 0.9f, 0.2f);

		DrawLine(start, tip, arrowColor, 4f);

		Vector2 off = dir.Rotated(Mathf.Pi / 2f) * 8f;
		DrawLine(tip, tip - dir*14f + off, arrowColor, 4f);
		DrawLine(tip, tip - dir*14f - off, arrowColor, 4f);
	}
}
