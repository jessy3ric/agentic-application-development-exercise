# Legal Agentic Script
This c# script aims to provide a legitimate legal document to a user query

## Base configurations
You need to get two api keys and store them in the user secrets:

1. "braveApiKey": this api key can be fetched on https://brave.com/search/api/ ; this api will be used by one of the agent to make queries on internet.
2. "openAiApiKey": this api key can be fetched on https://platform.openai.com/api-keys ; it will be used to power the agents with "gpt-4o-mini".

These two keys are mandatory for the script to fully work.

## Main library used
Semantic-Kernel