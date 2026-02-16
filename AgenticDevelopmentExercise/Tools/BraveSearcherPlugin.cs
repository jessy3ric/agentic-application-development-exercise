using Microsoft.SemanticKernel;
using Newtonsoft.Json.Linq;
using System.ComponentModel;

namespace AgenticDevelopmentExercise.Tools
{
    internal class BraveSearcherPlugin
    {
        private readonly HttpClient client;
        private string API_KEY;

        public BraveSearcherPlugin(string apiKey)
        {
            API_KEY = apiKey;
            client = new HttpClient();
        }

        [KernelFunction("web_search")]
        [Description(@"Makes a web search using Brave Search API to find current information from the web. 
Use this function when you need to:
- Find current/recent information not in your knowledge base
- Look up facts, news, or events that may have changed
- Get real-time data like prices, weather, or stock information
- Research topics requiring up-to-date web sources
- Verify or fact-check information

The function returns a JSON object with the following structure:
{
  ""grounding"": {
    ""generic"": [],
    ""poi"": {
      ""name"": ""string"",
      ""url"": ""string"",
      ""title"": ""string"",
      ""snippets"": [""string""]
    },
    ""map"": []
  },
  ""sources"": {}
}

The 'grounding.poi' (points of interest) contains relevant web results with:
- name: The source name
- url: The web page URL
- title: The page title
- snippets: Array of relevant text excerpts from the page

Use the snippets to answer user queries with accurate, cited information.")]
        [return: Description("A JSON object containing web search results with grounding information, sources, and relevant snippets from web pages")]
        public async Task<JObject?> Search(
            [Description("The search query to look up on the web. Use clear, specific keywords for best results.")]
        string query)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("X-Subscription-Token", API_KEY);
            string url = $"https://api.search.brave.com/res/v1/llm/context?q={Uri.EscapeDataString(query)}";

            try
            {
                string response = await client.GetStringAsync(url);
                return JObject.Parse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Brave Search Error: {ex.Message}");
                return null;
            }
        }
    }
}
