using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJsonAOT.Generators;

//[Generator]
internal class JsonIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.CreateSyntaxProvider(
            ShouldTransform<SerializableAttribute, TypeDeclarationSyntax>,
            Transform<TypeDeclarationSyntax>);

        IncrementalValuesProvider<(SymbolWithSyntax symbolWithSyntax, Compilation compilation)> symbolsWithCompilation
            = provider.Combine(context.CompilationProvider);

        context.RegisterSourceOutput(symbolsWithCompilation, OnSyntaxOutput);
    }


    private static void OnSyntaxOutput(SourceProductionContext context,
        (SymbolWithSyntax symbolWithSyntax, Compilation compilation) input)
    {
        Console.WriteLine("OnSyntaxOutput");
        var compilation = input.compilation;
        var symbolWithSyntax = input.symbolWithSyntax;
        var symbol = symbolWithSyntax.Symbol;
        if (symbol == null)
        {
            Console.Error.WriteLine($"Symbol not found for {symbolWithSyntax.Syntax.Kind()} at {symbolWithSyntax.Syntax.GetLocation()}");
            return;
        }

        List<INamespaceSymbol> namespaces = [];
        StringBuilder sb = new(40);

        if (!Utilities.TryGetNamespace(symbol, namespaces, sb, out var namespaceString))
        {
            Console.Error.WriteLine($"Failed to get namespace for {symbol}");
            return;
        }

        var accessModifier = Utilities.GetScope(symbolWithSyntax.Syntax);
        var tree = ClassGeneration.GenerateJsonSerializable(namespaceString, symbol.Name, accessModifier);

        if (!tree.TryGetText(out var text))
        {
            text = tree.GetText();
        }

        Console.WriteLine("--------------------");
        Console.WriteLine($"Generated code:\n{text}\n");
        Console.WriteLine("--------------------\n\n");
        try
        {
            Evil.RunJsonNetSourceGenerator(context.AddSource, compilation, text);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to generate source: {e}");
        }
    }

    private static bool ShouldTransform<TAttribute, TSyntax>(SyntaxNode syntaxNode, CancellationToken token)
        where TAttribute : Attribute
        where TSyntax : MemberDeclarationSyntax
    {
        return SyntaxReceiverByAttribute<TAttribute, TSyntax>.ShouldTransform(syntaxNode, true,
            Utilities.NonInterfaceDeclarationTypes);
    }

    private static SymbolWithSyntax Transform<TSyntax>(GeneratorSyntaxContext context, CancellationToken token)
        where TSyntax : TypeDeclarationSyntax
    {
        var syntax = (TSyntax)context.Node;
        var symbolInfo = context.SemanticModel.GetDeclaredSymbol(syntax!, token);
        Console.WriteLine($"Transformed {symbolInfo?.Name} at {syntax.GetLocation()}");
        return new SymbolWithSyntax(symbolInfo, syntax);
    }

    private readonly struct SymbolWithSyntax(ISymbol? symbol, TypeDeclarationSyntax syntax)
    {
        public readonly ISymbol? Symbol = symbol;
        public readonly TypeDeclarationSyntax Syntax = syntax;
    }
}