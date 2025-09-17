using System;
using GameLibrary.Physics;
using Microsoft.Xna.Framework;

namespace GameObjects.Animate;

public enum Facing
{
    Left,
    Right
}

public enum State
{
    Idle,
    Run,
    Jump,
    Fall,
    Slide
}

public record AnimationReturn(Facing Facing, State State);

public class AnimateGameObject
{
    private bool _OnGround;
    private Facing _FacingUserInput;
    private Facing _FacingPhysics;
    private State _State;
    private bool _userDirectionInput;
    private Facing _lastFacing;  // For hysteresis to avoid low-speed flips
    private State _lastState;    // For hysteresis on state changes

    public AnimateGameObject()
    {
        _OnGround = false;
        _State = State.Idle;
        _lastFacing = Facing.Right;  // Default initial facing
        _lastState = State.Idle;
    }

    public virtual AnimationReturn GetAnimation(PhysicsComponent physicsComponent)
    {
        _State = State.Fall;
        _OnGround = physicsComponent.Collider.IsOnGround;

        if (_OnGround)
        {
            _State = State.Idle;
        }
        else if (physicsComponent.Velocity.Y < 0)
        {
            _State = State.Jump;
        }

        if (physicsComponent.Velocity.X > 0f)
        {
            _FacingPhysics = Facing.Right;
        }
        if (physicsComponent.Velocity.X < 0f)
        {
            _FacingPhysics = Facing.Left;
        }

        // Hysteresis for facing/state to smooth low-speed flips on slopes
        float facingThreshold = 2f;  // Retain last at very low X speed
        if (Math.Abs(physicsComponent.Velocity.X) > facingThreshold)
        {
            _lastFacing = _FacingPhysics;
        }
        else
        {
            _FacingPhysics = _lastFacing;
        }

        // Set Run before slide detection, so slide can overwrite if applicable
        if ((Math.Abs(physicsComponent.Velocity.X) > facingThreshold) && _OnGround)
        {
            _State = State.Run;
        }

        // Detect sliding (on ground + downward velocity component + significant slope)
        float slopeThreshold = MathHelper.ToRadians(5f);  // ~5 degrees; ignore near-flat
        float downThreshold = 2f;  // Small positive Y velocity for downward slide
        if (_OnGround && physicsComponent.Velocity.Y > downThreshold && Math.Abs(physicsComponent.Collider.SlopeAngle) > slopeThreshold)
        {
            _State = State.Slide;  // Lock to slide state
        }

        if (_State == _lastState)
        {
            _State = _lastState;  // Retain to reduce chatter
        }
        else
        {
            _lastState = _State;
        }

        return new AnimationReturn(_FacingPhysics, _State);
    }

    public AnimationReturn GetAnimation(bool KeyUp, bool KeyDown, bool KeyLeft, bool KeyRight, PhysicsComponent physicsComponent)
    {
        _userDirectionInput = false;
        _State = State.Fall;
        _OnGround = physicsComponent.Collider.IsOnGround;

        if (KeyLeft || KeyRight)
        {
            _userDirectionInput = true;
        }

        if (KeyLeft && !KeyRight)
        {
            _FacingUserInput = Facing.Left;
        }
        if (!KeyLeft && KeyRight)
        {
            _FacingUserInput = Facing.Right;
        }

        if (_OnGround)
        {
            _State = State.Idle;
            if (_userDirectionInput)
            {
                _State = State.Run;
            }
        }
        else if (physicsComponent.Velocity.Y < 0)
        {
            _State = State.Jump;
        }

        // Hysteresis for facing/state
        float facingThreshold = 2f;
        if (Math.Abs(physicsComponent.Velocity.X) > facingThreshold)
        {
            if (physicsComponent.Velocity.X > 0f)
            {
                _FacingPhysics = Facing.Right;
            }
            else if (physicsComponent.Velocity.X < 0f)
            {
                _FacingPhysics = Facing.Left;
            }
            _lastFacing = _FacingPhysics;
        }
        else
        {
            _FacingPhysics = _lastFacing;
        }

        // Set Run (for momentum, no input) before slide, so slide can overwrite
        if ((Math.Abs(physicsComponent.Velocity.X) > facingThreshold) && _OnGround)
        {
            _State = State.Run;
        }

        // Detect sliding (on ground + downward velocity component + significant slope + no upward input)
        float slopeThreshold = MathHelper.ToRadians(5f);
        float downThreshold = 2f;
        if (_OnGround && physicsComponent.Velocity.Y > downThreshold && Math.Abs(physicsComponent.Collider.SlopeAngle) > slopeThreshold && !KeyUp)
        {
            _State = State.Slide;  // Lock to slide
        }

        if (_userDirectionInput)
        {
            _lastFacing = _FacingUserInput;  // Update last on input
            return new AnimationReturn(_FacingUserInput, _State);
        }

        if (_State == _lastState)
        {
            _State = _lastState;
        }
        else
        {
            _lastState = _State;
        }

        return new AnimationReturn(_FacingPhysics, _State);
    }
}