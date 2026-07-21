using System;
using System.ComponentModel;
using Godot;

public partial class Player : CharacterBody2D
{
	[Export] public float GravityStrength = 1400f; // per second
	[Export] public float Friction = 800f;
	[Export] public float MaxSpeed = 1500f;
	[Export] public Vector2 BodySize = new Vector2(28, 28);
	[Export] public Color DeathParticleColor = new Color(49/255f, 102/255f, 198/255f);

	[Export] public SwitchCounter switchCounter;
	[Export] public SwitchUi switchUi;

	private Vector2 _gravityDir = Vector2.Down;

	private const float DeathHoldThreshold = 1.5f;
	private float _holdTime = 0.5f;
	private bool _isHolding;
	private bool _hasExploded = false;

	private ColorRect _vignetteRect;
	private ShaderMaterial _vignetteMat;
	private float _vignetteTime;
	private Camera2D _camera;
	private Vector2 _cameraBaseZoom = Vector2.One;

	public float spriteSize;
	private Sprite2D _sprite;
	private Vector2 _collCenter;
	private Vector2 _collHalfSize;

	private float _squishY = 1f;
	private Tween _squishTween;
	private bool _wasGrounded = false;
	private float _currentYScale = 1f;
	
	public bool Frozen = false;
	private bool _isDead = false;
	private bool switchesLimited;

	public override async void _Ready()
	{
		this.TopLevel = true; // render over other things, mostly for arrow
		this.UpDirection = -_gravityDir;

		switchesLimited = switchCounter != null && switchUi != null;

		_sprite = GetNode<Sprite2D>("Sprite2D"); // todo shift to exported property
		spriteSize = _sprite.Scale.X; // should always be a square

		CollisionShape2D collShape = GetNode<CollisionShape2D>("CollisionShape2D");
		_collCenter = collShape.Position;
		_collHalfSize = ((RectangleShape2D)collShape.Shape).Size / 2f;

		InitHoldEffect();

		if (!switchesLimited) return;
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		switchUi.UpdateSwitches(switchCounter.Limit);
	}

	#region key handling, hold to restart logic
	public override void _Process(double delta)
	{
		// keypress stuff
		if (Input.IsActionJustPressed("Button"))
		{
			// flip
			if (switchesLimited && switchCounter.GetRemaining() == 0)
				Die();
			FlipGravity();

			_isHolding = true;
			_holdTime = 0.0f;
			_hasExploded = false;
		}
		if (_isHolding && Input.IsActionPressed("Button") && Mathf.Abs(Velocity.Length()) < 0.002)
		{
			_holdTime += (float)delta;
			UpdateHoldEffect(_holdTime / DeathHoldThreshold, (float)delta);

			if (_holdTime >= DeathHoldThreshold && !_hasExploded)
			{
				_hasExploded = true;
				_isHolding = false;
				Die();
			}
		}
		if (Input.IsActionJustReleased("Button"))
		{
			_isHolding = false;
			ClearHoldEffect();
		}

		if (Input.IsActionJustPressed("Menu"))
			GetTree().ChangeSceneToFile("res://MainMenu.tscn");

		UpdateSprite();
		QueueRedraw(); // todo?
	}

	private void InitHoldEffect()
	{
		_camera = new Camera2D { Enabled = false };
		AddChild(_camera);
		_cameraBaseZoom = _camera.Zoom;

		Shader shader = new() { Code = VignetteShader };
		_vignetteMat = new ShaderMaterial { Shader = shader };

		_vignetteRect = new ColorRect
		{
			AnchorRight = 1f,
			AnchorBottom = 1f,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Material = _vignetteMat,
			Visible = false
		};

		CanvasLayer layer = new() { Layer = 50 };
		AddChild(layer);
		layer.AddChild(_vignetteRect);
	}

	private void UpdateHoldEffect(float rawT, float dt)
	{
		const float DeadZone = 0.2f;
		if (rawT < DeadZone) return;
		float t = (rawT - DeadZone) / (1f - DeadZone);

		_vignetteTime += dt;
		_vignetteRect.Visible = true;
		_vignetteMat.SetShaderParameter("progress", t);
		_vignetteMat.SetShaderParameter("time", _vignetteTime);

		if (_camera == null) // this is sort of a holdover from a previous zoom handling but im too scared to delete it
		{
			_camera = GetViewport().GetCamera2D();
			if (_camera != null) _cameraBaseZoom = _camera.Zoom;
		}
		if (_camera != null) {
			_camera.Enabled = true;
			_camera.Zoom = _cameraBaseZoom * Mathf.Lerp(1f, 1.3f, Mathf.SmoothStep(0f, 1f, t));

			const float maxShake = 14f;
			float shakeAmt = t*t * maxShake;
			_camera.Offset = new Vector2(
				Mathf.Sin(_vignetteTime * Mathf.Lerp(8f, 24f, t)) * shakeAmt,
				Mathf.Sin(_vignetteTime * Mathf.Lerp(11f, 30f, t) + 1.3f) * shakeAmt
			);
		}
	}

	public void ClearHoldEffect()
	{
		_vignetteTime = 0f;
		if (_vignetteRect != null) _vignetteRect.Visible = false;
		if (_camera != null) { 
			_camera.Enabled = false;
			_camera.Zoom = _cameraBaseZoom;
			_camera.Offset = Vector2.Zero;
		}
	}

	private const string VignetteShader = @"
	shader_type canvas_item;
	uniform float progress : hint_range(0.0, 1.0) = 0.0;
	uniform float time = 0.0;

	void fragment() {
		vec2 uv = UV - vec2(0.5);
		float dist = length(uv);

		// ring closes inward
		float inner = 0.5 - progress * 0.32;
		float outer = inner + 0.22;

		// pulse rate accelerates
		float pulse = sin(time * (4.0 + progress * 12.0)) * 0.018 * progress;

		float vignette = smoothstep(inner + pulse, outer, dist);

		vec3 col = mix(vec3(0.0), vec3(0.25, 0.0, 0.0), progress);
		COLOR = vec4(col, clamp(vignette * progress * 1.5, 0.0, 0.9));
	}
	";
	#endregion

	#region movement
	public override void _PhysicsProcess(double delta)
	{
		if (Frozen) return;

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
		_sprite.Scale = new Vector2(spriteSize * xScale, spriteSize * yScale);

		float halfExtent = _collHalfSize.Dot(_gravityDir.Abs());
		Vector2 floorEdge = _collCenter + _gravityDir * halfExtent;
		
		_sprite.Position = floorEdge - _gravityDir * ((spriteSize/2*16) * yScale);

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

	private void FlipGravity()
	{
		Vector2 newDir = GetTargetGravityDir();
		if (newDir == _gravityDir) return;
		if (!Timer.Instance.Started) Timer.Instance.Start();

		_gravityDir = newDir;
		UpDirection = -_gravityDir;
		if (switchesLimited)
		{
			switchCounter.Switched();
			switchUi.UpdateSwitches(switchCounter.GetRemaining());
		}
	}
	#endregion

	public void Die()
	{
		ClearHoldEffect();

		if (_isDead) return;
		_isDead = true;

		this.Visible = false;
		SetPhysicsProcess(false);
		SetProcess(false);

		SlimeDeathEffect effect = new();
		GetTree().CurrentScene.AddChild(effect);
		
		effect.GlobalPosition = this.GlobalPosition;
		effect.Init(_gravityDir, DeathParticleColor, spriteSize);
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
			arrowOrigin = _sprite.Position + _gravityDir * (2.5f * spriteSize); // 2.5f being the offset from tex center to visual slime center
		}
		Vector2 start = arrowOrigin + dir * (hangPx*spriteSize);
		Vector2 tip = start + dir * 30f;
		
		Color arrowColor = new Color(1f, 0.9f, 0.2f);
		DrawLine(start, tip, arrowColor, 4f);
		Vector2 off = dir.Rotated(Mathf.Pi / 2f) * 8f;
		DrawLine(tip, tip - dir*14f + off, arrowColor, 4f);
		DrawLine(tip, tip - dir*14f - off, arrowColor, 4f);
	}
}
