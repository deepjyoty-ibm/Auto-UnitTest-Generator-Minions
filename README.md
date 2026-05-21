# Auto Unit Test Generator

[![.NET](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/badge/NuGet-Auto--UnitTest--Generator.cli-blue.svg)](https://www.nuget.org/packages/Auto-UnitTest-Generator.cli)

An AI-powered CLI tool that automatically generates high-quality unit tests for C# code using multi-agent orchestration. Leverages OpenAI or Groq AI models to create comprehensive test suites with configurable coverage targets.

## 🌟 Features

- **🤖 AI-Powered Generation**: Uses advanced AI models (GPT-4, Groq) to generate intelligent unit tests
- **🎯 Multi-Agent Architecture**: Employs specialized agents for generation, review, and fixing
- **📊 Coverage-Driven**: Automatically iterates to achieve target code coverage
- **🔧 Test Framework**: Currently supports xUnit (NUnit and MSTest support planned for future releases)
- **🎭 Mocking Library**: Currently supports Moq (NSubstitute and FakeItEasy support planned for future releases)
- **✅ Quality Assurance**: Built-in code quality analysis and unreachable code detection
- **🔄 Iterative Refinement**: Automatically fixes build errors and improves coverage
- **⚙️ Highly Configurable**: JSON-based configuration for all aspects of test generation

## 🏗️ Architecture

The tool uses a sophisticated multi-agent system:

1. **Generator Agent**: Creates initial unit tests based on source code analysis
2. **Reviewer Agent**: Analyzes generated tests for quality and coverage
3. **Fix Agent**: Resolves build errors and improves test coverage
4. **Orchestrator**: Coordinates agents through iterative refinement cycles

## 📋 Prerequisites

### Required Software
- .NET 10.0 SDK or later
- OpenAI API key (set as environment variable)
- Visual Studio 2022+ or VS Code (recommended)

### Required NuGet Packages (Test Project)

Your test project must have the following packages installed:

#### Test Framework & Dependencies
```bash
cd YourTestProject
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package coverlet.collector
dotnet restore
```

#### Coverage Tool (Required)
```bash
dotnet tool install --global dotnet-coverage
```

## 🚀 Installation

### Install as Global Tool

```bash
dotnet tool install --global Auto-UnitTest-Generator.cli
```

### Install from Source

```bash
git clone <repository-url>
cd Auto-UnitTest-Generator
dotnet build
dotnet pack Auto-UnitTest-Generator.Cli/Auto-UnitTest-Generator.Cli.csproj
dotnet tool install --global --add-source ./Auto-UnitTest-Generator.Cli/nupkg Auto-UnitTest-Generator.cli
```

## ⚙️ Configuration

### Set OpenAI API Key

The tool uses the OpenAI API key from your environment variables. Set it before running:

**Windows (Command Prompt):**
```cmd
set OPENAI_API_KEY=your-api-key-here
# Make it permanent:
setx OPENAI_API_KEY "your-api-key-here"
```

**Windows (PowerShell):**
```powershell
$env:OPENAI_API_KEY = "your-api-key-here"
# Make it permanent:
[System.Environment]::SetEnvironmentVariable('OPENAI_API_KEY', 'your-api-key-here', 'User')
```

**Linux/Mac:**
```bash
export OPENAI_API_KEY="your-api-key-here"
# Make it permanent (add to ~/.bashrc or ~/.zshrc):
echo 'export OPENAI_API_KEY="your-api-key-here"' >> ~/.bashrc
```

### Default Configuration

The tool comes with built-in defaults (can be overridden via command-line options):
- **AI Provider**: OpenAI
- **Model**: gpt-4o
- **Test Framework**: xUnit
- **Mocking Library**: Moq
- **Assertion Library**: FluentAssertions
- **Target Coverage**: 90% (override with `--coverage`)
- **Max Attempts**: 3 (override with `--maxattempts`)

## 📖 Usage

### Basic Usage

```bash
autounittest --filepath ./src/UserService.cs --generatefilepath ./tests/UserServiceTests.cs
```

### With Custom Coverage Target

```bash
autounittest -f ./src/UserService.cs -g ./tests/UserServiceTests.cs --coverage 95
```

### With Custom Max Attempts

```bash
autounittest -f ./src/UserService.cs -g ./tests/UserServiceTests.cs --maxattempts 5
```

### With Both Custom Coverage and Max Attempts

```bash
autounittest -f ./src/UserService.cs -g ./tests/UserServiceTests.cs --coverage 95 --maxattempts 5
```

### Command-Line Options

| Option | Alias | Description | Required | Default |
|--------|-------|-------------|----------|---------|
| `--filepath` | `-f` | Path to source C# file | Yes | - |
| `--generatefilepath` | `-g` | Path for generated test file | Yes | - |
| `--coverage` | `-c` | Target coverage percentage (0-100) | No | 90 |
| `--maxattempts` | `-m` | Maximum refinement iterations | No | 3 |

## 🎯 How It Works

1. **Analysis**: Reads and analyzes the source code structure
2. **Context Extraction**: Extracts namespaces, dependencies, and method signatures
3. **Generation**: AI generates comprehensive unit tests
4. **Validation**: Builds and runs tests to verify correctness
5. **Coverage Analysis**: Measures code coverage using dotnet-coverage
6. **Review**: Analyzes test quality and identifies gaps
7. **Refinement**: Iteratively improves tests until target coverage is achieved
8. **Quality Check**: Detects unreachable code and quality issues

## 📊 Output

The tool provides detailed feedback:

```
✅ SUCCESS!
   Test file: ./tests/UserServiceTests.cs
   Coverage: 92.5%
   Attempts: 2

📊 Iteration Log:
   Iteration 1: Generated initial tests (Coverage: 85.0%)
   Iteration 2: Fixed build errors and improved coverage (Coverage: 92.5%)

⚠️  CODE QUALITY ANALYSIS
   • Line 45-50: Unreachable code detected after return statement
   💡 Recommendation: Review these sections in your source code.
```

## 🔍 Example

### Source Code (UserService.cs)

```csharp
public class UserService
{
    private readonly IUserRepository _repository;
    private readonly IEmailService _emailService;

    public UserService(IUserRepository repository, IEmailService emailService)
    {
        _repository = repository;
        _emailService = emailService;
    }

    public User GetUser(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Invalid Id");

        var user = _repository.GetById(id);
        if (user is null)
            throw new UserNotFoundException("User not found");

        return user;
    }

    public async Task RegisterUserAsync(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
            throw new ArgumentException("Email required");

        _repository.Add(user);
        await _emailService.SendWelcomeEmailAsync(user.Email);
    }
}
```

### Generated Tests (UserServiceTests.cs)

The tool automatically generates comprehensive tests including:
- Happy path scenarios
- Exception handling tests
- Edge cases and boundary conditions
- Async/await patterns
- Mock setup and verification
- Fluent assertions

## 🛠️ Supported Technologies

### AI Provider
- **OpenAI**: Uses GPT-4o model for intelligent test generation
- Set `OPENAI_API_KEY` environment variable

### Testing Stack (Current Support)
- **Test Framework**: xUnit (primary support)
- **Mocking Library**: Moq (primary support)
- **Assertion Library**: FluentAssertions

### Future Roadmap
- **Test Frameworks**: NUnit, MSTest (planned)
- **Mocking Libraries**: NSubstitute, FakeItEasy (planned)

## 📁 Project Structure

```
Auto-UnitTest-Generator/
├── Auto-UnitTest-Generator.Cli/          # CLI application
│   ├── Program.cs                         # Entry point
│   └── autounittest.config.json          # Default configuration
├── Auto-UnitTest-Generator.Infrastructure/ # Core logic
│   ├── Agents/                            # AI agents
│   │   ├── GeneratorAgent.cs
│   │   ├── ReviewerAgent.cs
│   │   └── FixAgent.cs
│   ├── AI/                                # AI client implementations
│   ├── Configuration/                     # Configuration models
│   ├── Services/                          # Core services
│   └── Models/                            # Data models
└── SandboxExample/                        # Example project
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## 👥 Authors

- Nagarajan S
- Deepjyoty Roy
- Ramkumar T
- AbdulHameed Sayed

**Company**: IBM

## 🔗 Links

- [NuGet Package](https://www.nuget.org/packages/Auto-UnitTest-Generator.cli)
- [Documentation](./QUICKSTART.md)
- [Issue Tracker](https://github.com/your-repo/issues)

## 💡 Tips

1. **Start with lower coverage targets** (70-80%) and increase gradually
2. **Use strict mode** for production code
3. **Review generated tests** before committing
4. **Set appropriate temperature** (0.2 recommended for consistency)
5. **Use environment variables** for API keys (never commit keys)

## 🐛 Troubleshooting

### Build Errors
- Ensure all dependencies are installed
- Check that the test project references the source project
- Verify NuGet packages are restored

### Low Coverage
- Increase `maxAttempts` in configuration
- Enable all generation options
- Review code quality warnings for unreachable code

### API Errors
- Verify API key is set correctly
- Check API rate limits
- Ensure model name is correct for your provider

## 📞 Support

For issues and questions:
- Open an issue on GitHub
- Check existing documentation
- Review configuration examples

---

**Happy Testing! 🎉**