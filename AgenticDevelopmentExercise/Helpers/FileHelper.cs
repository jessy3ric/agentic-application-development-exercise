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
