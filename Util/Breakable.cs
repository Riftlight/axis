using Godot;

public partial class Breakable : StaticBody2D
{

	[Export] public float SpeedThreshold = 1000f;

	private Vector2 _downVector;
	private CollisionShape2D coll;

	public override void _Ready()
	{
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
			GD.Print(speedTowardDown + " vs threshold of " + SpeedThreshold);
			this.SetDeferred(PropertyName.Visible, false);
			this.coll.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		}
	}
}
