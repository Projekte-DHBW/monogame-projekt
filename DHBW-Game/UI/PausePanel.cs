using GameLibrary.Graphics;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework.Audio;
using System;
using DHBW_Game;
using DHBW_Game.UI;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using GameLibrary;
using GameLibrary.Input;
using GameLibrary.Scenes;
using MonoGameTutorial.UI;

namespace DHBW_Game.UI;

/// <summary>
/// Represents a UI panel which is shown when the game is paused.
/// </summary>
public class PausePanel : Panel
{
    private AnimatedButton _resumeButton;

    // The UI sound effect to play when a UI event is triggered.
    private SoundEffect _uiSoundEffect;

    // Reference to the texture atlas that we can pass to UI elements when they
    // are created.
    private TextureAtlas _atlas;


    public AnimatedButton OptionsButton { get; private set; }

    private readonly Action onOptions;
    
    
    public PausePanel(TextureAtlas atlas, SoundEffect uiSoundEffect, Action onOptions)
    {
        // Validate input parameters
        if (atlas == null) throw new ArgumentNullException(nameof(atlas));
        if (uiSoundEffect == null) throw new ArgumentNullException(nameof(uiSoundEffect));

        _uiSoundEffect = uiSoundEffect;
        this.onOptions = onOptions;
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
        textInstance.Text = "PAUSED";
        textInstance.CustomFontFile = @"fonts/04b_30.fnt";
        textInstance.UseCustomFont = true;
        textInstance.FontScale = 0.5f;
        textInstance.X = 10f;
        textInstance.Y = 10f;
        AddChild(textInstance);

        _resumeButton = new AnimatedButton(_atlas);
        _resumeButton.Text = "RESUME";
        _resumeButton.Anchor(Gum.Wireframe.Anchor.BottomLeft);
        _resumeButton.Visual.X = 9f;
        _resumeButton.Visual.Y = -9f;
        _resumeButton.Click += HandleResumeButtonClicked;
        AddChild(_resumeButton);

        AnimatedButton quitButton = new AnimatedButton(_atlas);
        quitButton.Text = "QUIT";
        quitButton.Anchor(Gum.Wireframe.Anchor.BottomRight);
        quitButton.Visual.X = -9f;
        quitButton.Visual.Y = -9f;
        quitButton.Click += HandleQuitButtonClicked;
        AddChild(quitButton);

        // Options button
        OptionsButton = new AnimatedButton(atlas);
        OptionsButton.Anchor(Gum.Wireframe.Anchor.TopRight); // Anchor to bottom-right
        OptionsButton.Visual.X = 0; // Horizontal position offset
        OptionsButton.Visual.Y = 5; // Vertical position offset
        OptionsButton.Text = "Options"; // Button text
        OptionsButton.Click += HandleOptionsClicked; // Subscribe to click event
        AddChild(OptionsButton); // Add to panel

    }
    private void HandleOptionsClicked(object sender, EventArgs e)
    {
        // Play the UI sound effect for interaction
        Core.Audio.PlaySoundEffect(_uiSoundEffect);
        // Invoke the provided options action
        onOptions?.Invoke();
    }
    private void HandleResumeButtonClicked(object sender, EventArgs e)
    {
        // A UI interaction occurred, play the sound effect
        Core.Audio.PlaySoundEffect(_uiSoundEffect);

        // Make the pause panel invisible to resume the game.
        IsVisible = false;
        ServiceLocator.Get<Game1>().Resume();
        
    }
    private void HandleQuitButtonClicked(object sender, EventArgs e)
    {
        // A UI interaction occurred, play the sound effect
        Core.Audio.PlaySoundEffect(_uiSoundEffect);

        // Go back to the title scene.
        Core.ChangeScene(new Scenes.TitleScene());
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