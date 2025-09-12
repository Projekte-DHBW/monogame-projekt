using System.Collections.Generic;

namespace DHBW_Game.Question_System;

/// <summary>
/// Represents a multiple-choice question with a question text, options, correct answer, explanation, and topic.
/// </summary>
public class MultipleChoiceQuestion
{
    /// <summary>
    /// Gets or sets the text of the question.
    /// </summary>
    public string QuestionText { get; set; }

    /// <summary>
    /// Gets or sets the tts friendly version of the question text.
    /// </summary>
    public string TTSFriendlyText { get; set; }

    /// <summary>
    /// Gets or sets the lecturer id of the question. Usually, this is the surname of the lecturer.
    /// </summary>
    public string LecturerID { get; set; }

    /// <summary>
    /// Gets or sets the list of answer options for the question.
    /// </summary>
    public List<string> Options { get; set; }

    /// <summary>
    /// Gets or sets the index of the correct option in the <see cref="Options"/> list.
    /// </summary>
    public int CorrectOptionIndex { get; set; }

    /// <summary>
    /// Gets or sets the explanation for the correct answer.
    /// </summary>
    public string Explanation { get; set; }

    /// <summary>
    /// Gets or sets the topic or category of the question.
    /// </summary>
    public string Topic { get; set; }
}