using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace NetJsonAOT.Generators;

internal partial class JsonSerializerGenerator
{
    // This is a modified version of the output of https://roslynquoter.azurewebsites.net/

    private static bool TryCompileSyntaxTree(SyntaxTree tree)
    {
        var refApis = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Select(a => MetadataReference.CreateFromFile(a.Location));

        var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var compilation = CSharpCompilation.Create("TestBuild", [tree], refApis, compilationOptions);

        Console.ForegroundColor = ConsoleColor.Red;
        var errorCount = 0;
        foreach (var d in compilation.GetDiagnostics())
        {
            if (d is null)
                continue;

            if (d.Severity != DiagnosticSeverity.Error)
                continue;

            Console.Error.WriteLine(d);
            ++errorCount;
        }

        Console.ResetColor();

        return errorCount == 0;
    }
}