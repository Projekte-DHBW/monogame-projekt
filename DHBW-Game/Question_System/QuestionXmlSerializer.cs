using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DHBW_Game.Question_System;

/// <summary>
/// Handles serialization and deserialization of multiple-choice questions to/from XML.
/// </summary>
public class QuestionXmlSerializer
{
    // Default file path for XML file operations
    private readonly string _defaultFilePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestionXmlSerializer"/> class with an optional default file path.
    /// </summary>
    /// <param name="defaultFilePath">The default path for file operations. If null, uses user-specific AppData directory.</param>
    public QuestionXmlSerializer(string defaultFilePath = null)
    {
        // Set the default file path, using AppData directory if none provided
        _defaultFilePath = defaultFilePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DHBW-Game", "questions.xml");
    }

    /// <summary>
    /// Loads and parses questions from an XML file.
    /// </summary>
    /// <param name="filePath">The path to the XML file. Uses default if null.</param>
    /// <returns>A list of valid <see cref="MultipleChoiceQuestion"/> objects. Returns empty list on failure.</returns>
    public List<MultipleChoiceQuestion> LoadFromFile(string filePath = null)
    {
        // Use provided file path or default
        var path = filePath ?? _defaultFilePath;
        
        // Check if the file exists
        if (!File.Exists(path))
        {
            Console.WriteLine($"Warning: Question file not found at {path}. Returning empty list.");
            return new List<MultipleChoiceQuestion>();
        }

        try
        {
            // Load and parse the XML document
            var doc = XDocument.Load(path);
            return ParseQuestions(doc);
        }
        catch (Exception ex)
        {
            // Handle errors during file loading
            Console.WriteLine($"Error loading XML file at {path}: {ex.Message}");
            return new List<MultipleChoiceQuestion>();
        }
    }

    /// <summary>
    /// Parses questions from an XML string.
    /// </summary>
    /// <param name="xmlContent">The XML content as a string.</param>
    /// <returns>A list of valid <see cref="MultipleChoiceQuestion"/> objects. Returns empty list on failure.</returns>
    public List<MultipleChoiceQuestion> LoadFromString(string xmlContent)
    {
        // Check if the XML content is empty or whitespace
        if (string.IsNullOrWhiteSpace(xmlContent))
        {
            Console.WriteLine("Warning: XML content is empty. Returning empty list.");
            return new List<MultipleChoiceQuestion>();
        }

        try
        {
            // Parse the XML string into an XDocument
            var doc = XDocument.Parse(xmlContent);
            return ParseQuestions(doc);
        }
        catch (Exception ex)
        {
            // Handle errors during string parsing
            Console.WriteLine($"Error parsing XML string: {ex.Message}");
            return new List<MultipleChoiceQuestion>();
        }
    }

    /// <summary>
    /// Saves questions to an XML file in the Gemini-compatible format.
    /// </summary>
    /// <param name="questions">The list of questions to save.</param>
    /// <param name="filePath">The path to save to. Uses default if null.</param>
    public void SaveToFile(List<MultipleChoiceQuestion> questions, string filePath = null)
    {
        // Use provided file path or default
        var path = filePath ?? _defaultFilePath;
        
        // Create a new XML document with a root <Questions> element
        var doc = new XDocument(new XElement("Questions"));

        // Process each question for serialization
        foreach (var q in questions ?? new List<MultipleChoiceQuestion>())
        {
            // Validate question before saving
            if (string.IsNullOrEmpty(q.QuestionText) || q.Options == null || q.Options.Count < 1 ||
                q.CorrectOptionIndex < 0 || q.CorrectOptionIndex >= q.Options.Count)
            {
                Console.WriteLine($"Warning: Skipping invalid question during save: '{q.QuestionText ?? "Unnamed"}'");
                continue;
            }

            // Create a <Question> element with Topic attribute
            var questionElement = new XElement("Question", new XAttribute("Topic", q.Topic ?? "Unknown"));
            
            // Add question text
            questionElement.Add(new XElement("Text", q.QuestionText));

            // Create <Options> element and add each option
            var optionsElement = new XElement("Options");
            foreach (var option in q.Options)
            {
                optionsElement.Add(new XElement("Option", option));
            }
            questionElement.Add(optionsElement);

            // Add correct option index and explanation
            questionElement.Add(new XElement("CorrectOptionIndex", q.CorrectOptionIndex));
            questionElement.Add(new XElement("Explanation", q.Explanation ?? ""));

            // Add the question element to the document root
            doc.Root?.Add(questionElement);
        }

        // Ensure the directory exists before saving
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
        
        // Save the XML document to the file
        doc.Save(path);
    }

    /// <summary>
    /// Serializes questions to an XML string.
    /// </summary>
    /// <param name="questions">The list of questions to serialize.</param>
    /// <returns>The XML string representation of the questions.</returns>
    public string SaveToString(List<MultipleChoiceQuestion> questions)
    {
        // Create a new XML document with a root <Questions> element
        var doc = new XDocument(new XElement("Questions"));

        // Process each question for serialization
        foreach (var q in questions ?? new List<MultipleChoiceQuestion>())
        {
            // Validate question before serializing
            if (string.IsNullOrEmpty(q.QuestionText) || q.Options == null || q.Options.Count < 1 ||
                q.CorrectOptionIndex < 0 || q.CorrectOptionIndex >= q.Options.Count)
            {
                continue; // Silently skip invalid questions for string output
            }

            // Create a <Question> element with Topic attribute
            var questionElement = new XElement("Question", new XAttribute("Topic", q.Topic ?? "Unknown"));
            
            // Add question text
            questionElement.Add(new XElement("Text", q.QuestionText));

            // Create <Options> element and add each option
            var optionsElement = new XElement("Options");
            foreach (var option in q.Options)
            {
                optionsElement.Add(new XElement("Option", option));
            }
            questionElement.Add(optionsElement);

            // Add correct option index and explanation
            questionElement.Add(new XElement("CorrectOptionIndex", q.CorrectOptionIndex));
            questionElement.Add(new XElement("Explanation", q.Explanation ?? ""));

            // Add the question element to the document root
            doc.Root?.Add(questionElement);
        }

        // Return the XML string
        return doc.ToString();
    }

    /// <summary>
    /// Parses questions from an <see cref="XDocument"/> object.
    /// </summary>
    /// <param name="doc">The XML document containing the questions.</param>
    /// <returns>A list of valid <see cref="MultipleChoiceQuestion"/> objects.</returns>
    private List<MultipleChoiceQuestion> ParseQuestions(XDocument doc)
    {
        // Initialize the list for parsed questions
        var questions = new List<MultipleChoiceQuestion>();

        // Validate that the XML has a valid <Questions> root element
        if (doc.Root == null || doc.Root.Name != "Questions")
        {
            Console.WriteLine("Warning: XML does not have a <Questions> root element.");
            return questions;
        }

        // Process each <Question> element
        foreach (var questionElement in doc.Root.Elements("Question"))
        {
            try
            {
                // Create a new MultipleChoiceQuestion object
                var question = new MultipleChoiceQuestion
                {
                    // Extract Topic attribute, default to "Unknown" if missing
                    Topic = questionElement.Attribute("Topic")?.Value ?? "Unknown",
                    
                    // Extract question text
                    QuestionText = questionElement.Element("Text")?.Value,
                    
                    // Extract options from <Options> element
                    Options = questionElement.Element("Options")?.Elements("Option")
                        .Select(o => o.Value)
                        .ToList() ?? new List<string>(),
                    
                    // Parse correct option index, default to -1 if invalid
                    CorrectOptionIndex = int.TryParse(questionElement.Element("CorrectOptionIndex")?.Value, out int index) ? index : -1,
                    
                    // Extract explanation, default to empty string if missing
                    Explanation = questionElement.Element("Explanation")?.Value ?? ""
                };

                // Validate question text
                if (string.IsNullOrEmpty(question.QuestionText))
                {
                    Console.WriteLine("Warning: Skipping question with missing <Text>.");
                    continue;
                }

                // Validate options
                if (question.Options.Count < 1)
                {
                    Console.WriteLine($"Warning: Skipping question '{question.QuestionText}' with no options.");
                    continue;
                }

                // Validate correct option index
                if (question.CorrectOptionIndex < 0 || question.CorrectOptionIndex >= question.Options.Count)
                {
                    Console.WriteLine($"Warning: Skipping question '{question.QuestionText}' with invalid CorrectOptionIndex.");
                    continue;
                }

                // Add valid question to the list
                questions.Add(question);
            }
            catch (Exception ex)
            {
                // Handle errors during individual question parsing
                Console.WriteLine($"Error parsing question: {ex.Message}");
            }
        }

        // Return the list of parsed questions
        return questions;
    }
}