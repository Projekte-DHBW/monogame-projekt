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


    public TestScene(PhysicsEngine physicsEngine)
    {
        _physicsEngine = physicsEngine;
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
        // Create rectangle colliders which are static and act as walls/platforms/ground.
        RectangleCollider rectangleCollider1 = new RectangleCollider(400, 500, 400, 50, 25);
        RectangleCollider rectangleCollider2 = new RectangleCollider(900, 600, 800, 50, 0);
        RectangleCollider rectangleCollider3 = new RectangleCollider(50, 400, 800, 50, 90);
        RectangleCollider rectangleCollider4 = new RectangleCollider(1200, 400, 800, 50, 90);
        RectangleCollider rectangleCollider5 = new RectangleCollider(200, 400, 500, 50, 0);
        RectangleCollider rectangleCollider6 = new RectangleCollider(200, 20, 500, 50, 0);
        
        // Add these rectangle colliders to the collision engine. Note: they are not physics objects and thus don't have to be added to the physics engine.
        _collisionEngine.Add(rectangleCollider1);
        _collisionEngine.Add(rectangleCollider2);
        _collisionEngine.Add(rectangleCollider3);
        _collisionEngine.Add(rectangleCollider4);
        _collisionEngine.Add(rectangleCollider5);
        _collisionEngine.Add(rectangleCollider6);
        
        // Initialize the game objects at their starting position.
        _character.Initialize(startingPosition: new Vector2(500, 50));
        _circleColliderTest.Initialize(startingPosition: new Vector2(400, 100));
        _rectangleColliderTest.Initialize(startingPosition: new Vector2(500, 200));
        
        // Add the game objects to the physics engine (and thereby also to the collision engine).
        _physicsEngine.Add(_character);
        _physicsEngine.Add(_circleColliderTest);
        _physicsEngine.Add(_rectangleColliderTest);
    }
    
    public override void LoadContent()
    {
        // Create the game objects. In LoadContent() because here we could pass an animated sprite into the constructor.
        _character = new TestCharacter(mass: 2f, isElastic: false);
        _circleColliderTest = new CircleColliderTest(mass: 1f, isElastic: true);
        _rectangleColliderTest = new RectangleColliderTest(mass: 1f, isElastic: true);
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