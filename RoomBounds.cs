using System.ComponentModel;
using Godot;

[Tool]
public partial class RoomBounds : Node2D
{
	[Export] public float WallThickness = 32f;
	[Export] public Color WallColor = new Color(0.25f, 0.25f, 0.32f);
	
	// Project viewport size, used only for the editor overlay, at runtime the actual viweport size is used
	[Export] public Vector2 DesignSize = new Vector2(1152f, 648f);

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (Engine.IsEditorHint()) return; // dont spawn real walls while just inside editor

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

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint())
			QueueRedraw();
	}

	public override void _Draw()
	{
		if (!Engine.IsEditorHint()) return;

		// OK to place
		Rect2 safeRect = new Rect2(
			new Vector2(WallThickness, WallThickness), 
			DesignSize - new Vector2(WallThickness*2f, WallThickness*2f)
		);

		// draw safe
		DrawRect(safeRect, new Color(0f, 1f, 0.4f, 0.06f));
		DrawRect(safeRect, new Color(0f, 1f, 0.4f, 0.7f), filled: false, width: 0.6f);

		// draw walls
		DrawRect(new Rect2(Vector2.Zero, new Vector2(DesignSize.X, WallThickness)), new Color(WallColor, 0.4f));
		DrawRect(new Rect2(new Vector2(0, DesignSize.Y - WallThickness), new Vector2(DesignSize.X, WallThickness)), new Color(WallColor, 0.4f));
		DrawRect(new Rect2(Vector2.Zero, new Vector2(WallThickness, DesignSize.Y)), new Color(WallColor, 0.4f));
		DrawRect(new Rect2(new Vector2(DesignSize.X - WallThickness, 0), new Vector2(WallThickness, DesignSize.Y)), new Color(WallColor, 0.4f));
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
