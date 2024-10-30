using System.Text;
using Microsoft.CodeAnalysis;
using SyntaxReceiver = NetJsonAOT.Generators.SyntaxReceiverByAttribute
    <System.SerializableAttribute, Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>;

namespace NetJsonAOT.Generators;

[Generator]
internal partial class JsonSerializerGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(receiverCreator: () =>
            new SyntaxReceiver(
                acceptSubTypes: true,
                acceptableDeclarationTypes: Utilities.NonInterfaceDeclarationTypes
            )
        );
    }

    public void Execute(GeneratorExecutionContext context)
    {
        Console.WriteLine("Execute JsonSerializerGenerator");
        var syntaxReceiver = context.SyntaxReceiver;
        if (!syntaxReceiver.Is(out SyntaxReceiver? receiver))
            return;

        List<INamespaceSymbol> namespaces = [];
        StringBuilder sb = new(80);
        foreach (var syntax in receiver.Collected)
        {
            var semanticModel = context.Compilation.GetSemanticModel(syntax.SyntaxTree);
            var symbol = semanticModel.GetDeclaredSymbol(syntax);
            if (symbol == null)
            {
                Console.Error.WriteLine($"Symbol not found for {syntax.Kind()} at {syntax.GetLocation()}");
                continue;
            }

            if (!Utilities.TryGetNamespace(symbol, namespaces, sb, out var namespaceString))
                continue;

            var accessModifier = Utilities.GetScope(syntax);
            var tree = ClassGeneration.GenerateJsonSerializable(namespaceString, symbol.Name, accessModifier);

            if (!tree.TryGetText(out var text))
            {
                text = tree.GetText();
            }

            Console.WriteLine("--------------------");
            Console.WriteLine($"Generated code:\n{text}\n");
            Console.WriteLine("--------------------\n\n");

            var fullyQualifiedClassName = namespaceString + "." + symbol.Name;
            var code = tree.GetText();
            context.AddSource(fullyQualifiedClassName + ".Generated.cs", code);

            Evil.RunJsonNetSourceGenerator(context.AddSource, context.Compilation, code);
        }

        // generate dictionary of jsonContextTypes
        var codeBuilder = new StringBuilder();
        codeBuilder.AppendLine("""
                               using System;
                               using System.Collections.Generic;
                               using System.Text.Json;
                               using System.Text.Json.Serialization;

                               namespace NetJsonAOT 
                               {
                                   internal static class RuntimeJson 
                                   {
                                       public static IReadOnlyDictionary<Type, Type> JsonContextTypes = new Dictionary<Type, Type>() 
                                       {
                               """);
        foreach (var item in GeneratedTypes.GeneratedTypeInfos)
        {
            codeBuilder.AppendLine(
                $"\t\t\t{{typeof({item.OriginalFullyQualifiedTypeName}), typeof({item.GeneratedTypeName})}},");
        }

        codeBuilder.AppendLine("}; // end dictionary");
        codeBuilder.AppendLine("""

                               public static IReadOnlyDictionary<Type, JsonSerializerContext> JsonContexts = new Dictionary<Type, JsonSerializerContext>() 
                               {
                               """);

        foreach (var item in GeneratedTypes.GeneratedTypeInfos)
        {
            codeBuilder.AppendLine(
                $"\t\t\t{{typeof({item.OriginalFullyQualifiedTypeName}), new {item.GeneratedTypeName}()}},");
        }

        codeBuilder.AppendLine("}; // end dictionary");

        codeBuilder.AppendLine("""
                               public static IReadOnlyDictionary<Type, JsonSerializerOptions> JsonSerializerOptions;

                               static RuntimeJson()
                               {
                                   var optionsDict = new Dictionary<Type, JsonSerializerOptions>();
                                   foreach (var item in JsonContexts)
                                   {
                                        var options = new JsonSerializerOptions{
                                            TypeInfoResolver = item.Value,
                                            IncludeFields = true
                                        };
                                       
                                       optionsDict.Add(item.Key, options);
                                   }
                                   
                                   JsonSerializerOptions = optionsDict;
                               }
                               """);


        codeBuilder.AppendLine("""
                               } // end class
                               } // end namespace
                               """);
        var dictCode = codeBuilder.ToString();
        context.AddSource("JsonContexts.Generated.cs", dictCode);
        GeneratedTypes.GeneratedTypeInfos.Clear();
    }
}