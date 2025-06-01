using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace NetJsonAOT.Generators;

// thank you to https://isadorasophia.com/articles/serialization/#6
// for this dastardly plan
internal static class Evil
{
    public static void RunJsonNetSourceGenerator(
        Action<string, SourceText> addSource, Compilation compilation,
        params SourceText[] sources)
    {
        ParseOptions options;
        if (compilation is CSharpCompilation { SyntaxTrees.Length: > 0 } csharpCompilation)
        {
            options = csharpCompilation.SyntaxTrees[0].Options;
        }
        else
        {
            options = CSharpParseOptions.Default.WithLanguageVersion(
                LanguageVersion.Latest);
        }

        // Add all sources to our compilation.
        foreach (var sourceText in sources)
        {
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(sourceText, options);
            compilation = compilation.AddSyntaxTrees(syntaxTree);
        }

        if (!TryGetJsonNetSourceGenerator(out var jsonGenerator))
            return;

        GeneratorDriver driver = CSharpGeneratorDriver.Create(jsonGenerator);
        driver = driver.RunGenerators(compilation);

        var driverResult = driver.GetRunResult();

        foreach (var result in driverResult.Results)
        {
            foreach (var source in result.GeneratedSources)
            {
                Console.WriteLine("--------------------");
                Console.WriteLine($"JSON GENERATED {source.HintName}\n{source.SourceText}\n");
                Console.WriteLine("--------------------\n\n");
                addSource("_" + source.HintName, source.SourceText);
            }
        }
    }

    private static bool TryGetJsonNetSourceGenerator([NotNullWhen(true)] out ISourceGenerator? jsonGenerator)
    {
        const string rootNamespace = "System.Text.Json";
        const string sourceGenerationNamespace = rootNamespace + ".SourceGeneration";
        const string sourceGeneratorFullTypeName = sourceGenerationNamespace + ".JsonSourceGenerator";

        var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(
            a => a.FullName.Contains(sourceGenerationNamespace));
        
        if(assembly is null)
        {
            Console.Error.WriteLine($"Unable to find {sourceGenerationNamespace} assembly");
            jsonGenerator = null;
            return false;
        }

        Type? textJsonForbiddenImporterType;
        try
        {
            textJsonForbiddenImporterType = assembly.GetType(sourceGeneratorFullTypeName);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Unable to find {sourceGeneratorFullTypeName} generator: {e}");
            jsonGenerator = null;
            return false;
        }
        
        if (textJsonForbiddenImporterType == null)
        {
            Console.Error.WriteLine($"Unable to find {sourceGeneratorFullTypeName} generator type");
            jsonGenerator = null;
            return false;
        }

        // See declaration of type at
        // https://github.com/dotnet/runtime/blob/c5bead63f8386f716b8ddd909c93086b3546efed/src/libraries/System.Text.Json/gen/JsonSourceGenerator.Roslyn4.0.cs
        jsonGenerator = (Activator.CreateInstance(textJsonForbiddenImporterType) as IIncrementalGenerator)?
            .AsSourceGenerator();
        
        return jsonGenerator != null;
    }
}