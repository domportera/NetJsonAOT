using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace NetJsonAOT.Generators;

internal static class ExtensionMethods
{
    public static bool Is<T>(this ISyntaxReceiver? syntaxReceiver, [NotNullWhen(true)] out T? receiver)
        where T : ISyntaxReceiver
    {
        if (syntaxReceiver is not T possibleReceiver)
        {
            Console.Error.WriteLine($"SyntaxReceiver is not the expected type: " +
                                    $"{syntaxReceiver?.GetType().ToString() ?? "null"}");
            receiver = default;
            return false;
        }

        receiver = possibleReceiver;
        return true;
    }
}