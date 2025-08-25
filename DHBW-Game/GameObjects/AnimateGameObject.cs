using GameLibrary.Physics;

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
    Fall
}

public record AnimationReturn(Facing Facing, State State);
public class AnimateGameObject
{
    private bool _OnGround;
    private Facing _FacingUserInput;
    private Facing _FacingPhysics;
    private State _State;
    private bool _userDirectionInput;

    public AnimateGameObject()
    {
        _OnGround = false;
        _State = State.Idle;
    }

    public virtual AnimationReturn GetAnimation(PhysicsComponent physicsComponent)
    {
        _State = State.Fall;
        _OnGround = false;

        if (physicsComponent.Velocity.Y == 0f)
        {
            _OnGround = true;
        }

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

        if ((physicsComponent.Velocity.X != 0f) && (_OnGround))
        {
            _State = State.Run;
        }

        return new AnimationReturn(_FacingPhysics, _State);
    }

    public AnimationReturn GetAnimation(bool KeyUp, bool KeyDown, bool KeyLeft, bool KeyRight, PhysicsComponent physicsComponent)
    {
        _userDirectionInput = false;
        _State = State.Fall;
        _OnGround = false;

        if (KeyLeft || KeyRight)
        {
            _userDirectionInput = true;
        }

        if (physicsComponent.Velocity.Y == 0f)
        {
            _OnGround = true;
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

        if (_userDirectionInput)
        {
            return new AnimationReturn(_FacingUserInput, _State);
        }

        if (physicsComponent.Velocity.X > 0f)
        {
            _FacingPhysics = Facing.Right;
        }
        if (physicsComponent.Velocity.X < 0f)
        {
            _FacingPhysics = Facing.Left;
        }

        if ((physicsComponent.Velocity.X != 0f) && (_OnGround))
        {
            _State = State.Run;
        }

        return new AnimationReturn(_FacingPhysics, _State);
    }
}