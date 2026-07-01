using Godot;

public partial class RoomBounds : Node2D
{
	[Export] public float WallThickness = 32f;
	[Export] public Color WallColor = new Color(0.25f, 0.25f, 0.32f);

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Vector2 viewport = GetViewportRect().Size;

		CreateWall("Floor", 
			new Vector2(viewport.X / 2f, viewport.Y - WallThickness / 2f),
			new Vector2(viewport.X, WallThickness)
		);

		CreateWall("Ceiling",
			new Vector2(viewport.X / 2f, WallThickness / 2f),
			new Vector2(viewport.X, WallThickness)
		);

		CreateWall("Left",
			new Vector2(WallThickness / 2f, viewport.Y / 2f),
			new Vector2(WallThickness, viewport.Y)
		);

		CreateWall("Right", 
			new Vector2(viewport.X - WallThickness/2f, viewport.Y / 2f),
			new Vector2(WallThickness, viewport.Y)
		);
	}

	private void CreateWall(string name, Vector2 pos, Vector2 size)
	{
		StaticBody2D body = new StaticBody2D { Name = name };
		AddChild(body);
		body.Position = pos;

		CollisionShape2D coll = new CollisionShape2D();
		body.AddChild(coll);
		coll.Shape = new RectangleShape2D { Size = size };

		ColorRect visual = new ColorRect { Size = size, Position = -size/2f, Color = WallColor };
		body.AddChild(visual);
	}
}
