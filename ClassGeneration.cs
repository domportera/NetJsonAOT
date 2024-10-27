using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NetJsonAOT.Generators;

public static class ClassGeneration
{
    public static SyntaxTree GenerateJsonSerializable(string fullNamespace, string className, SyntaxKind accessModifier)
    {
        // todo - runtime types and using statements
        // function to be called "CreateClassWithAttributes or something like that
        var usingDirectives = new[]
        {
            SF.UsingDirective(SF.ParseName("System")), SF.UsingDirective(SF.ParseName("System.Text.Json")),
            SF.UsingDirective(SF.ParseName("System.Text.Json.Serialization"))
        };

        var namespaceLastPart = $"{className}.JsonGen";
        var generatedNamespace = string.IsNullOrWhiteSpace(fullNamespace) 
            ? namespaceLastPart 
            : $"{fullNamespace}.{namespaceLastPart}";
        
        var tree = SF.SyntaxTree(
            root: SF.CompilationUnit()
                .WithUsings(SF.List(usingDirectives))
                
                // add namespace of original type
                .WithMembers(SF.SingletonList<MemberDeclarationSyntax>(
                    SF.NamespaceDeclaration(SF.ParseName(generatedNamespace)))
                )
                .WithMembers(SF.SingletonList<MemberDeclarationSyntax>(
                        CreateClassDeclaration(fullNamespace, className, accessModifier)
                    )
                )
                .NormalizeWhitespace(),
            encoding: Encoding.UTF8
        );

        return tree;

        static ClassDeclarationSyntax CreateClassDeclaration(string fullNamespace, string className, SyntaxKind scope)
        {
            switch (scope)
            {
                case SyntaxKind.InternalKeyword:
                    break;
                case SyntaxKind.PublicKeyword:
                    break;
                case SyntaxKind.ProtectedKeyword:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
            }
/*
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(Person))]
    internal partial class PersonJsonContext : JsonSerializerContext
    {
    }
*/

            var jsonContextClassName = className + "JsonContext";
            var fullyQualifiedClassName = fullNamespace + "." + className;
            
            GeneratedTypes.AddType(fullyQualifiedClassName, jsonContextClassName);

            const string jsonContextName = "System.Text.Json.Serialization.JsonSerializerContext";

            //todo - add serialization attribute for field types 
            return SF.ClassDeclaration(jsonContextClassName)
                .WithAttributeLists(SF.List(
                        [
                            SF.AttributeList(SF.SingletonSeparatedList(SF
                                .Attribute(SF.IdentifierName("JsonSourceGenerationOptions"))
                                .WithArgumentList(SF.AttributeArgumentList(SF.SeparatedList<AttributeArgumentSyntax>(
                                    new SyntaxNodeOrToken[]
                                    {
                                        SF.AttributeArgument(SF.LiteralExpression(SyntaxKind.TrueLiteralExpression))
                                            .WithNameEquals(SF.NameEquals(SF.IdentifierName("WriteIndented"))),
                                        SF.Token(SyntaxKind.CommaToken), SF.AttributeArgument(SF.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SF.IdentifierName("JsonSourceGenerationMode"),
                                                SF.IdentifierName("Default")))
                                            .WithNameEquals(SF.NameEquals(SF.IdentifierName("GenerationMode"))),
                                        SF.Token(SyntaxKind.CommaToken), SF.AttributeArgument(SF.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SF.IdentifierName("JsonCommentHandling"), SF.IdentifierName("Skip")))
                                            .WithNameEquals(SF.NameEquals("ReadCommentHandling")),
                                        SF.Token(SyntaxKind.CommaToken), SF
                                            .AttributeArgument(SF.LiteralExpression(SyntaxKind.TrueLiteralExpression))
                                            .WithNameEquals(SF.NameEquals("IgnoreReadOnlyFields"))
                                    }))))),
                            SF.AttributeList(SF.SingletonSeparatedList(
                                GenerateAttributeWithTypeArgument(fullyQualifiedClassName)))
                        ]
                    )
                )
                .WithModifiers(
                    SF.TokenList([SF.Token(scope), SF.Token(SyntaxKind.PartialKeyword)])
                )
                .WithBaseList(
                    SF.BaseList(
                        SF.SingletonSeparatedList<BaseTypeSyntax>(
                            SF.SimpleBaseType(
                                SF.IdentifierName(jsonContextName)
                            )
                        )
                    )
                );
        }
    }

    private static AttributeSyntax GenerateAttributeWithTypeArgument(string fullyQualifiedTypeName)
    {
        return SF.Attribute(SF.IdentifierName("JsonSerializable"))
            .WithArgumentList(SF.AttributeArgumentList(
                SF.SingletonSeparatedList(
                    SF.AttributeArgument(SF.TypeOfExpression(SF.IdentifierName(fullyQualifiedTypeName))))));
    }
}