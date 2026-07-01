using Godot;

[Tool]
public partial class Block : StaticBody2D
{
	private Vector2 _size = new Vector2(64, 64);
	private Color _color = new Color(0.25f, 0.25f, 0.32f);
	private bool _collisionDisabled = false;

	[Export]
	public Vector2 Size
	{
		get => _size;
		set { _size = value; UpdateShape(); QueueRedraw(); }
	}

	[Export]
	public Color Color
	{
		get => _color;
		set { _color = value; QueueRedraw(); }
	}

	[Export]
	public bool CollisionDisabled
	{
		get => _collisionDisabled;
		set
		{
			_collisionDisabled = value;
			var shape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			if (shape != null) shape.Disabled = value;
		}
	}

	public override void _Ready()
	{
		var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

		if (collision == null)
		{
			collision = new CollisionShape2D
			{
				Name = "CollisionShape2D",
				Shape = new RectangleShape2D()
			};
			AddChild(collision);
			if (Engine.IsEditorHint())
				collision.Owner = GetTree().EditedSceneRoot;
		}
		else if (collision.Shape == null)
		{
			collision.Shape = new RectangleShape2D();
		}
		else if (!Engine.IsEditorHint())
		{
			// Duplicate shape resource at runtime so instances don't share it
			collision.Shape = (Shape2D)collision.Shape.Duplicate();
		}

		collision.Disabled = _collisionDisabled;
		UpdateShape();
	}

		public override void _Process(double delta)
	{
		if (!Engine.IsEditorHint()) return;

		var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collision?.Shape is RectangleShape2D rect && rect.Size != _size)
		{
			_size = rect.Size;
			// dont call update shape, shape is already correctly set by handles, just visual update
			QueueRedraw();
		}
	}

	private void UpdateShape()
	{
		var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collision?.Shape is RectangleShape2D rect)
		{
			rect.Size = _size;
		}
	}

	public override void _Draw()
	{
		// Origin is the top-left corner of the block.
		// DrawRect from (0,0) with _size matches the collision shape exactly.
		DrawRect(new Rect2(-_size / 2f, _size), _color);
	}
}
