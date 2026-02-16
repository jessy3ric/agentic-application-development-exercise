using AgenticDevelopmentExercise.Helpers;
using AgenticDevelopmentExercise.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.Connectors.OpenAI;
#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0050

public class Program
{
    // Create a thread-safe queue
    private const string ModelId = "gpt-4o-mini";

    public static async Task Main(string[] args)
    {
        Console.WriteLine("--- Agentic Script Started ---");
        if (args.Length == 0) 
        {
            throw new ArgumentException("The args are empty. Without user input (text, document, image) this script cannot work. Please refer to the documentation");
        }

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

        var fileHelper = new FileHelper();
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
        SequentialOrchestration agentsOrchestration = await SetupAgents(kernel, prompts);

        InProcessRuntime runtime = new InProcessRuntime();
        await runtime.StartAsync();

        var result = await agentsOrchestration.InvokeAsync(
        "example user query",
        runtime);
        string output = await result.GetValueAsync(TimeSpan.FromSeconds(120));
        Console.WriteLine($"\n# RESULT: {output}");
    }

    public static async Task<SequentialOrchestration> SetupAgents(Kernel kernel, IDictionary<string, string> prompts)
    {
        // Enable planning
        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        ChatCompletionAgent analystAgent = new ChatCompletionAgent
        {
            Name = "Analyst",
            Instructions = prompts["AnalyzerAgentPrompt"],
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


        SequentialOrchestration orchestration = new(analystAgent, researchAgent, lawyerAgent);
        return orchestration;
    }
}
