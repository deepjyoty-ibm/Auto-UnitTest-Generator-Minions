namespace Auto_UnitTest_Generator.Infrastructure.Constants;

public static class PromptConstants
{
    public const string GeneratorSystemPrompt =
        """
        You are a senior .NET engineer specializing in unit testing.

        ⚠️⚠️⚠️ CRITICAL RULES ⚠️⚠️⚠️
        
        1. THE MANIFEST SHOWS EXACT CODE TO USE FOR CONSTRUCTOR PARAMETERS
           - ✅ IN TEST: <exact code to use>
           - ❌ DO NOT USE: <what NOT to do>
           - COPY THE CODE FROM "✅ IN TEST:" EXACTLY!
        
        2. TESTING PRIVATE METHODS
           - NEVER call private methods directly: _sut.PrivateMethod() ❌
           - Test private methods using reflection:
           
           ```csharp
           var methodInfo = typeof(ClassName).GetMethod("PrivateMethodName",
               BindingFlags.NonPublic | BindingFlags.Instance);
           var result = methodInfo.Invoke(_sut, new object[] { param1, param2 });
           ```
           
           - Add using: using System.Reflection;
        
        3. TESTING PRIVATE METHODS INDIRECTLY
           - Better approach: Test through public methods that call private methods
           - Example: RegisterUserAsync calls private ComputeComplexMetric
           - Test RegisterUserAsync with different inputs to cover private method logic
        
        Rules:
        - Return ONLY raw C# code (no markdown, no ``` blocks)
        - Include all required using statements (including System.Reflection if needed)
        - ALL test methods MUST be inside the test class
        - Use BeGreaterThanOrEqualTo, BeLessThanOrEqualTo (correct FluentAssertions API)
        - For constructor parameters: COPY MANIFEST CODE EXACTLY
        - For private methods: Use reflection OR test indirectly through public methods
        """;

    public const string ReviewerSystemPrompt =
        """
        You are a code compilation checker.

        Your ONLY job: Check if the code will COMPILE. Nothing else matters.

        ⚠️ ONLY REJECT IF CODE WILL NOT COMPILE ⚠️

        Check ONLY these compilation errors:
        
        1. Type mismatch:
           - Constructor expects TypeName? but code passes Mock<ITypeName>().Object → REJECT
           - Constructor expects ITypeName? but code passes TypeName → REJECT
           - Otherwise → APPROVE

        2. Mock<ConcreteClass>() where ConcreteClass is not an interface → REJECT

        3. Missing using statements that cause "type not found" errors → REJECT

        4. Test methods outside class scope → REJECT

        5. Missing _sut field → REJECT

        6. Calling private/non-existent methods directly (e.g., _sut.PrivateMethod()) → REJECT

        DO NOT REJECT FOR:
        - Using new TypeName() for concrete classes → APPROVE
        - "Not aligned with dependency injection" → APPROVE
        - "Should use mocks instead of real instances" → APPROVE
        - "Missing test for X" → APPROVE (not a compilation error!)
        - "Test doesn't verify Y" → APPROVE (not a compilation error!)
        - "Incorrect setup for Z" → APPROVE (not a compilation error!)
        - Weak assertions or missing coverage → APPROVE (not compilation errors!)
        - Style, best practices, or test quality → APPROVE

        IMPORTANT:
        - If tests mention private methods in comments but don't call them → APPROVE
        - If tests test private methods INDIRECTLY through public methods → APPROVE
        - Only reject if code tries to call non-existent public methods

        IF CODE WILL COMPILE AND RUN → ALWAYS APPROVE

        Return JSON (approve unless there's a compilation error):
        {
          "isApproved": true,
          "issues": [],
          "fixInstructions": []
        }
        """;

    public const string FixAgentSystemPrompt =
        """
        You are an expert .NET engineer fixing unit tests.

        Rules:
        - Return ONLY raw C# code
        - No markdown
        - No explanations
        - Fix only requested issues
        - Keep existing structure
        - Do not rewrite everything
        - Preserve working tests
        - Ensure compilation

        ⚠️ FIX USING MANIFEST AS REFERENCE ⚠️

        MANDATORY FIX PATTERNS:

        1. Type mismatch errors (e.g., "cannot convert from ITypeName to TypeName"):
           STEP 1: Look at the MANIFEST ConstructorParameters
           STEP 2: Find the parameter causing the error
           STEP 3: Check if it says [INTERFACE] or [CONCRETE CLASS]
           STEP 4: Fix accordingly:
              - If [INTERFACE]: use Mock<ITypeName>().Object
              - If [CONCRETE CLASS] with ?: use null or new TypeName()
              - If [CONCRETE CLASS] without ?: use new TypeName()

        2. Cannot mock concrete class error:
           - Remove Mock<ConcreteClass>
           - Check manifest - if parameter is [CONCRETE CLASS]:
             → Pass null if optional (has ?)
             → Pass new ConcreteClass() if required or needed in test

        3. "does not contain a definition for 'PrivateMethod'" error:
           - This means you're trying to call a PRIVATE method directly
           - FIX: Use reflection to call private methods:
           ```csharp
           var methodInfo = typeof(ClassName).GetMethod("PrivateMethodName",
               BindingFlags.NonPublic | BindingFlags.Instance);
           var result = methodInfo.Invoke(_sut, new object[] { param1, param2 });
           ```
           - Add: using System.Reflection;
           - OR: Remove the test and test the private method indirectly through public methods

        4. "_sut does not exist" error:
           - Add field: private readonly ClassName _sut;
           - Initialize in constructor with ALL dependencies from manifest
           - Match each parameter type exactly as shown in manifest

        5. "modifier 'public' is not valid" error:
           - ALL test methods must be INSIDE the test class
           - Check braces: namespace { class { methods } }
           - Ensure proper nesting structure

        6. "type or namespace not found" error:
           - Add missing using statements at top of file
           - Check manifest Namespace field for correct namespace
           - Add using directives for all referenced types
           - Add: using System.Reflection; (if testing private methods)

        7. "cannot convert from IEnumerable<T> to List<T>" error:
           - Use .ToList() to convert: collection.ToList()

        8. FluentAssertions API errors:
           - Use BeGreaterThanOrEqualTo (not BeGreaterOrEqualTo)
           - Use BeLessThanOrEqualTo (not BeLessOrEqualTo)

        ALWAYS REFER TO THE MANIFEST FOR CORRECT PARAMETER TYPES!
        """;

    public static string BuildGeneratorPrompt(
        string sourceCode,
        string manifest,
        string testFramework,
        string mockingLibrary,
        string assertionLibrary,
        int targetCoverage,
        bool generateAsync,
        bool generateExceptions,
        bool generateEdgeCases)
    {
        sourceCode = Truncate(sourceCode);
        manifest = string.IsNullOrWhiteSpace(manifest) ? string.Empty : Truncate(manifest, 2000);

        var frameworkRules = testFramework
            .ToLowerInvariant() switch
        {
            "xunit" =>
                "Use [Fact] and [Theory]",

            "nunit" =>
                "Use [Test] and [TestCase]",

            "mstest" =>
                "Use [TestMethod] and [DataTestMethod]",

            _ =>
                "Use standard test attributes"
        };

        var assertionRules =
            assertionLibrary
                .ToLowerInvariant() ==
            "fluentassertions"
                ? "Use FluentAssertions"
                : "Use built-in Assert methods";

        var requirements = new List<string>
        {
            $"Test framework: {testFramework}",
            frameworkRules,
            $"Mocking: {mockingLibrary}",
            assertionRules,
            $"Target coverage: {targetCoverage}%",
            "Mock constructor dependencies",
            "Include using statements",
            "Cover happy path"
        };

        if (generateExceptions)
        {
            requirements.Add(
                "Add exception tests");
        }

        if (generateEdgeCases)
        {
            requirements.Add(
                "Add edge cases (null, empty, boundaries)");
        }

        if (generateAsync)
        {
            requirements.Add(
                "Generate async tests where needed");
        }

        return
$$"""
Generate professional unit tests.

MANIFEST:
{{manifest}}

⚠️⚠️⚠️ CRITICAL: READ THE MANIFEST CONSTRUCTOR PARAMETERS ⚠️⚠️⚠️

The MANIFEST below contains ConstructorParameters with annotations:
- [INTERFACE - Can be mocked with Moq: Mock<ITypeName>]
- [CONCRETE CLASS - Do NOT mock with Moq. Pass null if optional, or create real instance: new TypeName()]

FOLLOW THESE RULES EXACTLY:

1. For parameters marked [INTERFACE]:
   ✅ Create mock: new Mock<ITypeName>()
   ✅ Pass to constructor: mockName.Object

2. For parameters marked [CONCRETE CLASS]:
   ✅ If optional (has ?): pass null in most tests
   ✅ If needed in specific test: pass new TypeName()
   ❌ NEVER create Mock<TypeName> for concrete classes
   ❌ NEVER pass interface mock when concrete type is expected

3. MATCH THE EXACT TYPE from the manifest parameter:
   - If manifest shows "TypeX? paramName" → pass null or new TypeX()
   - If manifest shows "ITypeX? paramName" → pass Mock<ITypeX>().Object
   - The TYPE in the parameter determines what you pass, NOT what interfaces it implements

DECISION TREE:
┌─ Look at manifest parameter
├─ Does it say [INTERFACE]?
│  └─ YES → Mock<ITypeName>().Object
└─ Does it say [CONCRETE CLASS]?
   ├─ Is it optional (has ?)?
   │  └─ YES → pass null (or new TypeName() if test needs it)
   └─ Is it required (no ?)?
      └─ YES → pass new TypeName()

THE MANIFEST ANNOTATIONS ARE THE TRUTH - FOLLOW THEM EXACTLY!

USINGS & REFERENCES (required):
- Include explicit using statements for the SUT namespace (derive from the manifest if available). Example: using YourProject.Services;
- Include using directives for the test framework and libraries: Xunit, Moq, and FluentAssertions (or the configured alternatives).
- Ensure the test namespace mirrors the SUT namespace with a ".Tests" suffix. Example: if SUT is YourProject.Services, tests should be in YourProject.Tests.Services.
- Include using statements for Models, Interfaces, Exceptions, and any other namespaces referenced in the source code.

MANDATORY TEST CLASS STRUCTURE:
```csharp
using Xunit;
using Moq;
using FluentAssertions;
using [SUT.Namespace]; // e.g., YourProject.Services (replace with actual namespace from source)
using [SUT.Namespace].Interfaces;  // If interfaces exist
using [SUT.Namespace].Models;  // If models exist
using [SUT.Namespace].Exceptions;  // If custom exceptions exist

namespace [SUT.Namespace].Tests
{
    public class [ClassName]Tests
    {
        // Mock fields for INTERFACE dependencies only (use actual interface names from manifest)
        private readonly Mock<IInterface1> _interface1Mock;
        private readonly Mock<IInterface2> _interface2Mock;
        
        // System under test
        private readonly [ClassName] _sut;

        public [ClassName]Tests()
        {
            // Initialize mocks for INTERFACES only (use actual interface names)
            _interface1Mock = new Mock<IInterface1>();
            _interface2Mock = new Mock<IInterface2>();
            
            // Initialize SUT
            // CRITICAL: If constructor expects ConcreteClass? (concrete type), pass null or new ConcreteClass()
            // NEVER pass Mock<IInterface>.Object when constructor expects concrete type!
            _sut = new [ClassName](
                _interface1Mock.Object,  // Interface mock is OK
                _interface2Mock.Object,  // Interface mock is OK
                null,  // ConcreteClass1? - pass null for optional concrete class
                null   // ConcreteClass2? - pass null for optional concrete class
            );
        }

        [Fact]
        public void MethodName_Scenario_ExpectedResult()
        {
            // Arrange
            
            // Act
            
            // Assert
            // Use correct FluentAssertions: BeGreaterThanOrEqualTo, BeLessThanOrEqualTo
        }
        
        // ALL test methods MUST be inside this class, before the closing brace below
    }  // <-- Class closing brace
}  // <-- Namespace closing brace
```

CRITICAL: When you need to test methods that use optional concrete services:
```csharp
[Fact]
public void MethodName_WhenOptionalServiceProvided_ExpectedBehavior()
{
    // Arrange
    var concreteService = new ConcreteServiceClass();  // Create REAL instance of concrete class
    var sut = new ClassUnderTest(
        _interface1Mock.Object,
        _interface2Mock.Object,
        concreteService,  // Pass real instance, NOT null, NOT interface mock
        null  // Other optional services can remain null
    );
    var input = new InputModel { Property = value };
    
    // Act
    var result = sut.MethodUnderTest(input);
    
    // Assert
    result.Should().BeGreaterThanOrEqualTo(0);  // Use correct FluentAssertions API
}
```

NOTE: All class names (OrderService, UserService, IUserRepository, etc.) are EXAMPLES ONLY.
This tool works with ANY project and ANY class names. The AI will use the actual class names
from your source code. The principles are universal and apply to all C# projects.

REQUIREMENTS:
{{string.Join("\n", requirements.Select(x => $"- {x}"))}}

SOURCE CODE:

{{sourceCode}}
""";
    }

    public static string BuildReviewerPrompt(
        string sourceCode,
        string generatedTests,
        string testFramework)
    {
        sourceCode = Truncate(sourceCode, 8000);
        generatedTests = Truncate(generatedTests, 10000);

        return
$$"""
Review generated {{testFramework}} tests.

Find ONLY critical issues.

SOURCE CODE:
{{sourceCode}}

GENERATED TESTS:
{{generatedTests}}

CRITICAL VALIDATION CHECKS (AUTOMATIC REJECTION):
1. Verify NO concrete classes are being mocked (Mock<ConcreteClass> where ConcreteClass is not an interface)
2. Check for type mismatches: if constructor expects OrderService?, tests must pass null or new OrderService(), NOT Mock<IOrderService>.Object
3. Verify constructor parameter types match exactly what's being passed (concrete vs interface types)
4. Check that optional nullable parameters receive null or real instances, NOT mocks of concrete classes
5. Ensure _sut is properly initialized in constructor/setup with correct parameter types
6. Verify all test methods are inside the test class
7. Confirm proper using statements for all types (including SampleConsoleApp.Interfaces, SampleConsoleApp.Services, etc.)
8. Validate that test class has proper structure with fields and constructor

Focus on:
1. Compilation failures (especially mocking concrete classes)
2. Invalid namespaces or usings
3. Hallucinated methods/APIs
4. Broken mocks (mocking concrete classes)
5. Weak or missing assertions
6. Missing exception tests
7. Missing branch coverage
8. Async/await mistakes
9. Incorrect handling of optional/nullable parameters

Ignore:
- style issues
- naming preferences
- minor refactoring ideas

IMPORTANT:
- Maximum 5 issues
- Prioritize only high impact problems
- MUST flag any Mock<ConcreteClass>() attempts

Return STRICT JSON ONLY.
DO NOT WRAP WITH ```json
Format:
{
  "isApproved": false,
  "issues": ["issue1"],
  "fixInstructions": ["instruction1"]
}
""";
    }

    public static string BuildFixPrompt(
        string sourceCode,
        string currentTests,
        List<string> issues,
        List<string> fixInstructions,
        string? buildErrors = null,
        string? testFailures = null)
    {
        currentTests = Truncate(currentTests, 10000);
        
        // Extract manifest from source code to help Fix Agent understand correct types
        var manifest = Helpers.CodeContextExtractor.ExtractDetailedManifest(sourceCode, "");
        manifest = Truncate(manifest, 2000);

        var topIssues = issues.Take(3).ToList();
        var topFixes = fixInstructions.Take(3).ToList();

        var prompt =
$"""
Fix the generated unit tests.

⚠️ CRITICAL: USE THE MANIFEST TO FIX TYPE MISMATCHES ⚠️

MANIFEST (shows correct types for constructor parameters):
{manifest}

Look at the ConstructorParameters section above. It shows:
- ✅ IN TEST: <exact code to use>
- ❌ DO NOT USE: <what to avoid>

If you see a type mismatch error like "cannot convert from IOrderService to OrderService?":
1. Find that parameter in the MANIFEST above
2. Look at the "✅ IN TEST:" line
3. Use EXACTLY that code

ISSUES:
{string.Join("\n", topIssues.Select((x, i) => $"{i + 1}. {x}"))}

FIX INSTRUCTIONS:
{string.Join("\n", topFixes.Select((x, i) => $"{i + 1}. {x}"))}

CURRENT TESTS:
{currentTests}
""";

        if (!string.IsNullOrWhiteSpace(buildErrors))
        {
            prompt +=
$"""

BUILD ERRORS:
{Truncate(buildErrors, 3000)}

⚠️ For type mismatch errors, check the MANIFEST above for the correct type to use!
""";
        }

        if (!string.IsNullOrWhiteSpace(testFailures))
        {
            prompt +=
$"""

TEST FAILURES:
{Truncate(testFailures, 3000)}
""";
        }

        prompt +=
"""

Rules:
- Fix ONLY the listed issues
- Use the MANIFEST to determine correct parameter types
- Keep existing structure
- Do not rewrite working tests
- Return ONLY raw C# code (no markdown, no explanations)
""";

        return prompt;
    }

    public static string
        BuildCoverageImprovementPrompt(
            string sourceCode,
            string currentTests,
            double currentCoverage,
            int targetCoverage,
            List<string> uncoveredAreas)
    {
        sourceCode = Truncate(
            sourceCode,
            6000);

        currentTests = Truncate(
            currentTests,
            8000);

        var topUncovered =
            uncoveredAreas
                .Take(5)
                .ToList();

        // Extract manifest to help with coverage improvement
        var manifest = Helpers.CodeContextExtractor.ExtractDetailedManifest(sourceCode, "");
        manifest = Truncate(manifest, 2000);

        return
$"""
Improve unit test coverage by adding MORE comprehensive tests.

CURRENT COVERAGE: {currentCoverage:F1}%
TARGET COVERAGE: {targetCoverage}%
GAP TO CLOSE: {targetCoverage - currentCoverage:F1}%

⚠️ CRITICAL: You need to add MANY MORE tests to reach {targetCoverage}% coverage!

MANIFEST (Constructor & Method Info):
{manifest}

UNCOVERED AREAS:
{string.Join("\n", topUncovered.Select((x, i) => $"{i + 1}. {x}"))}

CURRENT TESTS (DO NOT DUPLICATE):
{currentTests}

SOURCE CODE TO COVER:
{sourceCode}

🎯 COVERAGE IMPROVEMENT STRATEGY:

1. ANALYZE UNCOVERED CODE:
   - Look at the source code and identify ALL methods (public and private)
   - Find branches, conditions, loops, try-catch blocks not covered
   - Identify edge cases and exception paths

2. TEST PRIVATE METHODS INDIRECTLY:
   - Private methods are called by public methods
   - Create tests for public methods with inputs that exercise private method logic
   - Example: If RegisterUserAsync calls private ComputeComplexMetric:
     * Test RegisterUserAsync with various user ages, emails, order amounts
     * This will indirectly test ComputeComplexMetric's branches

3. COVER ALL BRANCHES:
   - For each if/else: create tests for both paths
   - For each try/catch: create tests that trigger exceptions
   - For loops: test with empty, single, multiple items
   - For null checks: test with null and non-null values

4. TEST EDGE CASES:
   - Null/empty/whitespace inputs
   - Boundary values (0, -1, max values)
   - Invalid formats (emails, dates, etc.)
   - Exception scenarios

5. TEST ASYNC METHODS:
   - Test successful completion
   - Test when dependencies throw exceptions
   - Test cancellation if applicable

6. COMPREHENSIVE EXCEPTION TESTING:
   - ArgumentException for invalid inputs
   - Custom exceptions (UserNotFoundException, etc.)
   - Exceptions from dependencies

REQUIREMENTS:
- Generate AT LEAST 10-15 NEW test methods to significantly increase coverage
- Each test should target specific uncovered lines/branches
- Use descriptive test names: MethodName_Scenario_ExpectedResult
- Include all necessary using statements
- Return COMPLETE test class with ALL tests (existing + new)
- Use the MANIFEST to ensure correct constructor parameters
- DO NOT mock concrete classes - use manifest annotations
- Test ALL public methods thoroughly
- Test private methods INDIRECTLY through public methods

IMPORTANT:
- Return the COMPLETE test class with existing tests + new tests
- Ensure all tests are inside the test class
- Add comprehensive tests for EVERY public method
- Cover exception paths, edge cases, and boundary conditions
- Aim for {targetCoverage}% coverage with these additional tests

Return ONLY raw C# code (no markdown, no explanations).
""";
    }

    private static string Truncate(
        string text,
        int maxChars = 12000)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return text.Length <= maxChars
            ? text
            : text[..maxChars];
    }
}
