# .NET JSON AOT
[![NuGet](https://img.shields.io/nuget/v/domportera.NetJsonAOT.svg)](https://www.nuget.org/packages/domportera.NetJsonAOT/)

This project is a source generator that allows you to easily use System.Text.JSON in your AOT projects.

This is in very early stages, but is working. Currently, the incremental generator does not work due to the evil things going on in here to make the native JSON source generators run before this one, however build-time source generation does work.

In order to reference this in your project, find it on [nuget](https://www.nuget.org/packages/domportera.NetJsonAOT/) and add the following tags to your csproj's package reference:

```xml
<ItemGroup>
    <PackageReference Include="domportera.NetJsonAOT" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>

```
In order to serialize an AOT class, decorate it with a `System.Serializable` attribute, like this:

```cs
[Serializable]
internal class MyClass
{
    public float MyGuy;
}
```

The source generator will seek out types tagged as Serializable to generate the proper classes.

This also creates a static NetJsonAOT class for your convenience.

```cs


using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using NetJsonAOT; // this is actually a reference to a namespace generated within your project - every project has its own internal NetJsonAot namespace which provdes the RuntimeJson static class

namespace ExampleSerialization;

void SomeMethod()
{
    // this is a dictionary of your type (key) to the generated JsonSerializerContext types
    var typeDictionary = RuntimeJson.JsonContextTypes;

    // this is a dictionary of your type (key) to actual instances of their respective JsonSerializerContext types
    var contexts = RuntimeJson.JsonContexts;


    // this is a dictionary of your type (key) to instances of JsonSerializerOptions, ready to be used in actual serialization & deserialization
    var options = RuntimeJson.JsonSerializerOptions;
}


internal static class Serialization
{
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    public static bool TryDeserialize<T>(string json, [NotNullWhen(true)] out T? result, [NotNullWhen(false)] out string? reason)
    {
        // this is a dictionary of your type (key) to instances of JsonSerializerOptions, ready to be used in actual serialization & deserialization
        if(!RuntimeJson.JsonSerializerOptions.TryGetValue(typeof(T), out var options))
        {
            result = default;
            reason  = $"Could not find JsonSerializerOptions for {typeof(T).Name}";
            return false;
        }

        try
        {
            result = JsonSerializer.Deserialize<T>(json, options);

            if (result == null)
            {
                reason = $"Could not convert {json} to {nameof(T)}";
                return false;
            }

            reason = null;
            return true;
        }
        catch (JsonException e)
        {
            result = default;
            reason = $"Could not convert json to {nameof(T)} - exception thrown: {e.Message}.\n'{json}'";
            return false;
        }
        catch (Exception e)
        {
            result = default;
            reason = $"Unexpected exception thrown: {e.Message}.\n'{json}'";
            return false;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    public static string Serialize<T>(T config)
    {
        // this is a dictionary of your type (key) to instances of JsonSerializerOptions, ready to be used in actual serialization & deserialization
        if(!RuntimeJson.JsonSerializerOptions.TryGetValue(typeof(T), out var options))
            throw new InvalidOperationException($"Could not find JsonSerializerOptions for {nameof(T)}");

        return JsonSerializer.Serialize(config, options);
    }
}
```
