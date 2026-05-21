# Quick Start Guide - Auto Unit Test Generator

Get started with Auto Unit Test Generator in 5 minutes! This guide will walk you through installation, configuration, and generating your first unit tests.

## 📋 Prerequisites

Before you begin, ensure you have:

### Required Software
- .NET 10.0 SDK or later
- OpenAI API key (see Step 2)
- A test project in your solution

### Required NuGet Packages

Your test project must have the following packages installed:

```bash
cd YourTestProject
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package coverlet.collector
dotnet restore
```

### Coverage Tool (Required)
```bash
dotnet tool install --global dotnet-coverage
```

## 📦 Step 1: Installation

### Option A: Install from NuGet (Recommended)

```bash
dotnet tool install --global Auto-UnitTest-Generator.cli
```

### Option B: Install from Source

```bash
git clone <repository-url>
cd Auto-UnitTest-Generator
dotnet pack Auto-UnitTest-Generator.Cli/Auto-UnitTest-Generator.Cli.csproj
dotnet tool install --global --add-source ./Auto-UnitTest-Generator.Cli/nupkg Auto-UnitTest-Generator.cli
```

### Verify Installation

```bash
autounittest --help
```

You should see the help message with available options.

## 🔑 Step 2: Set Up API Key

### For OpenAI

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

### For Groq (Alternative)

**Windows (Command Prompt):**
```cmd
set GROQ_API_KEY=your-groq-api-key-here
# Make it permanent:
setx GROQ_API_KEY "your-groq-api-key-here"
```

**Windows (PowerShell):**
```powershell
$env:GROQ_API_KEY = "your-groq-api-key-here"
[System.Environment]::SetEnvironmentVariable('GROQ_API_KEY', 'your-groq-api-key-here', 'User')
```

**Linux/Mac:**
```bash
export GROQ_API_KEY="your-groq-api-key-here"
echo 'export GROQ_API_KEY="your-groq-api-key-here"' >> ~/.bashrc
```

## 🎯 Step 3: Generate Your First Test

### Basic Example

Let's say you have a file `src/Services/UserService.cs`:

```bash
autounittest --filepath ./src/Services/UserService.cs --generatefilepath ./tests/UserServiceTests.cs
```

### Using Short Aliases

```bash
autounittest -f ./src/Services/UserService.cs -g ./tests/UserServiceTests.cs
```

### With Custom Coverage Target

```bash
autounittest -f ./src/Services/UserService.cs -g ./tests/UserServiceTests.cs --coverage 95
```

### With Custom Max Attempts

```bash
autounittest -f ./src/Services/UserService.cs -g ./tests/UserServiceTests.cs --maxattempts 5
```

### With Both Custom Options

```bash
autounittest -f ./src/Services/UserService.cs -g ./tests/UserServiceTests.cs --coverage 95 --maxattempts 5
```

### Available Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--filepath` | `-f` | Path to source C# file (required) | - |
| `--generatefilepath` | `-g` | Path for generated test file (required) | - |
| `--coverage` | `-c` | Target coverage percentage (0-100) | 90 |
| `--maxattempts` | `-m` | Maximum refinement iterations | 3 |

## 📊 Step 4: Understanding the Output

### Successful Generation

```
╔════════════════════════════════════════════════════════════╗
║        Auto-UnitTest-Generator - AI Test Generation       ║
╚════════════════════════════════════════════════════════════╝

📋 Loading configuration...
✅ Configuration loaded successfully
   Provider: OpenAI
   Model: gpt-4o
   Framework: xUnit with Moq
   Target Coverage: 90%

📂 Validating input file...
✅ Source file loaded: ./src/Services/UserService.cs
   Lines: 150

🔍 Detecting solution context...
✅ Solution found: MyProject.sln

🤖 Initializing AI agents...
✅ Agents initialized

════════════════════════════════════════════════════════════

🎯 Iteration 1: Generating initial tests...
✅ Tests generated successfully
🔨 Building test project...
✅ Build successful
🧪 Running tests...
✅ All tests passed (12/12)
📊 Measuring coverage...
   Coverage: 87.5%

════════════════════════════════════════════════════════════

✅ SUCCESS!
   Test file: ./tests/UserServiceTests.cs
   Coverage: 87.5%
   Attempts: 1

📊 Iteration Log:
   Iteration 1: Generated initial tests (Coverage: 87.5%)
```

### With Refinement

```
🎯 Iteration 1: Generating initial tests...
   Coverage: 75.0%

🔍 Iteration 2: Reviewing and improving tests...
   Coverage: 85.0%

🔧 Iteration 3: Final refinements...
   Coverage: 91.0%

✅ SUCCESS!
   Test file: ./tests/UserServiceTests.cs
   Coverage: 91.0%
   Attempts: 3
```

## 🎓 Common Scenarios

### Scenario 1: Simple Service Class

**Source Code:**
```csharp
public class CalculatorService
{
    public int Add(int a, int b) => a + b;
    public int Subtract(int a, int b) => a - b;
    public int Multiply(int a, int b) => a * b;
    public int Divide(int a, int b) => a / b;
}
```

**Command:**
```bash
autounittest -f ./src/CalculatorService.cs -g ./tests/CalculatorServiceTests.cs
```

**Generated Tests Include:**
- Basic arithmetic operations
- Division by zero exception handling
- Edge cases (negative numbers, zero, max values)

### Scenario 2: Service with Dependencies

**Source Code:**
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

    public async Task<User> GetUserAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Invalid ID");
        
        return await _repository.GetByIdAsync(id);
    }
}
```

**Command:**
```bash
autounittest -f ./src/UserService.cs -g ./tests/UserServiceTests.cs --coverage 90
```

**Generated Tests Include:**
- Mock setup for dependencies
- Async/await patterns
- Exception scenarios
- Verify method calls on mocks

### Scenario 3: Complex Business Logic

**Source Code:**
```csharp
public class OrderService
{
    public decimal CalculateTotal(Order order)
    {
        var subtotal = order.Items.Sum(i => i.Price * i.Quantity);
        var discount = CalculateDiscount(order);
        var tax = (subtotal - discount) * 0.1m;
        return subtotal - discount + tax;
    }

    private decimal CalculateDiscount(Order order)
    {
        if (order.Items.Count >= 10) return 0.2m;
        if (order.Items.Count >= 5) return 0.1m;
        return 0m;
    }
}
```

**Command:**
```bash
autounittest -f ./src/OrderService.cs -g ./tests/OrderServiceTests.cs
```

**Generated Tests Include:**
- Multiple item scenarios
- Discount tier testing
- Tax calculation verification
- Edge cases (empty orders, single items)

## 🔧 Default Configuration

The tool uses these built-in defaults (can be overridden via command-line):
- **AI Provider**: OpenAI with GPT-4o model
- **Test Framework**: xUnit (primary support; NUnit and MSTest planned for future releases)
- **Mocking Library**: Moq (primary support; NSubstitute and FakeItEasy planned for future releases)
- **Assertion Library**: FluentAssertions
- **Target Coverage**: 90% (use `--coverage` to override)
- **Max Attempts**: 3 (use `--maxattempts` to override)

No configuration file is needed - just set your `OPENAI_API_KEY` environment variable and you're ready to go!

### When to Adjust Settings

**Increase Max Attempts** when:
- Working with complex business logic
- Need higher coverage (95%+)
- Initial attempts don't reach target coverage

**Adjust Coverage Target** when:
- Quick prototyping: use `--coverage 70`
- Production code: use `--coverage 95`
- Critical systems: use `--coverage 98` with `--maxattempts 5`

**Example for critical code:**
```bash
autounittest -f ./src/PaymentService.cs -g ./tests/PaymentServiceTests.cs --coverage 98 --maxattempts 5
```

## 🚨 Troubleshooting

### Issue: "API key not found"

**Solution:**
```bash
# Verify environment variable is set
echo %OPENAI_API_KEY%  # Windows Command Prompt
echo $env:OPENAI_API_KEY  # Windows PowerShell
echo $OPENAI_API_KEY  # Linux/Mac

# If not set, set it again and restart terminal
```

### Issue: "Build failed"

**Solution:**
1. Ensure test project exists and references source project
2. Check that all NuGet packages are restored
3. Verify the test framework packages are installed

```bash
cd tests
dotnet add package xunit
dotnet add package Moq
dotnet add package FluentAssertions
dotnet restore
```

### Issue: "Low coverage achieved"

**Solution:**
1. Check for unreachable code in source (the tool will warn you)
2. Review the generated tests and source code structure
3. Ensure all dependencies are properly mocked
4. The tool automatically retries up to 3 times to improve coverage

### Issue: "File not found"

**Solution:**
```bash
# Use absolute paths
autounittest -f "C:/Projects/MyApp/src/UserService.cs" -g "C:/Projects/MyApp/tests/UserServiceTests.cs"

# Or navigate to project directory first
cd C:/Projects/MyApp
autounittest -f ./src/UserService.cs -g ./tests/UserServiceTests.cs
```

## 📝 Best Practices

### 1. Start Small
Begin with simple classes to understand the tool's behavior.

### 2. Review Generated Tests
Always review and understand generated tests before committing.

### 3. Iterative Approach
Start with lower coverage targets and increase gradually.

### 4. Use Version Control
Commit generated tests to track changes over time.

### 5. Test Project Structure
Maintain a clear test project structure:
```
MyProject/
├── src/
│   └── Services/
│       └── UserService.cs
└── tests/
    └── Services/
        └── UserServiceTests.cs
```

## 🎯 Next Steps

1. **Explore Full Documentation**: Check out the complete [README.md](./README.md)
2. **Integrate into CI/CD**: Automate test generation in your pipeline
3. **Batch Processing**: Generate tests for multiple files in your project
4. **Code Quality**: Use the tool's quality analysis to improve source code

## 💡 Pro Tips

- **Review generated tests** before committing to understand coverage
- **Check code quality warnings** to identify unreachable code in your source
- **Use environment variables** for API keys (never hardcode them)
- **Start with simpler classes** to understand the tool's behavior
- **The tool uses GPT-4o** which provides high-quality, production-ready tests

## 📚 Additional Resources

- [Full Documentation](./README.md)
- [Configuration Reference](./README.md#configuration-options)
- [Troubleshooting Guide](./README.md#troubleshooting)
- [Example Project](./SandboxExample)

## 🎉 You're Ready!

You now have everything you need to start generating unit tests with AI. Happy testing!

---

**Need Help?** Open an issue on GitHub or check the full documentation.