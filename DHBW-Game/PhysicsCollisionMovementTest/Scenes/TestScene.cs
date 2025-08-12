using DHBW_Game.GameObjects;
using GameLibrary;
using GameLibrary.Physics;
using GameLibrary.Physics.Colliders;
using GameLibrary.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DHBW_Game.Scenes;

public class TestScene : Scene
{
    // Game objects
    private TestCharacter _character;
    private CircleColliderTest _circleColliderTest;
    private RectangleColliderTest _rectangleColliderTest;
    
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
        // Create segments which are static and act as walls/platforms/ground.
        TestSegment testSegment1 = new TestSegment(400, 50, 25);
        TestSegment testSegment2 = new TestSegment(800, 50, 0);
        TestSegment testSegment3 = new TestSegment(800, 50, 90);
        TestSegment testSegment4 = new TestSegment(800, 50, 90);
        TestSegment testSegment5 = new TestSegment(500, 50, 0);
        TestSegment testSegment6 = new TestSegment(500, 50, 0);
        
        // Initialize the segments at their starting position.
        testSegment1.Initialize(new Vector2(400, 500));
        testSegment2.Initialize(new Vector2(900, 600));
        testSegment3.Initialize(new Vector2(50, 400));
        testSegment4.Initialize(new Vector2(1200, 400));
        testSegment5.Initialize(new Vector2(200, 400));
        testSegment6.Initialize(new Vector2(200, 20));
        
        // Create the dynamic game objects.
        _character = new TestCharacter(mass: 2f, isElastic: false);
        _circleColliderTest = new CircleColliderTest(mass: 1f, isElastic: true);
        _rectangleColliderTest = new RectangleColliderTest(mass: 1f, isElastic: true);
        
        // Initialize the dynamic game objects at their starting position.
        _character.Initialize(startingPosition: new Vector2(300, 200));
        _circleColliderTest.Initialize(startingPosition: new Vector2(400, 100));
        _rectangleColliderTest.Initialize(startingPosition: new Vector2(500, 200));
    }
    
    public override void LoadContent()
    {
    }
    
    public override void Update(GameTime gameTime)
    {
        // Update the game objects.
        _character.Update(gameTime);
        _circleColliderTest.Update(gameTime);
        _rectangleColliderTest.Update(gameTime);
    }
    
    public override void Draw(GameTime gameTime)
    {
        // Clear the back buffer.
        Core.GraphicsDevice.Clear(Color.CornflowerBlue);
        
        // Begin the sprite batch.
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Draw the game objects.
        _character.Draw();
        _circleColliderTest.Draw();
        _rectangleColliderTest.Draw();
        
        // Visualize the colliders. This enables debugging the colliders without relying on sprites which don't exactly depict the colliders.
        _collisionEngine.VisualizeColliders();

        // End the sprite batch when finished.
        Core.SpriteBatch.End();
    }
}