using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;

namespace DHBW_Game.Question_System;

/// <summary>
/// Manages a pool of multiple-choice questions, including loading, generating, and tracking answered questions.
/// </summary>
public class QuestionPool
{
    // Flat list of all questions in stable order (for global indexing/audio)
    private List<MultipleChoiceQuestion> _allQuestions = new List<MultipleChoiceQuestion>();

    // Dictionary of questions grouped by lecturer ID
    private Dictionary<string, List<MultipleChoiceQuestion>> _questionsByLecturer = new Dictionary<string, List<MultipleChoiceQuestion>>();

    // Dictionary of answered indices grouped by lecturer ID (local indices)
    private Dictionary<string, HashSet<int>> _answeredByLecturer = new Dictionary<string, HashSet<int>>();

    // File path for the XML file storing questions
    private readonly string _filePath;

    // Serializer for handling XML operations
    private readonly QuestionXmlSerializer _xmlSerializer;

    // Generator for creating new questions via API
    private QuestionGenerator _questionGenerator;

    // Random number generator for selecting a random question from the pool
    private readonly Random _randomNumberGenerator;

    // List to track running Python processes
    private readonly List<Process> _runningProcesses = new List<Process>();

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestionPool"/> class, loading questions from the specified or default XML file.
    /// </summary>
    /// <param name="filePath">Optional path to the XML file. Defaults to user AppData directory.</param>
    /// <exception cref="InvalidOperationException">Thrown if the API key is not found.</exception>
    public QuestionPool(string filePath = null)
    {
        // Set the file path, defaulting to AppData directory if not provided
        _filePath = filePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DHBW-Game", "questions.xml");

        // Initialize the XML serializer
        _xmlSerializer = new QuestionXmlSerializer(_filePath);

        // Load the API key from secure storage
        var apiKey = SecureApiKeyStorage.LoadApiKey();

        if (!string.IsNullOrEmpty(apiKey))
        {
            // Initialize the question generator with the API key
            _questionGenerator = new QuestionGenerator(apiKey);
        }

        // Initialize the random number generator
        _randomNumberGenerator = new Random();

        // Load questions from the XML file
        LoadQuestions();
    }

    /// <summary>
    /// Loads questions from the XML file into the pool, grouped by lecturer.
    /// </summary>
    private void LoadQuestions()
    {
        // Load flat list of questions using the XML serializer
        _allQuestions = _xmlSerializer.LoadFromFile();

        // Group questions by LecturerID
        _questionsByLecturer.Clear();
        foreach (var question in _allQuestions)
        {
            if (string.IsNullOrEmpty(question.LecturerID))
            {
                question.LecturerID = "berninger"; // Fallback if LecturerID is missing
            }
            if (!_questionsByLecturer.TryGetValue(question.LecturerID, out var list))
            {
                list = new List<MultipleChoiceQuestion>();
                _questionsByLecturer[question.LecturerID] = list;
            }
            list.Add(question);
        }

        // Clear answered indices to reset tracking
        _answeredByLecturer.Clear();
    }

    /// <summary>
    /// Rebuilds the flat list of all questions in a stable order (sorted by lecturer ID).
    /// </summary>
    private void RebuildAllQuestions()
    {
        _allQuestions = _questionsByLecturer
            .OrderBy(kvp => kvp.Key)
            .SelectMany(kvp => kvp.Value)
            .ToList();
    }

    /// <summary>
    /// Generates new questions using the API and adds them to the pool, saving to file. Deletes existing audio files.
    /// </summary>
    /// <param name="numberOfQuestions">The number of questions to generate.</param>
    /// <param name="keepExisting">Whether to keep existing questions or replace them.</param>
    /// <param name="updateStatus">Callback to update UI status.</param>
    /// <returns>A task that completes when the generation and saving are done.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the API key is missing.</exception>
    /// <exception cref="Exception">Thrown if the API fails to generate questions or returns no questions.</exception>
    public async Task GenerateNewQuestions(int numberOfQuestions, bool keepExisting, Action<string> updateStatus = null)
    {
        if (_questionGenerator == null)
        {
            var errorMessage = "API key not set. Please provide a valid API key to generate questions.";
            updateStatus?.Invoke($"Error: {errorMessage}");
        }

        updateStatus?.Invoke("Generating questions...");

        // Define audio directory
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var audioDir = Path.Combine(appData, "DHBW-Game", "audio", "questions");

        // Clear existing audio files to avoid mismatches (if no new audio files are generated, the game would think that the old audio files belong to the new questions)
        try
        {
            ClearExistingAudioFiles(audioDir, null); // Currently the UI update status is not needed
        }
        catch (Exception ex)
        {
            updateStatus?.Invoke($"Warning: Failed to clear old audio files: {ex.Message}");
            Console.WriteLine($"Failed to clear old audio files: {ex.Message}");
        }

        // Generate and parse questions via the API
        var newQuestions = await _questionGenerator.GenerateQuestions(numberOfQuestions);

        // Check if any questions were returned
        if (newQuestions.Count == 0)
        {
            var errorMessage = "Failed to generate questions: Invalid or empty response from API.";
            updateStatus?.Invoke($"Error: {errorMessage}");
        }

        // Clear existing questions if not keeping them
        if (!keepExisting)
        {
            _questionsByLecturer.Clear();
            _allQuestions.Clear();
        }

        // Add new questions to the grouped dictionary
        foreach (var q in newQuestions)
        {
            if (string.IsNullOrEmpty(q.LecturerID))
            {
                q.LecturerID = "berninger";
            }
            if (!_questionsByLecturer.TryGetValue(q.LecturerID, out var list))
            {
                list = new List<MultipleChoiceQuestion>();
                _questionsByLecturer[q.LecturerID] = list;
            }
            list.Add(q);
        }

        // Rebuild the flat list in stable order
        RebuildAllQuestions();

        // Save the updated question list to the XML file
        _xmlSerializer.SaveToFile(_allQuestions);

        // Send final success message via callback
        updateStatus?.Invoke("Questions generated!");
    }

    /// <summary>
    /// Ensures the TTS venv is set up and generates audio for questions using lecturer voice samples.
    /// </summary>
    /// <param name="voicePromptPath">Path to a single lecturer voice sample WAV file. If null, uses per-lecturer voices from voices_dir.</param>
    /// <param name="updateStatus">Callback to update UI status.</param>
    /// <returns>A task that completes with true if the entire process (setup + generation) succeeded; false otherwise.</returns>
    private async Task<bool> EnsureTTSSetupAndGenerateAudio(string voicePromptPath = null, Action<string> updateStatus = null)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var venvPath = Path.Combine(appData, "DHBW-Game", "tts_venv");
        var audioDir = Path.Combine(appData, "DHBW-Game", "audio", "questions");

        // Project root is the directory of the executing assembly (build output), where TTS is copied during build
        var projectRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        // Verify TTS exists (optional, for robustness in case of build issues)
        if (!Directory.Exists(Path.Combine(projectRoot, "TTS")) || !File.Exists(Path.Combine(projectRoot, "TTS", "tts_controller.py")))
        {
            // Log error or throw—e.g., updateStatus?.Invoke("Error: TTS folder not found in build output. Check csproj and rebuild.");
            throw new DirectoryNotFoundException("TTS folder missing in build output.");
        }

        Console.WriteLine($"Found project root at: {projectRoot}"); // For debugging

        var scriptPath = Path.Combine(projectRoot, "TTS", "setup_tts.ps1");
        var voicesDir = Path.Combine(projectRoot, "TTS", "voices");

        // Check if questions.xml exists
        if (!File.Exists(_filePath))
        {
            updateStatus?.Invoke($"Error: Questions XML not found at {_filePath}");
            Console.WriteLine($"Questions XML not found at {_filePath}. Skipping TTS generation.");
            return false;
        }

        // Count expected number of questions from XML
        int expectedAudioFiles = 0;
        try
        {
            var xmlDoc = XDocument.Load(_filePath);
            expectedAudioFiles = xmlDoc.Element("Questions")?.Elements("Question").Count() ?? 0;
        }
        catch (Exception ex)
        {
            updateStatus?.Invoke($"Error: Failed to parse questions.xml: {ex.Message}");
            Console.WriteLine($"Failed to parse questions.xml: {ex.Message}");
            return false;
        }

        if (expectedAudioFiles == 0)
        {
            updateStatus?.Invoke("No questions to generate audio for.");
            return false;
        }

        // Set up venv if needed
        if (!await SetupTTSVenvIfNeeded(venvPath, scriptPath, updateStatus))
        {
            Console.WriteLine("TTS setup failed. Skipping audio generation.");
            return false; // Explicit early return on setup failure
        }

        // Clear existing audio files
        try
        {
            ClearExistingAudioFiles(audioDir, updateStatus);
        }
        catch (Exception ex)
        {
            updateStatus?.Invoke($"Warning: Failed to clear old audio files: {ex.Message}");
            Console.WriteLine($"Failed to clear old audio files: {ex.Message}");
        }

        // Generate audio
        bool audioGenerated = await GenerateAudioWithTTS(projectRoot, _filePath, audioDir, voicePromptPath, voicesDir, expectedAudioFiles, updateStatus);
        if (audioGenerated)
        {
            updateStatus?.Invoke("Audio generated!");
            return true;
        }
        else
        {
            updateStatus?.Invoke("Warning: Audio generation incomplete. Check Python window and logs.");
            return false;
        }
    }

    /// <summary>
    /// Sets up the TTS virtual environment if it doesn't exist or is invalid by running the setup PowerShell script.
    /// </summary>
    /// <param name="venvPath">Path to the venv directory.</param>
    /// <param name="setupScriptPath">Path to the setup_tts.ps1 script.</param>
    /// <param name="updateStatus">Callback to update UI status.</param>
    /// <returns>True if setup succeeds or venv is valid; false on failure or timeout.</returns>
    private async Task<bool> SetupTTSVenvIfNeeded(string venvPath, string setupScriptPath, Action<string> updateStatus = null)
    {
        var venvPython = Path.Combine(venvPath, "Scripts", "python.exe");
        var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DHBW-Game", "logs");
        var startedFlagPattern = "tts_setup_started.flag";
        var successFlagPattern = "tts_setup_success.flag";
        var errorFlagPattern = "tts_setup_error.flag";

        // Ensure logs dir exists
        Directory.CreateDirectory(logDir);

        // Clean up old flags to avoid stale state
        try
        {
            var existingFlags = Directory.GetFiles(logDir, "*.flag");
            foreach (var flag in existingFlags)
            {
                if (Path.GetFileName(flag).StartsWith("tts_setup_"))
                {
                    File.Delete(flag);
                }
            }
            Console.WriteLine("Cleared old TTS setup flags in logs folder.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to clean old flags: {ex.Message}");
        }

        // Validate existing venv
        bool isVenvValid = false;
        if (File.Exists(venvPython))
        {
            Console.WriteLine($"Found venv python at {venvPython}. Validating...");
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = venvPython,
                    Arguments = "-c \"import chatterbox.tts\"",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string errorOutput = process.StandardError.ReadToEnd();
            process.WaitForExit();
            isVenvValid = process.ExitCode == 0;
            if (!isVenvValid)
            {
                Console.WriteLine($"Venv invalid: {errorOutput}");
                updateStatus?.Invoke("Error: Existing virtual environment is invalid. Re-running setup.");
            }
        }

        // Run setup if needed
        if (!File.Exists(venvPython) || !isVenvValid)
        {
            var setupArgs = $"-ExecutionPolicy Bypass -File \"{setupScriptPath}\" -VenvPath \"{venvPath}\"";
            Console.WriteLine($"Running setup detached: cmd /c start powershell.exe {setupArgs}");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c start \"\" powershell.exe {setupArgs}",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Minimized
                }
            };
            process.Start();
            _runningProcesses.Add(process); // Track the process

            // Poll for venv creation
            updateStatus?.Invoke("Setting up TTS environment...");
            bool setupComplete = false;
            const int setupTimeoutMs = 1800 * 1000; // 30 minutes
            const int setupPollIntervalMs = 1000; // 1 second
            var startTime = DateTime.UtcNow;
            bool startedDetected = false;
            bool errorDetected = false;

            while ((DateTime.UtcNow - startTime).TotalMilliseconds < setupTimeoutMs && !errorDetected)
            {
                var elapsed = (int)((DateTime.UtcNow - startTime).TotalSeconds);
                updateStatus?.Invoke($"Setting up TTS environment ({elapsed}s elapsed)...");

                // Force directory refresh and check for started flag
                try
                {
                    var startedFiles = Directory.GetFiles(logDir, startedFlagPattern);
                    if (!startedDetected && startedFiles.Length > 0)
                    {
                        startedDetected = true;
                        Console.WriteLine("TTS setup started flag detected in logs folder. Script is running.");
                        updateStatus?.Invoke("TTS setup script launched. Monitoring progress...");
                    }

                    // Check for success flag
                    var successFiles = Directory.GetFiles(logDir, successFlagPattern);
                    if (successFiles.Length > 0)
                    {
                        setupComplete = true;
                        Console.WriteLine("TTS setup success flag detected in logs folder.");
                        updateStatus?.Invoke("TTS environment set up! Starting audio generation...");
                        // Clean up flags
                        File.Delete(successFiles[0]);
                        if (startedDetected) File.Delete(Directory.GetFiles(logDir, startedFlagPattern)[0]);
                        break;
                    }

                    // Check for error flag
                    var errorFiles = Directory.GetFiles(logDir, errorFlagPattern);
                    if (errorFiles.Length > 0)
                    {
                        errorDetected = true;
                        var errorMessage = File.ReadAllText(errorFiles[0]);
                        updateStatus?.Invoke($"Error: {errorMessage}");
                        Console.WriteLine($"TTS setup failed: {errorMessage}");

                        // Clean up flags
                        File.Delete(errorFiles[0]);
                        if (startedDetected) File.Delete(Directory.GetFiles(logDir, startedFlagPattern)[0]);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error enumerating flags in {logDir}: {ex.Message}");
                }

                // Warn if started but no resolution after 15 minutes
                if (startedDetected && (DateTime.UtcNow - startTime).TotalMinutes > 15 && !setupComplete)
                {
                    var warningMessage = "TTS setup may be hanging. Check the PowerShell window or %APPDATA%\\DHBW-Game\\logs\\setup_tts.log for progress.";
                    updateStatus?.Invoke($"Warning: {warningMessage}");
                    Console.WriteLine(warningMessage);
                }

                await Task.Delay(setupPollIntervalMs);
            }

            if (!setupComplete)
            {
                var errorMessage = "TTS setup timed out or failed. Check the PowerShell window or %APPDATA%\\DHBW-Game\\logs\\setup_tts.log for details.";
                updateStatus?.Invoke($"Error: {errorMessage}");
                Console.WriteLine(errorMessage);
                return false;
            }
        }
        else
        {
            Console.WriteLine("Existing venv is valid. Skipping setup.");
            updateStatus?.Invoke("TTS environment ready! Starting audio generation...");
        }

        return true;
    }

    /// <summary>
    /// Clears existing question audio files from the specified directory.
    /// </summary>
    /// <param name="audioDir">Path to the audio directory.</param>
    /// <param name="updateStatus">Callback to update UI status.</param>
    private void ClearExistingAudioFiles(string audioDir, Action<string> updateStatus = null)
    {
        if (Directory.Exists(audioDir))
        {
            foreach (var file in Directory.GetFiles(audioDir, "question_*.wav"))
            {
                File.Delete(file);
            }
            updateStatus?.Invoke("Cleared existing audio files.");
        }
    }

    /// <summary>
    /// Launches the TTS Python script to generate audio files and polls for completion asynchronously.
    /// </summary>
    /// <param name="projectRoot">Path to the project root.</param>
    /// <param name="xmlPath">Path to questions.xml.</param>
    /// <param name="outputDir">Output directory for audio files.</param>
    /// <param name="voicePromptPath">Path to single voice sample (null for per-lecturer).</param>
    /// <param name="voicesDir">Directory for per-lecturer voice samples.</param>
    /// <param name="expectedAudioFiles">Number of expected audio files.</param>
    /// <param name="updateStatus">Callback to update UI status.</param>
    /// <returns>A task that completes with true if all expected files are generated; false on timeout.</returns>
    private async Task<bool> GenerateAudioWithTTS(string projectRoot, string xmlPath, string outputDir, string voicePromptPath, string voicesDir, int expectedAudioFiles, Action<string> updateStatus = null)
    {
        var ttsScript = Path.Combine(projectRoot, "TTS", "tts_controller.py");
        Directory.CreateDirectory(outputDir);

        // Launch TTS audio generation in a separate window, detached
        string cmdArgs;
        if (!string.IsNullOrEmpty(voicePromptPath))
        {
            // Single voice mode
            if (!File.Exists(voicePromptPath))
            {
                updateStatus?.Invoke($"Error: Voice sample not found at {voicePromptPath}");
                Console.WriteLine($"Voice sample not found at {voicePromptPath}. Skipping TTS generation.");
                return false;
            }
            cmdArgs = $"\"{ttsScript}\" --xml \"{xmlPath}\" --output \"{outputDir}\" --voice \"{voicePromptPath}\"";
        }
        else
        {
            // Per-lecturer mode, let Python handle lecturer iteration
            cmdArgs = $"\"{ttsScript}\" --xml \"{xmlPath}\" --output \"{outputDir}\" --voices_dir \"{voicesDir}\"";
        }

        var venvPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DHBW-Game", "tts_venv");
        var venvPython = Path.Combine(venvPath, "Scripts", "python.exe");

        Console.WriteLine($"Running TTS detached: cmd /c start \"\" \"{venvPython}\" {cmdArgs}");
        var processTTS = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start \"\" \"{venvPython}\" {cmdArgs}",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Minimized
            }
        };
        processTTS.Start();
        _runningProcesses.Add(processTTS); // Track the process

        // Poll for audio files asynchronously
        updateStatus?.Invoke("Generating audio...");
        bool audioGenerated = false;
        const int audioTimeoutMs = 1800 * 1000; // 30 minutes
        const int audioPollIntervalMs = 2000; // Check every 2 seconds
        var audioStartTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - audioStartTime).TotalMilliseconds < audioTimeoutMs)
        {
            var audioFiles = Directory.GetFiles(outputDir, "question_*.wav");
            var generatedCount = audioFiles.Length;
            if (generatedCount >= expectedAudioFiles)
            {
                audioGenerated = true;
                break;
            }
            // Update progress
            updateStatus?.Invoke($"Generating audio ({generatedCount}/{expectedAudioFiles} files complete)...");

            await Task.Delay(audioPollIntervalMs); // Non-blocking await to yield control to the game loop
        }

        return audioGenerated;
    }

    /// <summary>
    /// Generates audio for questions.
    /// </summary>
    /// <param name="updateStatus">Callback to update UI status.</param>
    /// <returns>A task that completes with true if the entire process (setup + generation) succeeded; false otherwise.</returns>
    public async Task<bool> GenerateAudioAsync(Action<string> updateStatus = null)
    {
        // Set to null to use per-lecturer voices
        return await EnsureTTSSetupAndGenerateAudio(null, updateStatus);
    }

    /// <summary>
    /// Cancels all running Python processes (e.g., TTS setup and audio generation).
    /// </summary>
    public void CancelRunningProcesses()
    {
        foreach (var process in _runningProcesses.ToList()) // Create a copy to avoid modification issues
        {
            try
            {
                if (!process.HasExited)
                {
                    // Kill the process and its descendants (cmd.exe and python.exe)
                    process.Kill(true); // true ensures the entire process tree is terminated
                    Console.WriteLine($"Terminated process with ID {process.Id}");
                }
                _runningProcesses.Remove(process); // Remove from tracking
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to terminate process with ID {process.Id}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Refreshes the question generator by reloading the API key from storage.
    /// </summary>
    public void RefreshQuestionGenerator()
    {
        var apiKey = SecureApiKeyStorage.LoadApiKey();
        _questionGenerator = string.IsNullOrEmpty(apiKey) ? null : new QuestionGenerator(apiKey);
    }

    /// <summary>
    /// Retrieves a random unanswered question for the specified lecturer from the pool along with its local and global indices.
    /// The global index corresponds to the position in the XML/flat list, used for audio file naming.
    /// </summary>
    /// <param name="lecturerID">The ID of the lecturer to get a question for.</param>
    /// <returns>A tuple containing the question, its local index in the lecturer's list, and its global index in the flat list, or (null, -1, -1) if no unanswered questions remain for that lecturer.</returns>
    public (MultipleChoiceQuestion Question, int LocalIndex, int GlobalIndex) GetNextQuestion(string lecturerID)
    {
        if (string.IsNullOrEmpty(lecturerID) || !_questionsByLecturer.TryGetValue(lecturerID, out var questions))
        {
            return (null, -1, -1);
        }

        // Get answered set for this lecturer
        if (!_answeredByLecturer.TryGetValue(lecturerID, out var answered))
        {
            answered = new HashSet<int>();
            _answeredByLecturer[lecturerID] = answered;
        }

        // Get indices of unanswered questions (local)
        var availableIndices = Enumerable.Range(0, questions.Count)
            .Where(i => !answered.Contains(i))
            .ToList();

        // Return null if no unanswered questions are available
        if (availableIndices.Count == 0)
        {
            return (null, -1, -1);
        }

        // Select a random local index from available indices
        var localIndex = availableIndices[_randomNumberGenerator.Next(availableIndices.Count)];
        var q = questions[localIndex];

        // Find the global index in the flat list
        int globalIndex = _allQuestions.IndexOf(q);
        if (globalIndex == -1)
        {
            // Should not happen if lists are consistent
            Console.WriteLine($"Warning: Question not found in flat list for {lecturerID}.");
            return (null, -1, -1);
        }

        return (q, localIndex, globalIndex);
    }

    /// <summary>
    /// Marks a question as answered by its lecturer ID and local index.
    /// </summary>
    /// <param name="lecturerID">The ID of the lecturer.</param>
    /// <param name="localIndex">The local index of the question in the lecturer's list.</param>
    public void MarkAsAnswered(string lecturerID, int localIndex)
    {
        if (string.IsNullOrEmpty(lecturerID) || !_questionsByLecturer.TryGetValue(lecturerID, out var questions))
        {
            return;
        }

        if (localIndex >= 0 && localIndex < questions.Count)
        {
            if (!_answeredByLecturer.TryGetValue(lecturerID, out var answered))
            {
                answered = new HashSet<int>();
                _answeredByLecturer[lecturerID] = answered;
            }
            answered.Add(localIndex);
        }
    }

    /// <summary>
    /// Resets the answered status of all questions, making them all available again.
    /// </summary>
    public void ResetAnswered()
    {
        foreach (var answered in _answeredByLecturer.Values)
        {
            answered.Clear();
        }
    }

    /// <summary>
    /// Deletes a question from the pool by its lecturer ID and local index and adjusts answered indices.
    /// </summary>
    /// <param name="lecturerID">The ID of the lecturer.</param>
    /// <param name="localIndex">The local index of the question in the lecturer's list to delete.</param>
    public void DeleteQuestion(string lecturerID, int localIndex)
    {
        if (string.IsNullOrEmpty(lecturerID) || !_questionsByLecturer.TryGetValue(lecturerID, out var questions))
        {
            return;
        }

        // Validate the index and remove the question
        if (localIndex >= 0 && localIndex < questions.Count)
        {
            questions.RemoveAt(localIndex);

            // Adjust answered indices to account for the removed question
            if (_answeredByLecturer.TryGetValue(lecturerID, out var answered))
            {
                var newAnswered = new HashSet<int>();
                foreach (var i in answered)
                {
                    if (i < localIndex)
                    {
                        newAnswered.Add(i);
                    }
                    else if (i > localIndex)
                    {
                        newAnswered.Add(i - 1);
                    }
                }
                _answeredByLecturer[lecturerID] = newAnswered;
            }

            // Rebuild the flat list
            RebuildAllQuestions();

            // Save the updated question list to the XML file
            _xmlSerializer.SaveToFile(_allQuestions);
        }
    }

    /// <summary>
    /// Edits a question in the pool by replacing it with a new one at the specified lecturer ID and local index.
    /// Assumes the updated question has the same LecturerID; if not, it will be moved to the new group.
    /// </summary>
    /// <param name="lecturerID">The current ID of the lecturer for the question to edit.</param>
    /// <param name="localIndex">The local index of the question in the current lecturer's list.</param>
    /// <param name="updatedQuestion">The updated question object.</param>
    public void EditQuestion(string lecturerID, int localIndex, MultipleChoiceQuestion updatedQuestion)
    {
        if (string.IsNullOrEmpty(lecturerID) || !_questionsByLecturer.TryGetValue(lecturerID, out var questions) || updatedQuestion == null)
        {
            return;
        }

        // Validate the index
        if (localIndex >= 0 && localIndex < questions.Count)
        {
            // If LecturerID changed, remove from old group and add to new
            if (updatedQuestion.LecturerID != lecturerID)
            {
                questions.RemoveAt(localIndex);

                // Adjust answered for old lecturer
                if (_answeredByLecturer.TryGetValue(lecturerID, out var oldAnswered))
                {
                    var newOldAnswered = new HashSet<int>();
                    foreach (var i in oldAnswered)
                    {
                        if (i < localIndex)
                        {
                            newOldAnswered.Add(i);
                        }
                        else if (i > localIndex)
                        {
                            newOldAnswered.Add(i - 1);
                        }
                    }
                    _answeredByLecturer[lecturerID] = newOldAnswered;
                }

                // Add to new group
                if (!_questionsByLecturer.TryGetValue(updatedQuestion.LecturerID, out var newList))
                {
                    newList = new List<MultipleChoiceQuestion>();
                    _questionsByLecturer[updatedQuestion.LecturerID] = newList;
                }
                newList.Add(updatedQuestion);

                // Answered for new lecturer remains unchanged
            }
            else
            {
                // Same group, just update
                questions[localIndex] = updatedQuestion;
            }

            // Rebuild the flat list
            RebuildAllQuestions();

            // Save the updated question list to the XML file
            _xmlSerializer.SaveToFile(_allQuestions);
        }
    }

    /// <summary>
    /// Gets all questions in the pool as a read-only list (flattened).
    /// </summary>
    public IReadOnlyList<MultipleChoiceQuestion> AllQuestions => _allQuestions.AsReadOnly();

    /// <summary>
    /// Gets the number of unanswered questions in the pool (total across all lecturers).
    /// </summary>
    public int UnansweredCount
    {
        get
        {
            int total = 0;
            foreach (var kvp in _questionsByLecturer)
            {
                var answeredCount = _answeredByLecturer.TryGetValue(kvp.Key, out var answered) ? answered.Count : 0;
                total += kvp.Value.Count - answeredCount;
            }
            return total;
        }
    }
}