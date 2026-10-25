using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using SFB;
using System;

public class QuizInfoMakerManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject quizInfoMakerPanel;

    [Header("Main Quiz Maker Buttons")]
    public GameObject backButton;
    public GameObject quizSelectButton;

    [Header("Entry Generation")]
    public QuizInfoEntryUI entryTemplate;
    public Transform entryContent;

    [Header("Quiz Info Buttons")]
    public Button confirmEntryButton;
    public Button cancelEntryButton;

    private readonly List<QuizInfoEntryUI> generatedEntries =
        new List<QuizInfoEntryUI>();

    private string currentQuizPath;
    private string currentInfoPath;

    private void Start()
    {
        if (quizInfoMakerPanel != null)
            quizInfoMakerPanel.SetActive(false);

        if (confirmEntryButton != null)
            confirmEntryButton.onClick.AddListener(ConfirmEntries);

        if (cancelEntryButton != null)
            cancelEntryButton.onClick.AddListener(CloseQuizInfoMaker);
    }

    public void OpenQuizInfoMaker()
    {
        currentQuizPath = QuizMakerManager.SelectedQuizFilePath;

        if (string.IsNullOrEmpty(currentQuizPath) ||
            !File.Exists(currentQuizPath))
        {
            Debug.LogWarning("Select an existing quiz file before opening Quiz Info Maker.");
            {
                return;
            }
        }

        string folder = Path.GetDirectoryName(currentQuizPath);
        string quizName = Path.GetFileNameWithoutExtension(currentQuizPath);

        currentInfoPath = Path.Combine(folder, quizName + "_info.txt");

        // Create the companion file only if it does not exist.
        if (!File.Exists(currentInfoPath))
        {
            File.WriteAllText(currentInfoPath, "");
            Debug.Log("Created Quiz Info file: " + currentInfoPath);
        }

        ClearGeneratedEntries();
        GenerateEntries();
        LoadExistingEntries();

        quizInfoMakerPanel.SetActive(true);

        if (backButton != null)
        { 
            backButton.SetActive(false);
        }

        if (quizSelectButton != null)
        {
            quizSelectButton.SetActive(false);
        }
    }

    private void GenerateEntries()
    {
        string[] lines = File.ReadAllLines(currentQuizPath);

        string questionID = null;
        string questionText = null;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (!line.StartsWith("ID:"))
            {
                continue;
            }

            questionID = line.Substring(3).Trim();

            if (i + 1 >= lines.Length)
            {
                continue;
            }

            questionText = lines[i + 1].Trim();

            // Remove the displayed question number, if present.
            int dotIndex = questionText.IndexOf('.');

            if (dotIndex >= 0)
            {
                questionText = questionText.Substring(dotIndex + 1).Trim();
            }

            QuizInfoEntryUI entry = Instantiate(entryTemplate, entryContent);

            entry.gameObject.SetActive(true);
            entry.Setup(questionID, questionText, this);

            generatedEntries.Add(entry);
        }

        Debug.Log("Generated " + generatedEntries.Count + " Quiz Info entries.");
    }

    private void ClearGeneratedEntries()
    {
        foreach (QuizInfoEntryUI entry in generatedEntries)
        {
            if (entry != null)
            {
                Destroy(entry.gameObject);
            }
        }

        generatedEntries.Clear();
    }

    public void CloseQuizInfoMaker()
    {
        if (quizInfoMakerPanel != null)
            quizInfoMakerPanel.SetActive(false);

        if (backButton != null)
            backButton.SetActive(true);

        if (quizSelectButton != null)
            quizSelectButton.SetActive(true);

        ClearGeneratedEntries();
    }

    private void ConfirmEntries()
    {
        if (string.IsNullOrEmpty(currentInfoPath))
        {
            Debug.LogError("No Quiz Info file path selected.");
            {
                return;
            }
        }

        StringBuilder builder = new StringBuilder();

        foreach (QuizInfoEntryUI entry in generatedEntries)
        {
            QuizInfoData info = entry.GetInfoData();

            string title = info.title ?? "";
            string imagePath = info.imagePath ?? "";
            string description = info.description ?? "";

            // Keep each field on one line for compatibility
            // with the existing Quiz Info parser.
            title = title.Replace("\r", " ").Replace("\n", " ");
            imagePath = imagePath.Replace("\r", " ").Replace("\n", " ");
            description = description.Replace("\r", " ").Replace("\n", " ");

            builder.AppendLine("[ID:" + info.id + "]");
            builder.AppendLine("TITLE: " + title);
            builder.AppendLine("IMAGE: " + imagePath);
            builder.AppendLine("DESC:");
            builder.AppendLine(description);
            builder.AppendLine("[END]");
            builder.AppendLine();
        }

        try
        {
            File.WriteAllText(currentInfoPath, builder.ToString());

            Debug.Log("Saved " + generatedEntries.Count +" Quiz Info entries to: " + currentInfoPath);

            CloseQuizInfoMaker();
        }

        catch (System.Exception ex)
        {
            Debug.LogError("Failed to save Quiz Info: " + ex.Message);
        }
    }

    private void LoadExistingEntries()
    {
        if (!File.Exists(currentInfoPath))
        {
            return;
        }

        string[] lines = File.ReadAllLines(currentInfoPath);

        Dictionary<string, QuizInfoData> savedInfo = new Dictionary<string, QuizInfoData>();

        QuizInfoData currentInfo = null;

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            if (line.StartsWith("[ID:") && line.EndsWith("]"))
            {
                currentInfo = new QuizInfoData();

                currentInfo.id = line.Substring(4, line.Length - 5).Trim();

                currentInfo.title = "";
                currentInfo.imagePath = "";
                currentInfo.description = "";
            }

            else if (line == "[END]")
            {
                if (currentInfo != null &&!string.IsNullOrEmpty(currentInfo.id))
                {
                    savedInfo[currentInfo.id] = currentInfo;
                }

                currentInfo = null;
            }

            else if (currentInfo != null)
            {
                if (line.StartsWith("TITLE:"))
                {
                    currentInfo.title = line.Substring(6).Trim();
                }
                else if (line.StartsWith("IMAGE:"))
                {
                    currentInfo.imagePath = line.Substring(6).Trim();
                }
                else if (line.StartsWith("DESC:"))
                {
                    currentInfo.description = "";
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    if (!string.IsNullOrEmpty(currentInfo.description))
                        currentInfo.description += "\n";

                    currentInfo.description += line;
                }
            }
        }

        foreach (QuizInfoEntryUI entry in generatedEntries)
        {
            if (savedInfo.TryGetValue(entry.QuestionID, out QuizInfoData info))
            {
                entry.LoadExistingInfo(info);
            }
        }

        Debug.Log("Loaded " + savedInfo.Count + " existing Quiz Info entries.");
    }

    public void SelectEntryImage(QuizInfoEntryUI entry)
    {
        if (entry == null || string.IsNullOrEmpty(currentQuizPath))
            return;

        ExtensionFilter[] extensions =
        {
        new ExtensionFilter("Image Files", "png", "jpg", "jpeg")
    };

        string[] paths = StandaloneFileBrowser.OpenFilePanel(
            "Select Quiz Info Image",
            "",
            extensions,
            false
        );

        if (paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
            return;

        try
        {
            string packageFolder = Path.GetDirectoryName(currentQuizPath);
            string imagesFolder = Path.Combine(packageFolder, "Images");

            Directory.CreateDirectory(imagesFolder);

            string source = Path.GetFullPath(paths[0]);
            string fileName = Path.GetFileName(source);
            string destination = Path.GetFullPath(
                Path.Combine(imagesFolder, fileName)
            );

            // Don't overwrite an existing image with different contents.
            if (!string.Equals(source, destination, StringComparison.OrdinalIgnoreCase)
                && File.Exists(destination))
            {
                string name = Path.GetFileNameWithoutExtension(fileName);
                string extension = Path.GetExtension(fileName);
                int number = 1;

                do
                {
                    fileName = name + "_" + number + extension;
                    destination = Path.Combine(imagesFolder, fileName);
                    number++;
                }
                while (File.Exists(destination));
            }

            if (!string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(source, destination);
            }

            entry.SetImagePath("Images/" + fileName);

            Debug.Log(
                "Quiz Info image imported for " +
                entry.QuestionID + ": " + entry.ImagePath
            );
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to import Quiz Info image: " + ex.Message);
        }
    }

}