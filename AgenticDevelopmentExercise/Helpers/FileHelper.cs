using AgenticDevelopmentExercise.Entities;
using System.Xml.Linq;

namespace AgenticDevelopmentExercise.Helpers
{
    internal class FileHelper
    {
        private string[] _fileNames = { "Prompts/AnalyzerAgentPrompt.txt", "Prompts/ResearcherAgentPrompt.txt", "Prompts/LawyerAgentPrompt.txt" };

        public async Task<IDictionary<string, string>> GetSystemPrompts()
        {
            IDictionary<string, string> promptMap = new Dictionary<string, string>();
            try
            {
                promptMap = await LoadPromptsAsDictionaryAsync();
            }
             catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return promptMap;
        }

        public async Task<(string? content, DocumentType type)> ParseDocumentAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Document file not found: {filePath}");
            }

            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            // Text-based documents
            switch (extension)
            {
                case ".txt":
                case ".md":
                case ".log":
                    return (await File.ReadAllTextAsync(filePath), DocumentType.Text);

                case ".pdf":
                    return (await ParsePdfAsync(filePath), DocumentType.PDF);

                case ".docx":
                case ".doc":
                    return (await ParseWordDocumentAsync(filePath), DocumentType.Word);

                case ".csv":
                    return (await File.ReadAllTextAsync(filePath), DocumentType.CSV);

                case ".json":
                    return (await File.ReadAllTextAsync(filePath), DocumentType.JSON);

                case ".xml":
                    return (await File.ReadAllTextAsync(filePath), DocumentType.XML);

                default:
                    throw new ArgumentException(
                        $"Unsupported document type: {extension}\n" +
                        "Supported formats: Images (jpg, png, gif, etc.), PDF, DOCX, TXT, MD, CSV, JSON, XML"
                    );
            }
        }

        private async Task<string> ParsePdfAsync(string filePath)
        {
            var parser = new DocumentParser();
            return await parser.ParsePdfAsync(filePath);
        }

        private async Task<string> ParseWordDocumentAsync(string filePath)
        {
            var parser = new DocumentParser();
            return await parser.ParseWordDocumentAsync(filePath);
        }

        /// <summary>
        /// Loads files into a Dictionary where Key = Filename, Value = Content.
        /// </summary>
        private async Task<Dictionary<string, string>> LoadPromptsAsDictionaryAsync()
        {
            var absolutePaths = _fileNames.Select(p => Path.GetFullPath(p)).ToArray();

            // Create the tasks for reading
            var tasks = absolutePaths.Select(async path => new
            {
                Name = Path.GetFileName(path),
                Content = await File.ReadAllTextAsync(path)
            });

            // Wait for all reads to complete
            var results = await Task.WhenAll(tasks);

            return results.ToDictionary(x => x.Name.Split('.')[0], x => x.Content);
        }
    }
}
