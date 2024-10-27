using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetJsonAOT.Generators;

/// <summary>
/// A syntax receiver that listens for members with a specific attribute.
/// </summary>
/// <typeparam name="TAttribute"></typeparam>
/// <typeparam name="TSyntax"></typeparam>
internal class SyntaxReceiverByAttribute<TAttribute, TSyntax> : ISyntaxReceiver
    where TAttribute : Attribute
    where TSyntax : MemberDeclarationSyntax
{
    public static readonly string[] SerializableAttributes = GetAttributeNames<TAttribute>();
    public List<TSyntax> Collected { get; } = [];

    private readonly Type[]? _acceptableDeclarationTypes;
    private readonly bool _acceptSubtypes;

    /// <summary>
    /// Constructor for the <see cref="SyntaxReceiverByAttribute{TAttribute,TSyntax}"/> class.
    /// </summary>
    /// <param name="acceptSubTypes">
    /// Optional - if types are provided to this constructor, this bool governs whether
    /// this instance will accept derivative types of the provided types.
    /// For example, if you were to provide [<see cref="TypeDeclarationSyntax"/>], then type <see cref="StructDeclarationSyntax"/>
    /// would be accepted when true, and would be ignored when false
    /// </param>
    /// <param name="acceptableDeclarationTypes">
    /// Optional - The types to be accepted. If empty, any derivative type of the generic <see cref="TAttribute"/> type will be parsed.
    /// </param>
    /// <exception cref="ArgumentException">Thrown if any provided types are not compatible with the provided type <see cref="TAttribute"/></exception>
    public SyntaxReceiverByAttribute(bool acceptSubTypes = true,
        params Type[]? acceptableDeclarationTypes)
    {
        if (acceptableDeclarationTypes != null)
        {
            foreach (var type in acceptableDeclarationTypes)
            {
                if (typeof(TSyntax).IsAssignableFrom(type)) continue;
                
                var log = $"Type {type.FullName} is not assignable to {typeof(TSyntax).FullName}";
                Console.Error.WriteLine(log);
                throw new ArgumentException(log);
            }
        }

        _acceptableDeclarationTypes = acceptableDeclarationTypes;
        _acceptSubtypes = acceptSubTypes;
    }

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is not TSyntax syntax)
            return;

        if (!ShouldTransform(syntax, _acceptSubtypes, _acceptableDeclarationTypes)) 
            return;

        Collected.Add(syntax);
    }

    public static bool ShouldTransform(SyntaxNode syntax, bool acceptSubtypes = true, params Type[]? acceptableDeclarationTypes)
    {
        if (syntax is not MemberDeclarationSyntax memberSyntax)
            return false;
        
        if (acceptableDeclarationTypes != null)
        {
            var type = syntax.GetType();

            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (!acceptSubtypes && !acceptableDeclarationTypes.Contains(type))
                return false;

            if (acceptSubtypes && !acceptableDeclarationTypes.Any(t => t.IsAssignableFrom(type)))
                return false;
        }

        var shouldUse = false;

        foreach (var attributeListSyntax in memberSyntax.AttributeLists)
        {
            foreach (var attribute in attributeListSyntax.Attributes)
            {
                var name = attribute.Name.ToString();
                if (SerializableAttributes.Contains(name))
                {
                    shouldUse = true;
                    break;
                }
            }

            if (shouldUse)
                break;
        }

        return shouldUse;
    }

    private static string[] GetAttributeNames<T>() where T : Attribute
    {
        var t = typeof(T);
        var basicName = t.Name;
        var fullName = t.FullName!;

        const string attributeSuffix = "Attribute";
        if (!basicName.Contains(attributeSuffix))
            return [basicName, fullName];

        var suffixLength = attributeSuffix.Length;

        var basicNameWithoutAttributeSuffix = basicName[..^suffixLength];
        var fullNameWithoutAttributeSuffix = fullName[..^suffixLength];

        return [basicName, fullName, basicNameWithoutAttributeSuffix, fullNameWithoutAttributeSuffix];
    }
}