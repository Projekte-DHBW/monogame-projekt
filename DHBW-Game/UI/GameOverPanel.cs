using GameLibrary.Graphics;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework.Audio;
using System;
using DHBW_Game.Scenes;
using Gum.DataTypes;
using Gum.Managers;
using MonoGameGum.GueDeriving;
using GameLibrary;

namespace DHBW_Game.UI;

/// <summary>
/// Represents a UI panel which is shown when the player lost.
/// </summary>
public class GameOverPanel : Panel
{
    /// <summary>
    /// The restart button UI element.
    /// </summary>
    private AnimatedButton _restartButton;

    /// <summary>
    /// The UI sound effect to play when a UI event is triggered.
    /// </summary>
    private readonly SoundEffect _uiSoundEffect;

    /// <summary>
    /// Reference to the texture atlas that we can pass to UI elements when they
    /// are created.
    /// </summary>
    private readonly TextureAtlas _atlas;

    /// <summary>
    /// Initializes a new instance of the <see cref="PausePanel"/> class.
    /// </summary>
    /// <param name="atlas">The texture atlas used for UI elements.</param>
    /// <param name="uiSoundEffect">The sound effect to play on UI interactions.</param>
    /// <param name="onOptions">The action to invoke when the options button is clicked.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="atlas"/> or <paramref name="uiSoundEffect"/> is null.
    /// </exception>
    public GameOverPanel(TextureAtlas atlas, SoundEffect uiSoundEffect)
    {
        // Validate input parameters
        if (atlas == null) throw new ArgumentNullException(nameof(atlas));
        if (uiSoundEffect == null) throw new ArgumentNullException(nameof(uiSoundEffect));

        _uiSoundEffect = uiSoundEffect;
        _atlas = atlas;
        Anchor(Gum.Wireframe.Anchor.Center);
        Visual.WidthUnits = DimensionUnitType.Absolute;
        Visual.HeightUnits = DimensionUnitType.Absolute;
        Visual.Height = 70;
        Visual.Width = 264;

        IsVisible = false; // Initially hide the panel
        // Initialize child UI elements
        InitializeChildren(atlas);
    }

    /// <summary>
    /// Initializes the child UI elements of the pause panel.
    /// </summary>
    /// <param name="atlas">The texture atlas used for UI elements.</param>
    private void InitializeChildren(TextureAtlas atlas)
    {
        TextureRegion backgroundRegion = _atlas.GetRegion("panel-background");

        NineSliceRuntime background = new NineSliceRuntime();
        background.Dock(Gum.Wireframe.Dock.Fill);
        background.Texture = backgroundRegion.Texture;
        background.TextureAddress = TextureAddress.Custom;
        background.TextureHeight = backgroundRegion.Height;
        background.TextureLeft = backgroundRegion.SourceRectangle.Left;
        background.TextureTop = backgroundRegion.SourceRectangle.Top;
        background.TextureWidth = backgroundRegion.Width;
        AddChild(background);

        TextRuntime textInstance = new TextRuntime();
        textInstance.Text = "FAILED";
        textInstance.CustomFontFile = @"fonts/04b_30.fnt";
        textInstance.UseCustomFont = true;
        textInstance.FontScale = 0.5f;
        textInstance.X = 10f;
        textInstance.Y = 10f;
        AddChild(textInstance);

        _restartButton = new AnimatedButton(_atlas);
        _restartButton.Text = "RESTART";
        _restartButton.Anchor(Gum.Wireframe.Anchor.Left);
        _restartButton.Visual.X = 9f;
        _restartButton.Visual.Y = 19f;
        _restartButton.Click += HandleRestartButtonClicked;
        AddChild(_restartButton);

        AnimatedButton quitButton = new AnimatedButton(_atlas);
        quitButton.Text = "QUIT";
        quitButton.Anchor(Gum.Wireframe.Anchor.BottomRight);
        quitButton.Visual.X = -9f;
        quitButton.Visual.Y = -9f;
        quitButton.Click += HandleQuitButtonClicked;
        AddChild(quitButton);
    }

    /// <summary>
    /// Handles the click event for the restart button.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void HandleRestartButtonClicked(object sender, EventArgs e)
    {
        // A UI interaction occurred, play the sound effect
        Core.Audio.PlaySoundEffect(_uiSoundEffect);

        // Make the pause panel invisible to resume the game.
        IsVisible = false;

        // Reload the game scene
        Core.ChangeScene(new GameScene());

        // Resume the game
        ServiceLocator.Get<Game1>().UnGameOver();
        ServiceLocator.Get<Game1>().Resume();
    }

    /// <summary>
    /// Handles the click event for the quit button.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void HandleQuitButtonClicked(object sender, EventArgs e)
    {
        // A UI interaction occurred, play the sound effect
        Core.Audio.PlaySoundEffect(_uiSoundEffect);

        // Go back to the title scene.
        Core.ChangeScene(new TitleScene());

        ServiceLocator.Get<Game1>().UnGameOver();
    }

    /// <summary>
    /// Shows the pause panel.
    /// </summary>
    public void Show()
    {
        IsVisible = true; // Make the panel visible
    }

    /// <summary>
    /// Hides the pause panel.
    /// </summary>
    public void Hide()
    {
        IsVisible = false; // Hide the panel
    }
}