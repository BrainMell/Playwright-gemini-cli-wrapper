# Playwright Gemini CLI Wrapper

A C# .NET application that provides a CLI interface for interacting with the Gemini web interface using Playwright. This tool allows for automated and terminal-based management of Gemini conversations.

## Features

- **Session Management**: Persistent authentication using browser contexts.
- **Chat Pinning**: Quickly pin important conversations for easy access.
- **Chat Unpinning**: Remove chats from the pinned section when they are no longer needed.
- **Chat Renaming**: Update conversation titles directly from the CLI.
- **Chat Deletion**: Remove unwanted conversations with confirmation prompts.
- **Conversation Sharing**: Generate and manage shareable links for your conversations.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [Playwright](https://playwright.dev/dotnet/docs/intro)

## Setup

1. **Clone the repository**:
   ```bash
   git clone https://github.com/BrainMell/Playwright-gemini-cli-wrapper.git
   cd Playwright-gemini-cli-wrapper
   ```

2. **Install Dependencies**:
   ```bash
   dotnet build
   ```

3. **Install Playwright Browsers**:
   On Windows (PowerShell):
   ```powershell
   pwsh bin/Debug/net10.0/playwright.ps1 install
   ```
   On Linux/macOS:
   ```bash
   ./bin/Debug/net10.0/playwright install
   ```

## Usage

Run the application using the dotnet CLI:

```bash
dotnet run
```

The application will launch a Chromium instance and provide an interactive menu for managing your Gemini chats.

## Project Structure

- `Program.cs`: Main application logic and Playwright automation scripts.
- `PlaywrightProfile/`: Local browser profile data (excluded from git).
- `auth.json`: Authentication state (excluded from git).

## License

This project is for educational and personal use.
