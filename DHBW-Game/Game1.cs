#define PhysicsCollisionMovementTest

using DHBW_Game.Scenes;
using GameLibrary;
using GameLibrary.Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DHBW_Game;

public class Game1 : Core
{
    private PhysicsEngine _physicsEngine;

    public Game1() : base("DHBW Game", 1280, 720, false)
    {

    }

    protected override void Initialize()
    {
        base.Initialize();
        
        _physicsEngine = new PhysicsEngine(this);
        Components.Add(_physicsEngine);
        
#if PhysicsCollisionMovementTest
        TestScene testScene = new TestScene(_physicsEngine);
        
        ChangeScene(testScene);
#endif
    }

    protected override void LoadContent()
    {
        
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);
    }
}