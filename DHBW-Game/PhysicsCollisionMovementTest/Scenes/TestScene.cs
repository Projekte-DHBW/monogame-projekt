using DHBW_Game.GameObjects;
using DHBW_Game.Maps;
using GameLibrary;
using GameLibrary.Graphics;
using GameLibrary.Input;
using GameLibrary.Physics;
using GameLibrary.Physics.Colliders;
using GameLibrary.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DHBW_Game.Scenes;

public class TestScene : Scene
{
    // Player character
    private TestCharacter _character;

    private AnimatedSprite _playerRunning;
    private Sprite _playerStanding;

    private Vector2 _playerPosition;

    private Rectangle _roomBounds;

    private SpriteEffects _spriteEffects = SpriteEffects.None;

    private enum PlayerState
    {
        Standing,
        Walking
    }

    private PlayerState _currentPlayerState = PlayerState.Standing;

    // The map instance
    private Map _map;
    
    private readonly CollisionEngine _collisionEngine;
    private readonly PhysicsEngine _physicsEngine;


    public TestScene()
    {
        _physicsEngine = ServiceLocator.Get<PhysicsEngine>();
        _collisionEngine = _physicsEngine.CollisionEngine;
    }
    
    public override void Initialize()
    {
        // LoadContent is called during base.Initialize().
        base.Initialize();
        
        Core.ExitOnEscape = true;
        
        // Initialize a new game to be played.
        InitializeNewGame();
    }
    
    private void InitializeNewGame()
    {
        // Load the map from an XML configuration file using the content manager.
        _map = Map.FromFile(Core.Content, "Maps/test_map.xml");
        
        // Create the player character separately, using the start position from the map.
        _character = new TestCharacter(mass: 2f, isElastic: false);
        _character.Initialize(_map.StartPosition);

        // Set the initial player position
        _playerPosition = _map.StartPosition;
       
    }
    
    public override void LoadContent()
    {
        // Load the player texture atlases
        var playerRunning_atlas = TextureAtlas.FromFile(Core.Content, "Animated_Sprites/Player/Run-definition.xml");
        var playerStanding_atlas = TextureAtlas.FromFile(Core.Content, "Animated_Sprites/Player/Idle-definition.xml");

        // Create the player sprite for running
        _playerRunning = playerRunning_atlas.CreateAnimatedSprite("running-animation");
        _playerRunning.Scale = new Vector2(4.0f, 4.0f);

        // Create the player sprite for standing
        var standingRegion = playerStanding_atlas.GetRegion("standing");
        _playerStanding = new Sprite(standingRegion);
        _playerStanding.Scale = new Vector2(4.0f, 4.0f);

    }
    
    public override void Update(GameTime gameTime)
    {
        // Update the map (which updates all placed game objects).
        _map.Update(gameTime);
        
        // Update the player character.
        _character.Update(gameTime);

        // Update the player sprite based on the current state
        if (_currentPlayerState == PlayerState.Walking)
        {
            _playerRunning.Update(gameTime);
        }

        // Check for keyboard input and handle it
        CheckKeyboardInput();

    }

    private void CheckKeyboardInput()
    {
        KeyboardInfo keyboard = Core.Input.Keyboard;

        float speed = 5.0f; // Define movement speed

        if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up))
        {
            _playerPosition.Y -= speed;
            _currentPlayerState = PlayerState.Walking;
        }

        if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down))
        {
            _playerPosition.Y += speed;
            _currentPlayerState = PlayerState.Walking;
        }

        if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
        {
            _playerPosition.X -= speed;
            _spriteEffects = SpriteEffects.FlipHorizontally;
            _currentPlayerState = PlayerState.Walking;
        }

        if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
        {
            _playerPosition.X += speed;
            _spriteEffects = SpriteEffects.None;
            _currentPlayerState = PlayerState.Walking;
        }

        if ((keyboard.IsKeyUp(Keys.A)) && (keyboard.IsKeyUp(Keys.Left)) && (keyboard.IsKeyUp(Keys.Right)) && (keyboard.IsKeyUp(Keys.D)) && (keyboard.IsKeyUp(Keys.S)) && (keyboard.IsKeyUp(Keys.Down)) && (keyboard.IsKeyUp(Keys.W)) && (keyboard.IsKeyUp(Keys.Up)))
        {
            _currentPlayerState = PlayerState.Standing;
        }
    }

    public override void Draw(GameTime gameTime)
    {
        // Clear the back buffer.
        Core.GraphicsDevice.Clear(Color.CornflowerBlue);
        
        // Begin the sprite batch.
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Draw the map (which draws the background tilemap and all placed game objects).
        _map.Draw(Core.SpriteBatch);
        
        // Draw the player character.
        _character.Draw();

        // Draw the player sprite based on the current state
        if (_currentPlayerState == PlayerState.Walking)
        {
            _playerRunning.Draw(Core.SpriteBatch, _playerPosition, _spriteEffects);
        }
        else
        {
            _playerStanding.Draw(Core.SpriteBatch, _playerPosition, _spriteEffects); // Draw the standing sprite
        }

        // Visualize the colliders. This enables debugging the colliders without relying on sprites which don't exactly depict the colliders.
        _collisionEngine.VisualizeColliders();

        // End the sprite batch when finished.
        Core.SpriteBatch.End();
    }
}