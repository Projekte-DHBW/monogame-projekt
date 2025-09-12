using DHBW_Game.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using GameLibrary;
using GameLibrary.Scenes;
namespace DHBW_Game.Scenes;
using MonoGameGum;
using GameLibrary.Graphics;

public class TitleScene : Scene
{
    private const string DUNGEON_TEXT = "DHBW";
    private const string SLIME_TEXT = "Game";

    private SpriteFont _font5x;

    // The position to draw the dungeon text at.
    private Vector2 _dungeonTextPos;

    // The origin to set for the dungeon text.
    private Vector2 _dungeonTextOrigin;

    // The position to draw the slime text at.
    private Vector2 _slimeTextPos;

    // The origin to set for the slime text.
    private Vector2 _slimeTextOrigin;

    // The MonoGame logo texture
    private Texture2D _logo;

    // 🔹 Hintergrundbild
    private Texture2D _background;

    private SoundEffect _uiSoundEffect;
    private TitlePanel _titlePanel;
    private OptionsPanel _optionsPanel;
    private QuestionSystemPanel _questionSystemPanel;

    // Reference to the texture atlas that we can pass to UI elements when they
    // are created.
    private TextureAtlas _atlas;
    
    public override void Initialize()
    {
        // LoadContent is called during base.Initialize().
        base.Initialize();

        // While on the title screen, we can enable exit on escape so the player
        // can close the game by pressing the escape key.
        Core.ExitOnEscape = true;

        _font5x = ServiceLocator.Get<SpriteFont>("Font5x");

        // Set the position and origin for the Dungeon text.
        Vector2 size = _font5x.MeasureString(DUNGEON_TEXT);
        _dungeonTextPos = new Vector2(1000, 100);
        _dungeonTextOrigin = size * 0.5f;

        // Set the position and origin for the Slime text.
        size = _font5x.MeasureString(SLIME_TEXT);
        _slimeTextPos = new Vector2(1000, 207);
        _slimeTextOrigin = size * 0.5f;

        InitializeUI();
    }
    
    private void InitializeUI()
    {
        // Clear out any previous UI in case we came here from
        // a different screen:
        GumService.Default.Root.Children.Clear();
    
        // Create title panel
        _titlePanel = new TitlePanel(_atlas, _uiSoundEffect, 
            () =>
            {
                Core.ChangeScene(new GameScene());
                ServiceLocator.Get<Game1>().Resume();
            },
            () => 
            {
                _titlePanel.Hide();
                _optionsPanel.Show();
            });
        _titlePanel.AddToRoot();
    
        // Create options panel
        _optionsPanel = new OptionsPanel(_atlas, _uiSoundEffect, 
            () =>
            {
                _optionsPanel.Hide();
                _titlePanel.Show();
            },
            () =>
            {
                _optionsPanel.Hide();
                _questionSystemPanel.Show();
            },
            true); // Include deep settings
        _optionsPanel.AddToRoot();

        // Create question system panel
        _questionSystemPanel = new QuestionSystemPanel(_atlas, _uiSoundEffect,
            () =>
            {
                _questionSystemPanel.Hide();
                _optionsPanel.Show();
            });
        _questionSystemPanel.AddToRoot();
    }

    public override void LoadContent()
    {
        // 🔹 Lade das Logo
        _logo = Content.Load<Texture2D>("images/logo");

        // 🔹 Lade den Pixel-Art-Hintergrund
        _background = Content.Load<Texture2D>("images/hintergrund");

        // Load the sound effect to play when ui actions occur.
        _uiSoundEffect = Core.Content.Load<SoundEffect>("audio/ui");
        // Load the texture atlas from the xml configuration file.
        _atlas = TextureAtlas.FromFile(Core.Content, "images/atlas-definition.xml");
    }
    
    public override void Update(GameTime gameTime)
    {
        GumService.Default.Update(gameTime);
        _questionSystemPanel.Activity();
    }

    public override void Draw(GameTime gameTime)
    {
        Core.GraphicsDevice.Clear(new Color(32, 40, 78, 255));

        // Begin the sprite batch to prepare for rendering.
        Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // 🔹 Hintergrundbild zeichnen (exakte Fenstergröße, kein Zoom)
        Core.SpriteBatch.Draw(
            _background,
            destinationRectangle: new Rectangle(0, 0, Core.Instance.Window.ClientBounds.Width, Core.Instance.Window.ClientBounds.Height),
            color: Color.White
        );

        // 🔹 Logo in der oberen linken Ecke zeichnen
        Vector2 position = new Vector2(10, 10);
        float scale = 0.2f;
        Core.SpriteBatch.Draw(
            _logo,
            position,
            null,
            Color.White,
            0f,
            Vector2.Zero,
            scale,
            SpriteEffects.None,
            0f
        );

        // The color to use for the drop shadow text.
        Color dropShadowColor = Color.Black * 0.5f;

        // Draw the Dungeon text slightly offset from it is original position and
        // with a transparent color to give it a drop shadow.
        Core.SpriteBatch.DrawString(_font5x, DUNGEON_TEXT, _dungeonTextPos + new Vector2(10, 10), dropShadowColor, 0.0f, _dungeonTextOrigin, 1.0f, SpriteEffects.None, 1.0f);

        // Draw the Dungeon text on top of that at its original position.
        Core.SpriteBatch.DrawString(_font5x, DUNGEON_TEXT, _dungeonTextPos, Color.White, 0.0f, _dungeonTextOrigin, 1.0f, SpriteEffects.None, 1.0f);

        // Draw the Slime text slightly offset from it is original position and
        // with a transparent color to give it a drop shadow.
        Core.SpriteBatch.DrawString(_font5x, SLIME_TEXT, _slimeTextPos + new Vector2(10, 10), dropShadowColor, 0.0f, _slimeTextOrigin, 1.0f, SpriteEffects.None, 1.0f);

        // Draw the Slime text on top of that at its original position.
        Core.SpriteBatch.DrawString(_font5x, SLIME_TEXT, _slimeTextPos, Color.White, 0.0f, _slimeTextOrigin, 1.0f, SpriteEffects.None, 1.0f);

        // Always end the sprite batch when finished.
        Core.SpriteBatch.End();

        if (_titlePanel.IsVisible)
        {
            // Begin the sprite batch to prepare for rendering.
            Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // The color to use for the drop shadow text.
            dropShadowColor = Color.Black * 0.5f;

            // Draw the Dungeon text slightly offset from it is original position and
            // with a transparent color to give it a drop shadow
            Core.SpriteBatch.DrawString(_font5x, DUNGEON_TEXT, _dungeonTextPos + new Vector2(10, 10), dropShadowColor, 0.0f, _dungeonTextOrigin, 1.0f, SpriteEffects.None, 1.0f);

            // Draw the Dungeon text on top of that at its original position
            Core.SpriteBatch.DrawString(_font5x, DUNGEON_TEXT, _dungeonTextPos, Color.White, 0.0f, _dungeonTextOrigin, 1.0f, SpriteEffects.None, 1.0f);

            // Draw the Slime text slightly offset from it is original position and
            // with a transparent color to give it a drop shadow
            Core.SpriteBatch.DrawString(_font5x, SLIME_TEXT, _slimeTextPos + new Vector2(10, 10), dropShadowColor, 0.0f, _slimeTextOrigin, 1.0f, SpriteEffects.None, 1.0f);

            // Draw the Slime text on top of that at its original position
            Core.SpriteBatch.DrawString(_font5x, SLIME_TEXT, _slimeTextPos, Color.White, 0.0f, _slimeTextOrigin, 1.0f, SpriteEffects.None, 1.0f);

            // Always end the sprite batch when finished.
            Core.SpriteBatch.End();
        }

        GumService.Default.Draw();
    }
}