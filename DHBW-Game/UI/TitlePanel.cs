using System;
using GameLibrary;
using Gum.Forms.Controls;
using GameLibrary.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace DHBW_Game.UI;

/// <summary>
/// Represents a UI panel for the game's title screen, containing buttons for starting the game and accessing options.
/// </summary>
public class TitlePanel : Panel
{
    /// <summary>
    /// Gets the button for starting the game.
    /// </summary>
    public AnimatedButton StartButton { get; private set; }

    /// <summary>
    /// Gets the button for accessing the options menu.
    /// </summary>
    public AnimatedButton OptionsButton { get; private set; }

    // Sound effect for UI interactions
    private readonly SoundEffect _uiSoundEffect;
    
    // Callback for the start button action
    private readonly Action onStart;
    
    // Callback for the options button action
    private readonly Action onOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="TitlePanel"/> class.
    /// </summary>
    /// <param name="atlas">The texture atlas for UI elements.</param>
    /// <param name="uiSoundEffect">The sound effect played on UI interactions.</param>
    /// <param name="onStart">The action to invoke when the start button is clicked.</param>
    /// <param name="onOptions">The action to invoke when the options button is clicked.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="atlas"/> or <paramref name="uiSoundEffect"/> is null.</exception>
    public TitlePanel(TextureAtlas atlas, SoundEffect uiSoundEffect, Action onStart, Action onOptions)
    {
        // Validate input parameters
        if (atlas == null) throw new ArgumentNullException(nameof(atlas));
        if (uiSoundEffect == null) throw new ArgumentNullException(nameof(uiSoundEffect));

        _uiSoundEffect = uiSoundEffect;
        this.onStart = onStart; // Assign readonly field for start action
        this.onOptions = onOptions; // Assign readonly field for options action
        Dock(Gum.Wireframe.Dock.Fill); // Set panel to fill the parent container
        IsVisible = true; // Default to visible for title screen

        // Initialize child UI elements
        InitializeChildren(atlas);
    }

    /// <summary>
    /// Initializes the child UI elements, including the start and options buttons.
    /// </summary>
    /// <param name="atlas">The texture atlas for UI elements.</param>
    private void InitializeChildren(TextureAtlas atlas)
    {
        // Start button
        StartButton = new AnimatedButton(atlas);
        StartButton.Anchor(Gum.Wireframe.Anchor.BottomLeft); // Anchor to bottom-left
        StartButton.Visual.X = 50; // Horizontal position offset
        StartButton.Visual.Y = -12; // Vertical position offset
        StartButton.Text = "Start"; // Button text
        StartButton.Click += HandleStartClicked; // Subscribe to click event
        AddChild(StartButton); // Add to panel

        // Options button
        OptionsButton = new AnimatedButton(atlas);
        OptionsButton.Anchor(Gum.Wireframe.Anchor.BottomRight); // Anchor to bottom-right
        OptionsButton.Visual.X = -50; // Horizontal position offset
        OptionsButton.Visual.Y = -12; // Vertical position offset
        OptionsButton.Text = "Options"; // Button text
        OptionsButton.Click += HandleOptionsClicked; // Subscribe to click event
        AddChild(OptionsButton); // Add to panel

        StartButton.IsFocused = true; // Set initial focus to start button
    }

    /// <summary>
    /// Handles the start button click, triggering the start action and playing a sound.
    /// </summary>
    /// <param name="sender">The button that triggered the event.</param>
    /// <param name="e">Event arguments.</param>
    private void HandleStartClicked(object sender, EventArgs e)
    {
        // Play the UI sound effect for interaction
        Core.Audio.PlaySoundEffect(_uiSoundEffect);
        // Invoke the provided start action
        onStart?.Invoke();
    }

    /// <summary>
    /// Handles the options button click, triggering the options action and playing a sound.
    /// </summary>
    /// <param name="sender">The button that triggered the event.</param>
    /// <param name="e">Event arguments.</param>
    private void HandleOptionsClicked(object sender, EventArgs e)
    {
        // Play the UI sound effect for interaction
        Core.Audio.PlaySoundEffect(_uiSoundEffect);
        // Invoke the provided options action
        onOptions?.Invoke();
    }

    /// <summary>
    /// Shows the title panel and sets focus to the start button.
    /// </summary>
    public void Show()
    {
        IsVisible = true; // Make the panel visible
        StartButton.IsFocused = true; // Set focus to the start button
    }

    /// <summary>
    /// Hides the title panel.
    /// </summary>
    public void Hide()
    {
        IsVisible = false; // Hide the panel
    }
}