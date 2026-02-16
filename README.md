# Legal Agentic Script

This C# application leverages Semantic Kernel to generate legitimate legal documents based on user queries and uploaded reference materials.
## 🔐 Security & Base Configurations

We use the .NET Secret Manager to keep your API keys safe. These are stored in a local JSON file outside of your project folder, ensuring they are never accidentally committed to GitHub.
1. Initialize Secrets

Open your terminal in the project root and run:
```Bash

dotnet user-secrets init
```
2. Set your API Keys

Run the following commands to store your mandatory keys:

    Brave Search API (for internet research):
    ```Bash

    dotnet user-secrets set "braveApiKey" "YOUR_BRAVE_KEY_HERE"
    ```
    OpenAI API (powers gpt-4o-mini):
    Bash

    dotnet user-secrets set "openAiApiKey" "YOUR_OPENAI_KEY_HERE"

## 🚀 How to Run the Project

The app accepts two positional arguments. You can run it via the dotnet CLI from any OS.
Syntax
```Bash

dotnet run -- "<UserInput>" "<AdditionalDocumentPath>"
```
Usage Examples

Scenario A: File-based User Query
If your query is long and stored in a .txt file:
```Bash

dotnet run -- "./Queries/my_request.txt" "./Documents/state_law_requirements.pdf"
```
    Note: The script is smart. If the first argument is a valid file path, it reads the file; otherwise, it treats the string as the direct prompt.

Scenario B: A single query
If you want to use a simple query, you can just run the script like this: 
```Bash

dotnet run -- "My landloard is annoying me for X, for Y..."
```
## How It Works
### Agent Workflow

Analyst Agent

Receives user problem and any attached documents
Analyzes the legal nature of the issue
Extracts key facts and potential legal angles
Outputs research queries for the next agent


Researcher Agent

Takes the analyst's queries
Searches for relevant laws, regulations, and case law
Uses Brave Search to find current legal information
Compiles relevant legal precedents and statutes


Lawyer Agent

Combines user facts with legal research
Drafts a comprehensive legal document
Includes proper legal formatting and citations
Produces final deliverable

## 🛠 Main Libraries

    Semantic Kernel: The orchestration engine for our AI agents.

    Microsoft.Extensions.Configuration.UserSecrets: Handles the secure loading of your API keys.