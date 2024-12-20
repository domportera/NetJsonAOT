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
using NetJsonAOT; // this is actually a reference to a namespace generated within your project - every project has its own internal NetJsonAot class

void SomeMethod()
{
    // this is a dictionary of your type (key) to the generated JsonSerializerContext types
    var typeDictionary = RuntimeJson.JsonContextTypes;

    // this is a dictionary of your type (key) to actual instances of their respective JsonSerializerContext types
    var contexts = RuntimeJson.JsonContexts;


    // this is a dictionary of your type (key) to instances of JsonSerializerOptions, ready to be used in actual serialization & deserialization
    var options = RuntimeJson.JsonSerializerOptions;
}

// example deserialization - serialization should be very similar
bool Deserialize<T>(string json, [NotNullWhen(true)] out T? result, [NotNullWhen(false)] out string? reason)
{

    var gotOption = RuntimeJson.JsonSerializerOptions.TryGetValue(type, out var options);

    if(!gotOptions)
    {
        Console.WriteLine("Woah there, this might not work with aot....");
        // return false;
    }

    try
    {
        value = JsonSerializer.Deserialize(s, type, options);

        if (value == null)
        {
            reason = $"Could not convert {s} to {type.Name}";
            return false;
        }

        reason = null;
        return true;
    }
    catch (JsonException e)
    {
        value = default!;
        reason = $"Could not convert json to {type.Name} - exception thrown: {e.Message}.\n'{s}'";
        return false;
    }
    catch (Exception e)
    {
        value = default!;
        reason = $"Unexpected exception thrown: {e.Message}.\n'{s}'";
        return false;
    }
}
```
