#define PhysicsCollisionMovementTest

using DHBW_Game.Scenes;
using GameLibrary;
using GameLibrary.Physics;
using GameLibrary.Rendering;
using Microsoft.Xna.Framework;

namespace DHBW_Game;

public class Game1 : Core
{
    private PhysicsEngine _physicsEngine;
    private Camera _camera;

    public Game1() : base("DHBW Game", 1280, 720, false)
    {
    }

    protected override void Initialize()
    {
        base.Initialize();
        
        _physicsEngine = new PhysicsEngine(this);
        Components.Add(_physicsEngine);
        ServiceLocator.Register(_physicsEngine);
        ServiceLocator.Register(_physicsEngine.CollisionEngine);
        
        _camera = new Camera();
        ServiceLocator.Register(_camera);
        
#if PhysicsCollisionMovementTest
        TestScene testScene = new TestScene();
        
        ChangeScene(testScene);
#endif
    }

    protected override void LoadContent()
    {
        
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        _camera.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
    }
}