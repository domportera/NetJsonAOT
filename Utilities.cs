using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJsonAOT.Generators;

public static class Utilities
{
    public static readonly Type[] NonInterfaceDeclarationTypes =
    {
        typeof(ClassDeclarationSyntax),
        typeof(StructDeclarationSyntax),
        typeof(RecordDeclarationSyntax)
    };
    
    public static bool TryGetNamespace(ISymbol symbol, List<INamespaceSymbol> namespaces, StringBuilder sb,
        [NotNullWhen(true)] out string? namespaceString)
    {
        var nsp = symbol.ContainingNamespace;
        
        while(nsp is {IsGlobalNamespace: false})
        {
            namespaces.Add(nsp);
            nsp = nsp.ContainingNamespace;
        }

        if (namespaces.Count == 0)
        {
            namespaceString = null;
            return false;
        }

        for (int i = namespaces.Count - 1; i >= 0; i--)
        {
            sb.Append(namespaces[i].Name).Append('.');
            namespaces.RemoveAt(i);
        }


        namespaceString = sb.ToString(0, sb.Length - 1);
        sb.Clear();
        return true;
    }

    public static SyntaxKind GetScope(TypeDeclarationSyntax syntax)
    {
        // get private/public/protected/internal
        var modifiers = syntax.Modifiers;
        
        foreach (var modifier in modifiers)
        {
            var kind = modifier.Kind() switch
            {
                SyntaxKind.PrivateKeyword => SyntaxKind.InternalKeyword,
                SyntaxKind.ProtectedKeyword => SyntaxKind.ProtectedKeyword,
                SyntaxKind.PublicKeyword => SyntaxKind.PublicKeyword,
                SyntaxKind.InternalKeyword => SyntaxKind.InternalKeyword,
                _ => SyntaxKind.None
            };
            
            if (kind != SyntaxKind.None)
                return kind;
        }

        return SyntaxKind.InternalKeyword;
    }
}