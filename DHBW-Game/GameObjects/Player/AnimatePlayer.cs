using GameLibrary.Physics;

namespace GameObjects.Player;

public enum PlayerFacing
{
    Left,
    Right
}

public enum PlayerState
{
    Idle,
    Run,
    Jump,
    Fall
}

public record PlayerAnimationReturn(PlayerFacing Facing, PlayerState State);
public class AnimatePlayer
{
    private bool _playerOnGround;
    private PlayerFacing _playerFacingUserInput;
    private PlayerFacing _playerFacingPhysics;
    private PlayerState _playerState;
    private bool _userDirectionInput;
    private PlayerAnimationReturn _playerAnimationReturn;

    public AnimatePlayer()
    {
        _playerOnGround = false;
        _playerState = PlayerState.Idle;
    }

    public PlayerAnimationReturn GetAnimation(bool KeyUp, bool KeyDown, bool KeyLeft, bool KeyRight, PhysicsComponent physicsComponent)
    {
        _userDirectionInput = false;
        _playerState = PlayerState.Fall;
        _playerOnGround = false;

        if (KeyLeft || KeyRight)
        {
            _userDirectionInput = true;
        }

        if (physicsComponent.Velocity.Y == 0f)
        {
            _playerOnGround = true;
        }

        if (KeyLeft && !KeyRight)
        {
            _playerFacingUserInput = PlayerFacing.Left;
        }
        if (!KeyLeft && KeyRight)
        {
            _playerFacingUserInput = PlayerFacing.Right;
        }

        if (_playerOnGround)
        {
            _playerState = PlayerState.Idle;
            if (_userDirectionInput)
            {
                _playerState = PlayerState.Run;
            }
        } else if (physicsComponent.Velocity.Y < 0)
        {
            _playerState = PlayerState.Jump;
        }

        if (_userDirectionInput)
        {
            return new PlayerAnimationReturn(_playerFacingUserInput, _playerState);
        }

        if (physicsComponent.Velocity.X > 0f)
        {
            _playerFacingPhysics = PlayerFacing.Right;
        }
        if (physicsComponent.Velocity.X < 0f)
        {
            _playerFacingPhysics = PlayerFacing.Left;
        }

        if ((physicsComponent.Velocity.X != 0f) && (_playerOnGround))
        {
            _playerState = PlayerState.Run;
        }

        return new PlayerAnimationReturn(_playerFacingPhysics, _playerState);
    }
}