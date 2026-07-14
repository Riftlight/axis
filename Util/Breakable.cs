using Godot;

public partial class Breakable : StaticBody2D
{

	[Export] public float SpeedThreshold = 1000f;
	[Export] public Texture2D BreakableTexture;

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
		}
	}

	public override void _Draw()
	{
		if (BreakableTexture == null) return;

		CollisionShape2D coll = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (coll?.Shape is not RectangleShape2D rect) return; // this will probably happen later, doesnt happen rn

		DrawSetTransform(Vector2.Zero, 0f, Vector2.One / Scale);
		DrawTextureRect(BreakableTexture, new Rect2(-rect.Size / 2f, rect.Size), true);
		DrawSetTransform(Vector2.Zero, 0f);
	}

}
