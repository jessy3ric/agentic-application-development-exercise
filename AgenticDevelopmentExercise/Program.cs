using AgenticDevelopmentExercise.Entities;
using AgenticDevelopmentExercise.Helpers;
using AgenticDevelopmentExercise.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0050

public partial class Program
{
    // Create a thread-safe queue
    private const string ModelId = "gpt-4o-mini";

    public static async Task Main(string[] args)
    {
        Console.WriteLine("--- Agentic Script Started ---");
        // setup semantic kernel with the prompts and api keys and plugins
        if (args.Length == 0)
        {
            throw new ArgumentException("The args are empty. Without user input (text, document, image) this script cannot work. Please refer to the documentation");
        }
        var fileHelper = new FileHelper();

        (string userProblem, string? documentContent, DocumentType documentType) = await ParseArgumentsAsync(args, fileHelper);

        var config = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

        var openaiApiKey = config.GetValue<string>("openAiApiKey");
        if (string.IsNullOrEmpty(openaiApiKey))
        {
            throw new ArgumentException("The openai api key must be present in the user secrets. Refer to the readme.");
        }

        var braveApiKey = config.GetValue<string>("braveApiKey");
        if (string.IsNullOrEmpty(braveApiKey))
        {
            throw new ArgumentException("The brave api key must be present in the user secrets. Refer to the readme.");
        }

        var prompts = await fileHelper.GetSystemPrompts();


        var kernelBuilder = Kernel.CreateBuilder();

        kernelBuilder.AddOpenAIChatCompletion(
            modelId: ModelId,
            apiKey: openaiApiKey
        );

        kernelBuilder.Plugins.AddFromObject(
            new BraveSearcherPlugin(braveApiKey),
            "Browser"
        );
        

        Kernel? kernel = kernelBuilder.Build();
        // setup agents
        (SequentialOrchestration agentsOrchestration, ChatHistory chatHistory) = await SetupAgents(kernel, prompts, userProblem, documentContent, documentType);

        InProcessRuntime runtime = new InProcessRuntime();
        await runtime.StartAsync();

        var result = await agentsOrchestration.InvokeAsync(
        userProblem,
        runtime);
        string output = await result.GetValueAsync(TimeSpan.FromSeconds(1000));
        Console.WriteLine($"\n# RESULT: {output}");

        Console.WriteLine("\n\nORCHESTRATION HISTORY");
        foreach (ChatMessageContent message in chatHistory)
        {
            Console.WriteLine(message.Content);
            Console.WriteLine("\\n\\n");
        }
    }

    public static async Task<(SequentialOrchestration, ChatHistory)> SetupAgents(
        Kernel kernel,
        IDictionary<string, string> prompts,
        string userProblem,
        string? documentContent,
        DocumentType documentType)
    {
        // Enable planning
        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        ChatHistory history = [];

        ValueTask responseCallback(ChatMessageContent response)
        {
            history.Add(response);
            return ValueTask.CompletedTask;
        }

        // Prepare enhanced instructions with document context
        string enhancedAnalystInstructions = prompts["AnalyzerAgentPrompt"];
        if (!string.IsNullOrEmpty(documentContent))
        {
            enhancedAnalystInstructions += $"\n\n[ADDITIONAL CONTEXT]\n" +
                $"The user has provided a {documentType} document with the following content:\n" +
                $"---\n{documentContent}\n---\n" +
                $"Analyze this document in conjunction with the user's problem description.";
        }

        ChatCompletionAgent analystAgent = new ChatCompletionAgent
        {
            Name = "Analyst",
            Instructions = enhancedAnalystInstructions,
            Kernel = kernel,
            Description = "agent that analyzes the user request to help define its request legal status"
        };

        ChatCompletionAgent researchAgent = new ChatCompletionAgent
        {
            Name = "Researcher",
            Instructions = prompts["ResearcherAgentPrompt"],
            Kernel = kernel,
            Description = "agent that makes query to get the latest laws to help define the resulting legal document",
            Arguments = new KernelArguments(openAIPromptExecutionSettings)
        };

        ChatCompletionAgent lawyerAgent = new ChatCompletionAgent
        {
            Name = "Lawyer",
            Instructions = prompts["LawyerAgentPrompt"],
            Kernel = kernel,
            Description = "Edits the final document with all the laws and arguments according to the user situation",
        };

        SequentialOrchestration orchestration = new(analystAgent, researchAgent, lawyerAgent)
        {
            ResponseCallback = responseCallback,
        };

        return (orchestration, history);
    }

    private static async Task<(string userProblem, string? documentContent, DocumentType documentType)> ParseArgumentsAsync(string[] args, FileHelper fileHelper)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException(
                "No arguments provided. Please provide at least a problem description or path to a text file.\n" +
                "Usage: dotnet run \"<problem>\" [document_path]"
            );
        }

        // Parse first argument (user problem)
        string userProblem = await ParseUserProblemAsync(args[0]);
        Console.WriteLine($"[INFO] User problem loaded ({userProblem.Length} characters)");

        // Parse second argument (optional document)
        string? documentContent = null;
        DocumentType documentType = DocumentType.None;

        if (args.Length >= 2)
        {
            (documentContent, documentType) = await fileHelper.ParseDocumentAsync(args[1]);
            Console.WriteLine($"[INFO] Document loaded: {documentType}");
        }

        return (userProblem, documentContent, documentType);
    }

    private static async Task<string> ParseUserProblemAsync(string input)
    {
        // Check if input is a file path
        if (File.Exists(input))
        {
            string extension = Path.GetExtension(input).ToLowerInvariant();

            if (extension == ".txt")
            {
                return await File.ReadAllTextAsync(input);
            }
            else
            {
                throw new ArgumentException(
                    $"Unsupported file type for problem description: {extension}. " +
                    "Only .txt files are supported for the first argument."
                );
            }
        }

        // If not a file, treat as direct string input
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("The problem description cannot be empty.");
        }

        return input;
    }

}
