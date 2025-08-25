using System;
using GameLibrary;
using GameLibrary.Graphics;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework.Audio;
using MonoGameGum.GueDeriving;

namespace DHBW_Game.UI
{
    /// <summary>
    /// Represents a UI panel for adjusting game options such as music and sound effect volumes.
    /// </summary>
    public class OptionsPanel : Panel
    {
        /// <summary>
        /// Gets the back button for navigation and focus control.
        /// </summary>
        public AnimatedButton BackButton { get; private set; }

        /// <summary>
        /// Gets the slider for adjusting music volume.
        /// </summary>
        public OptionsSlider MusicSlider { get; private set; }

        /// <summary>
        /// Gets the slider for adjusting sound effect volume.
        /// </summary>
        public OptionsSlider SfxSlider { get; private set; }

        /// <summary>
        /// Gets the button for navigating to the question system panel.
        /// </summary>
        public AnimatedButton QuestionSystemButton { get; private set; }

        // Constants for slider behavior
        private const float SliderSmallChange = 0.1f; // Small increment for slider adjustments
        private const float SliderLargeChange = 0.2f; // Large increment for slider adjustments
        private const string OptionsFont = @"fonts/04b_30.fnt"; // Font used for text in the panel

        private readonly SoundEffect _uiSoundEffect; // Sound effect for UI interactions
        private readonly Action _onBack; // Callback for back button action
        private readonly Action _onQuestionSystem; // Callback for question system button action

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsPanel"/> class.
        /// </summary>
        /// <param name="atlas">The texture atlas for UI elements.</param>
        /// <param name="uiSoundEffect">The sound effect played on UI interactions.</param>
        /// <param name="onBack">The action to invoke when the back button is clicked.</param>
        /// <param name="onQuestionSystem">The action to invoke when the question system button is clicked.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="atlas"/> or <paramref name="uiSoundEffect"/> is null.</exception>
        public OptionsPanel(TextureAtlas atlas, SoundEffect uiSoundEffect, Action onBack, Action onQuestionSystem)
        {
            // Validate input parameters
            if (atlas == null) throw new ArgumentNullException(nameof(atlas));
            if (uiSoundEffect == null) throw new ArgumentNullException(nameof(uiSoundEffect));

            _uiSoundEffect = uiSoundEffect;
            
            // Store callbacks for use in handlers
            _onBack = onBack;
            _onQuestionSystem = onQuestionSystem;
            Dock(Gum.Wireframe.Dock.Fill); // Set panel to fill the parent container
            IsVisible = false; // Initially hide the panel

            // Initialize child UI elements
            InitializeChildren(atlas);
        }

        /// <summary>
        /// Initializes the child UI elements, including the title text, sliders, and buttons.
        /// </summary>
        /// <param name="atlas">The texture atlas for UI elements.</param>
        private void InitializeChildren(TextureAtlas atlas)
        {
            // Options title text
            TextRuntime optionsText = new TextRuntime();
            optionsText.X = 10; // Position X coordinate
            optionsText.Y = 150; // Position Y coordinate
            optionsText.Text = "OPTIONS"; // Title text
            optionsText.UseCustomFont = true; // Enable custom font
            optionsText.FontScale = 0.5f; // Font scaling
            optionsText.CustomFontFile = OptionsFont; // Set custom font file
            AddChild(optionsText); // Add to panel

            // Layout-Parameter
            const float sliderBaseY   = 55f; // Start point first slider
            const float sliderSpacing = 42f; // Distance between Music and SFX slider
            
            // Music slider
            MusicSlider = new OptionsSlider(atlas);
            MusicSlider.Name = "MusicSlider"; // Unique identifier
            MusicSlider.Text = "MUSIC"; // Label text
            MusicSlider.Anchor(Gum.Wireframe.Anchor.Top); // Anchor to top
            MusicSlider.Visual.Y = sliderBaseY; // Vertical position
            MusicSlider.Minimum = 0; // Minimum slider value
            MusicSlider.Maximum = 1; // Maximum slider value
            MusicSlider.Value = Core.Audio.SongVolume; // Initial value from audio settings
            MusicSlider.SmallChange = SliderSmallChange; // Small increment for fine adjustments
            MusicSlider.LargeChange = SliderLargeChange; // Large increment for coarse adjustments
            MusicSlider.ValueChanged += HandleMusicSliderValueChanged; // Subscribe to value change event
            MusicSlider.ValueChangeCompleted += HandleMusicSliderValueChangeCompleted; // Subscribe to value change completion
            AddChild(MusicSlider); // Add to panel

            // SFX slider
            SfxSlider = new OptionsSlider(atlas);
            SfxSlider.Name = "SfxSlider"; // Unique identifier
            SfxSlider.Text = "SFX"; // Label text
            SfxSlider.Anchor(Gum.Wireframe.Anchor.Top); // Anchor to top
            SfxSlider.Visual.Y =  sliderBaseY + sliderSpacing;; // Vertical position
            SfxSlider.Minimum = 0; // Minimum slider value
            SfxSlider.Maximum = 1; // Maximum slider value
            SfxSlider.Value = Core.Audio.SoundEffectVolume; // Initial value from audio settings
            SfxSlider.SmallChange = SliderSmallChange; // Small increment for fine adjustments
            SfxSlider.LargeChange = SliderLargeChange; // Large increment for coarse adjustments
            SfxSlider.ValueChanged += HandleSfxSliderChanged; // Subscribe to value change event
            SfxSlider.ValueChangeCompleted += HandleSfxSliderChangeCompleted; // Subscribe to value change completion
            AddChild(SfxSlider); // Add to panel

            // Question System button
            QuestionSystemButton = new AnimatedButton(atlas);
            QuestionSystemButton.Text = "QUESTION SYSTEM";
            QuestionSystemButton.Anchor(Gum.Wireframe.Anchor.Top);
            QuestionSystemButton.Visual.Y = 156f; // Below SFX slider (spacing consistent with ~63 units)
            QuestionSystemButton.Click += HandleQuestionSystemButtonClick; // Subscribe to click event
            AddChild(QuestionSystemButton);

            // Back button
            BackButton = new AnimatedButton(atlas);
            BackButton.Text = "BACK"; // Button text
            BackButton.Anchor(Gum.Wireframe.Anchor.BottomRight); // Anchor to bottom-right
            BackButton.X = -28f; // Horizontal position offset
            BackButton.Y = -10f; // Vertical position offset
            BackButton.Click += HandleOptionsButtonBack; // Subscribe to click event
            AddChild(BackButton); // Add to panel
        }

        /// <summary>
        /// Handles the question system button click, triggering the question system action and playing a sound.
        /// </summary>
        /// <param name="sender">The button that triggered the event.</param>
        /// <param name="args">Event arguments.</param>
        private void HandleQuestionSystemButtonClick(object sender, EventArgs args)
        {
            // A UI interaction occurred, play the sound effect
            Core.Audio.PlaySoundEffect(_uiSoundEffect);
            // Invoke the provided question system action
            _onQuestionSystem?.Invoke();
        }

        /// <summary>
        /// Handles changes to the sound effect slider value, updating the audio system.
        /// </summary>
        /// <param name="sender">The slider that triggered the event.</param>
        /// <param name="args">Event arguments.</param>
        private void HandleSfxSliderChanged(object sender, EventArgs args)
        {
            // Intentionally not playing the UI sound effect here to avoid constant triggering during adjustment
            var slider = (Slider)sender;
            Core.Audio.SoundEffectVolume = (float)slider.Value; // Update sound effect volume
        }

        /// <summary>
        /// Handles the completion of sound effect slider value changes, playing a test sound.
        /// </summary>
        /// <param name="sender">The slider that triggered the event.</param>
        /// <param name="args">Event arguments.</param>
        private void HandleSfxSliderChangeCompleted(object sender, EventArgs args)
        {
            // Play the UI sound effect to demonstrate the new volume
            Core.Audio.PlaySoundEffect(_uiSoundEffect);
        }

        /// <summary>
        /// Handles changes to the music slider value, updating the audio system.
        /// </summary>
        /// <param name="sender">The slider that triggered the event.</param>
        /// <param name="args">Event arguments.</param>
        private void HandleMusicSliderValueChanged(object sender, EventArgs args)
        {
            // Intentionally not playing the UI sound effect here to avoid constant triggering during adjustment
            var slider = (Slider)sender;
            Core.Audio.SongVolume = (float)slider.Value; // Update music volume
        }

        /// <summary>
        /// Handles the completion of music slider value changes, playing a test sound.
        /// </summary>
        /// <param name="sender">The slider that triggered the event.</param>
        /// <param name="args">Event arguments.</param>
        private void HandleMusicSliderValueChangeCompleted(object sender, EventArgs args)
        {
            // A UI interaction occurred, play the sound effect
            Core.Audio.PlaySoundEffect(_uiSoundEffect);
        }

        /// <summary>
        /// Handles the back button click, triggering the back action and playing a sound.
        /// </summary>
        /// <param name="sender">The button that triggered the event.</param>
        /// <param name="args">Event arguments.</param>
        private void HandleOptionsButtonBack(object sender, EventArgs args)
        {
            // A UI interaction occurred, play the sound effect
            Core.Audio.PlaySoundEffect(_uiSoundEffect);
            // Invoke the provided back action (e.g., show title or pause panel)
            _onBack?.Invoke();
        }

        /// <summary>
        /// Shows the options panel and sets focus to the back button.
        /// </summary>
        public void Show()
        {
            IsVisible = true; // Make the panel visible
            BackButton.IsFocused = true; // Set focus to the back button
        }

        /// <summary>
        /// Hides the options panel.
        /// </summary>
        public void Hide()
        {
            IsVisible = false; // Hide the panel
        }
    }
}