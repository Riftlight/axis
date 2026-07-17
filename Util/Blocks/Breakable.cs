using Godot;

public partial class Breakable : StaticBody2D
{

	[Export] public float SpeedThreshold = 1000f;
	[Export] public Texture2D BreakableTexture;
	[Export] public float PixelScale = 4f;

	private Vector2 _downVector;
	private CollisionShape2D coll;

	public override void _Ready()
	{
		this.TextureRepeat = TextureRepeatEnum.Enabled; // tile
		coll = GetNode<CollisionShape2D>("CollisionShape2D"); // should probably be replaced with [Export]ed property

		Vector2 rotatedDown = Vector2.Down.Rotated(Rotation);
		if (Mathf.Abs(rotatedDown.X) > Mathf.Abs(rotatedDown.Y))
			_downVector = rotatedDown.X > 0 ? Vector2.Right : Vector2.Left;
		else
			_downVector = rotatedDown.Y > 0 ? Vector2.Down : Vector2.Up;
	}


	public void PlayerHit(Vector2 playerVelocity, Vector2 contactNormal)
	{
		float speedTowardDown = playerVelocity.Dot(_downVector);
		if (speedTowardDown >= SpeedThreshold)
		{
			this.SetDeferred(PropertyName.Visible, false);
			this.coll.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

			BreakableBreakEffect bfe = new();
			GetTree().CurrentScene.AddChild(bfe);

			bfe.GlobalPosition = this.GlobalPosition;
			bfe.Init(this, new Color("#424252"));
		}
	}

	public override void _Draw()
	{
		if (BreakableTexture == null) return;
		CollisionShape2D coll = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (coll?.Shape is not RectangleShape2D rect) return; // this will probably happen later, doesnt happen rn


		Vector2 worldSize = rect.Size * Scale;
		DrawSetTransform(Vector2.Zero, 0f, new Vector2(PixelScale, PixelScale) / Scale);
		DrawTextureRect(BreakableTexture, new Rect2(-worldSize / (2f * PixelScale), worldSize / PixelScale), tile: true);
		DrawSetTransform(Vector2.Zero, 0f);

	}

}
