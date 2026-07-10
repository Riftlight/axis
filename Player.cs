using Godot;

public partial class Player : CharacterBody2D
{
	[Export] public float GravityStrength = 1400f; // per second
	[Export] public float Friction = 800f;
	[Export] public float MaxSpeed = 1500f;
	[Export] public Vector2 BodySize = new Vector2(28, 28);

	[Export] public SwitchCounter switchCounter;
	[Export] public SwitchUi switchUi;

	private Vector2 _gravityDir = Vector2.Down;

	private Sprite2D _sprite;
	private float _spriteSize;
	private Vector2 _collCenter;
	private Vector2 _collHalfSize;

	private float _squishY = 1f;
	private Tween _squishTween;
	private bool _wasGrounded = false;
	
	private float _currentYScale = 1f;

	public override void _Ready()
	{
		this.TopLevel = true; // render over other things, mostly for arrow
		this.UpDirection = -_gravityDir;

		_sprite = GetNode<Sprite2D>("Sprite2D"); // todo shift to exported property
		_spriteSize = _sprite.Scale.X; // should always be a square

		CollisionShape2D collShape = GetNode<CollisionShape2D>("CollisionShape2D");
		_collCenter = collShape.Position;
		_collHalfSize = ((RectangleShape2D)collShape.Shape).Size / 2f;

		if (switchUi != null && switchCounter != null)
			switchUi.UpdateSwitches(switchCounter.GetRemaining());
	}

	public override void _PhysicsProcess(double delta)
	{
		Velocity += _gravityDir * GravityStrength * (float)delta;
		Velocity = Velocity.LimitLength(MaxSpeed);
		
		// Friction
		if (IsGrounded())
		{
			// GD.Print(" on flooor ! " + _gravityDir);
			Vector2 surfaceDir = new Vector2(-_gravityDir.Y, _gravityDir.X);
			float lateralSpeed = Velocity.Dot(surfaceDir);
			float lessened = Mathf.MoveToward(lateralSpeed, 0f, Friction*(float) delta);
			Velocity += surfaceDir * (lessened - lateralSpeed);
		}

		// Break breakables
		Vector2 preSlideVelocity = Velocity;
		MoveAndSlide();
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			KinematicCollision2D coll = GetSlideCollision(i);
			if (coll.GetCollider() is Breakable breakable)
				breakable.PlayerHit(preSlideVelocity, coll.GetNormal());
		}

		bool grounded = IsGrounded();
		if (grounded && !_wasGrounded)
			LandingSquish(preSlideVelocity.Dot(_gravityDir));
		_wasGrounded = grounded;
	}

	public override void _Process(double delta)
	{
		UpdateSprite();
		QueueRedraw(); // todo
	}

	private void UpdateSprite()
	{
		// rotate the slime so its bottom is in the current down direction
		_sprite.Rotation = _gravityDir.Angle() - Mathf.Pi / 2f;

		Vector2 lateralDir = new Vector2(-_gravityDir.Y, _gravityDir.X);
		float fallSpeed = Velocity.Dot(_gravityDir);
		float lateralSpeed = Velocity.Dot(lateralDir);

		float yScale =
			(1f
			+ Mathf.Clamp(fallSpeed/MaxSpeed, -0.2f, 1f)
			- Mathf.Clamp(Mathf.Abs(lateralSpeed) / MaxSpeed, 0f, 0.15f))
			* _squishY;
		float xScale = 1f / yScale;

		_currentYScale = yScale;
		_sprite.Scale = new Vector2(_spriteSize * xScale, _spriteSize * yScale);

		float halfExtent = _collHalfSize.Dot(_gravityDir.Abs());
		Vector2 floorEdge = _collCenter + _gravityDir * halfExtent;
		
		_sprite.Position = floorEdge - _gravityDir * ((_spriteSize/2*16) * yScale);

	}

	private bool IsGrounded()
	{
		for (int i = 0; i < GetSlideCollisionCount(); i++)
			if (GetSlideCollision(i).GetNormal().Dot(-_gravityDir) > 0.7f) // 0.7 is approx cos(45deg), could lower threshold but should work fine on everything AA
				return true;
		return false;
	}

	private void LandingSquish(float impactSpeed)
	{
		float t = Mathf.Clamp(impactSpeed / MaxSpeed, 0f, 1f);
		_squishY = Mathf.Lerp(1f, 0.35f, t);

		_squishTween?.Kill();
		_squishTween = CreateTween();
		_squishTween.SetEase(Tween.EaseType.Out);
		_squishTween.SetTrans(Tween.TransitionType.Elastic);
		_squishTween.TweenMethod(Callable.From<float>(v => _squishY = v), _squishY, 1f, 0.5f);
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
		if (switchUi != null && switchCounter != null && switchCounter.GetRemaining() == 0) return;

		Vector2 newDir = GetTargetGravityDir();
		if (newDir == _gravityDir) return;

		_gravityDir = newDir;
		UpDirection = -_gravityDir;
		if (switchUi != null && switchCounter != null)
		{
			switchCounter.Switched();
			switchUi.UpdateSwitches(switchCounter.GetRemaining());
		}
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
		// gravity arrow
		Vector2 dir = GetTargetGravityDir();
		float inGravity = dir.Dot(_gravityDir); // 1 = floor, -1 = ceiling, 0 = horiz

		// pixel distance from tex center to visual slime edge
		float hangPx;
		Vector2 arrowOrigin = _sprite.Position;
		if (inGravity > 0.5f) 
			hangPx = 8f * _currentYScale;
		else if (inGravity < -0.5f) 
			hangPx = 3f * _currentYScale;
		else 
		{
			hangPx = 7f * (1f / _currentYScale);
			arrowOrigin = _sprite.Position + _gravityDir * (2.5f * _spriteSize); // 2.5f being the offset from tex center to visual slime center
		}
		Vector2 start = arrowOrigin + dir * (hangPx*_spriteSize);
		Vector2 tip = start + dir * 30f;
		
		Color arrowColor = new Color(1f, 0.9f, 0.2f);
		DrawLine(start, tip, arrowColor, 4f);
		Vector2 off = dir.Rotated(Mathf.Pi / 2f) * 8f;
		DrawLine(tip, tip - dir*14f + off, arrowColor, 4f);
		DrawLine(tip, tip - dir*14f - off, arrowColor, 4f);
	}
}
