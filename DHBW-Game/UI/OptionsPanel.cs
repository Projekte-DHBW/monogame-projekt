using System;
using System.Collections.Generic;
using DHBW_Game.Question_System;
using DHBW_Game.Save_System;
using GameLibrary;
using GameLibrary.Graphics;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
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

        /// <summary>
        /// Gets the button for resetting the game progress.
        /// </summary>
        public AnimatedButton ResetGameProgressButton { get; private set; }

        // Constants for slider behavior
        private const float SliderSmallChange = 0.1f; // Small increment for slider adjustments
        private const float SliderLargeChange = 0.2f; // Large increment for slider adjustments
        private const string OptionsFont = @"fonts/04b_30.fnt"; // Font used for text in the panel

        private readonly SoundEffect _uiSoundEffect; // Sound effect for UI interactions
        private readonly Action _onBack; // Callback for back button action
        private readonly Action _onQuestionSystem; // Callback for question system button action

        private ContainerRuntime _audioContainer;
        private ContainerRuntime _deeperSettingsContainer;
        private AnimatedButton _leftArrow;
        private AnimatedButton _rightArrow;
        private List<ContainerRuntime> _pages;
        private int _currentPage = 0;
        private readonly bool _includeDeepSettings;
        private float _resetFeedbackTimer = 0f;
        private const string ResetButtonText = "Reset Progress";

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsPanel"/> class.
        /// </summary>
        /// <param name="atlas">The texture atlas for UI elements.</param>
        /// <param name="uiSoundEffect">The sound effect played on UI interactions.</param>
        /// <param name="onBack">The action to invoke when the back button is clicked.</param>
        /// <param name="onQuestionSystem">The action to invoke when the question system button is clicked.</param>
        /// <param name="includeDeepSettings">Whether to include deeper settings such as for the question system or the save system.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="atlas"/> or <paramref name="uiSoundEffect"/> is null.</exception>
        public OptionsPanel(TextureAtlas atlas, SoundEffect uiSoundEffect, Action onBack, Action onQuestionSystem, bool includeDeepSettings = true)
        {
            // Validate input parameters
            if (atlas == null) throw new ArgumentNullException(nameof(atlas));
            if (uiSoundEffect == null) throw new ArgumentNullException(nameof(uiSoundEffect));

            _uiSoundEffect = uiSoundEffect;

            // Store callbacks for use in handlers
            _onBack = onBack;
            _onQuestionSystem = onQuestionSystem;
            _includeDeepSettings = includeDeepSettings;
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
            // Create containers for pages
            _audioContainer = new ContainerRuntime();
            _audioContainer.Dock(Gum.Wireframe.Dock.Fill);
            AddChild(_audioContainer);

            if (_includeDeepSettings)
            {
                _deeperSettingsContainer = new ContainerRuntime();
                _deeperSettingsContainer.Dock(Gum.Wireframe.Dock.Fill);
                AddChild(_deeperSettingsContainer);
            }

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
            _audioContainer.AddChild(MusicSlider.Visual); // Add to audio page

            // SFX slider
            SfxSlider = new OptionsSlider(atlas);
            SfxSlider.Name = "SfxSlider"; // Unique identifier
            SfxSlider.Text = "SFX"; // Label text
            SfxSlider.Anchor(Gum.Wireframe.Anchor.Top); // Anchor to top
            SfxSlider.Visual.Y =  sliderBaseY + sliderSpacing; // Vertical position
            SfxSlider.Minimum = 0; // Minimum slider value
            SfxSlider.Maximum = 1; // Maximum slider value
            SfxSlider.Value = Core.Audio.SoundEffectVolume; // Initial value from audio settings
            SfxSlider.SmallChange = SliderSmallChange; // Small increment for fine adjustments
            SfxSlider.LargeChange = SliderLargeChange; // Large increment for coarse adjustments
            SfxSlider.ValueChanged += HandleSfxSliderChanged; // Subscribe to value change event
            SfxSlider.ValueChangeCompleted += HandleSfxSliderChangeCompleted; // Subscribe to value change completion
            _audioContainer.AddChild(SfxSlider.Visual); // Add to audio page

            if (_includeDeepSettings)
            {
                // Question System button
                QuestionSystemButton = new AnimatedButton(atlas);
                QuestionSystemButton.Text = "QUESTION SYSTEM";
                QuestionSystemButton.Anchor(Gum.Wireframe.Anchor.Top);
                QuestionSystemButton.Visual.Y = sliderBaseY; // Vertical position (similar to audio sliders)
                QuestionSystemButton.Height = 40f;
                QuestionSystemButton.Visual.WidthUnits = DimensionUnitType.Absolute;
                QuestionSystemButton.Width = 200f;
                QuestionSystemButton.Click += HandleQuestionSystemButtonClick; // Subscribe to click event
                _deeperSettingsContainer.AddChild(QuestionSystemButton.Visual);

                // Reset game progress button
                ResetGameProgressButton = new AnimatedButton(atlas);
                ResetGameProgressButton.Text = ResetButtonText;
                ResetGameProgressButton.Anchor(Gum.Wireframe.Anchor.Top);
                ResetGameProgressButton.Visual.Y = sliderBaseY + sliderSpacing; // Vertical position (similar to audio sliders)
                ResetGameProgressButton.Height = 40f;
                ResetGameProgressButton.Visual.WidthUnits = DimensionUnitType.Absolute;
                ResetGameProgressButton.Width = 200f;
                ResetGameProgressButton.Click += HandleResetGameProgressButtonClick; // Subscribe to click event
                _deeperSettingsContainer.AddChild(ResetGameProgressButton.Visual);
            }

            // Navigation arrows
            _leftArrow = new AnimatedButton(atlas);
            _leftArrow.Text = "<";
            _leftArrow.Anchor(Gum.Wireframe.Anchor.Left);
            _leftArrow.Visual.X = 20;
            _leftArrow.Height = 30;
            _leftArrow.Click += HandleLeftArrowClick;
            AddChild(_leftArrow);

            _rightArrow = new AnimatedButton(atlas);
            _rightArrow.Text = ">";
            _rightArrow.Anchor(Gum.Wireframe.Anchor.Right);
            _rightArrow.Visual.X = -20;
            _rightArrow.Height = 30;
            _rightArrow.Click += HandleRightArrowClick;
            AddChild(_rightArrow);

            // Pages list
            _pages = new List<ContainerRuntime> { _audioContainer };
            if (_includeDeepSettings)
            {
                _pages.Add(_deeperSettingsContainer);
            }

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
        /// Handles the left arrow button click, playing a sound effect and navigating to the previous page if available.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="args">The event arguments.</param>
        private void HandleLeftArrowClick(object sender, EventArgs args)
        {
            Core.Audio.PlaySoundEffect(_uiSoundEffect);
            if (_currentPage > 0)
            {
                _currentPage--;
                UpdatePageVisibility();
            }
        }

        /// <summary>
        /// Handles the right arrow button click, playing a sound effect and navigating to the next page if available.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="args">The event arguments.</param>
        private void HandleRightArrowClick(object sender, EventArgs args)
        {
            Core.Audio.PlaySoundEffect(_uiSoundEffect);
            if (_currentPage < _pages.Count - 1)
            {
                _currentPage++;
                UpdatePageVisibility();
            }
        }

        /// <summary>
        /// Updates the visibility of the pages and navigation arrows based on the current page index, and sets focus to the first control on the active page.
        /// </summary>
        private void UpdatePageVisibility()
        {
            for (int i = 0; i < _pages.Count; i++)
            {
                _pages[i].Visible = (i == _currentPage);
            }

            _leftArrow.IsVisible = _pages.Count > 1 && _currentPage > 0;
            _rightArrow.IsVisible = _pages.Count > 1 && _currentPage < _pages.Count - 1;

            // Set focus to first control on the page
            if (_currentPage == 0)
            {
                MusicSlider.IsFocused = true;
            }
            else if (_includeDeepSettings && _currentPage == 1)
            {
                QuestionSystemButton.IsFocused = true;
            }
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
        /// Handles the reset game progress button click by resetting the progress and playing a sound.
        /// </summary>
        /// <param name="sender">The button that triggered the event.</param>
        /// <param name="args">Event arguments.</param>
        private void HandleResetGameProgressButtonClick(object sender, EventArgs args)
        {
            // A UI interaction occurred, play the sound effect
            Core.Audio.PlaySoundEffect(_uiSoundEffect);

            try
            {
                SaveManager.ResetProgress();
                ServiceLocator.Get<QuestionPool>().ResetAnswered();
                ResetGameProgressButton.Text = "Game Progress Deleted!"; // Temporary feedback text
                ResetGameProgressButton.IsEnabled = false; // Disable to prevent spam clicks
            }
            catch (Exception ex)
            {
                ResetGameProgressButton.Text = "Reset Failed!"; // Error feedback
                Console.WriteLine(ex.Message); // Log for debugging
            }

            // Start a timer to clear the message
            _resetFeedbackTimer = 2f; // 2 seconds
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

        public void Update(GameTime gameTime)
        {
            if (_resetFeedbackTimer > 0f)
            {
                _resetFeedbackTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (_resetFeedbackTimer <= 0f)
                {
                    ResetGameProgressButton.Text = ResetButtonText; // Revert to original
                    ResetGameProgressButton.IsEnabled = true; // Re-enable
                }
            }
        }

        /// <summary>
        /// Shows the options panel and sets focus to the back button.
        /// </summary>
        public void Show()
        {
            IsVisible = true; // Make the panel visible
            _currentPage = 0;
            UpdatePageVisibility();
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