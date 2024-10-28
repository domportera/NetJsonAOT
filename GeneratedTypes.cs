namespace NetJsonAOT.Generators;

internal static class GeneratedTypes
{
    public static readonly List<GeneratedTypeInfo> GeneratedTypeInfos = [];
    public readonly record struct GeneratedTypeInfo(string OriginalFullyQualifiedTypeName, string GeneratedTypeName)
    {
        public readonly string OriginalFullyQualifiedTypeName = OriginalFullyQualifiedTypeName;
        public readonly string GeneratedTypeName = GeneratedTypeName;
    }
    public static void AddType(string originalFullyQualifiedTypeName, string generatedTypeName)
    {
        var info = new GeneratedTypeInfo(originalFullyQualifiedTypeName, generatedTypeName);
        GeneratedTypeInfos.Add(info);
    }
}