# Custom JSON Serializer (Assignment 1)

This is a custom JSON serializer and deserializer built from scratch in C# without using `System.Text.Json` or `Newtonsoft.Json`. 

It uses C# reflection to inspect class properties dynamically, a custom recursive parser to read JSON text, and basic collection types to reconstruct objects.

---

## Supported Types

- Primitives: `string`, `int`, `long`, `short`, `byte`, `float`, `double`, `decimal`, `bool`, and `null`
- Special types: `DateTime`, `Guid`, `enum`, and nullable types (e.g., `int?`)
- Objects: Plain classes and nested objects at any depth
- Collections: Arrays, `List<T>`, and `IEnumerable<T>`
- Dictionaries: `Dictionary<string, object>` or `Dictionary<string, T>`

---

## How to Use

```csharp
using JsonCraft.Core;

// 1. Serialize an object
var user = new User { Id = 1, Name = "John", IsActive = true };
string json = JsonSerializer.Serialize(user);
Console.WriteLine(json);
// Output: {"Id":1,"Name":"John","IsActive":true}

// 2. Deserialize back into a C# object
var result = JsonSerializer.Deserialize<User>(json);
Console.WriteLine(result.Name);
```
## How It Works

1. Reading & Parsing JSON

Instead of regular expressions, I wrote a custom parser (JsonParser.cs) that reads through the JSON text character by character:

● Skips whitespace automatically.

● Handles escape characters like ", \, \n, \t, and unicode sequences (\uXXXX).

● Converts tokens into dictionaries, lists, strings, and numbers.

2. Deserialization
   
Deserializer.cs takes the parsed data and maps it into the target C# type using Activator.CreateInstance and reflection. It looks up properties by name and converts numbers, booleans, enums, dates, and collections to their correct types.

4. Circular References
   
If an object references itself directly or indirectly, a serializer can get stuck in an infinite loop.
To prevent this, I track object references in a HashSet. If an object is already being processed in the active call stack, it stops immediately and throws a JsonSerializationException.

6. Error Handling
   
The library validates the input and throws clear exceptions instead of silently failing:

● Trailing commas in arrays or objects raise errors.

● Unclosed brackets or quotes report the exact index where the issue happened.

● Type mismatches throw an error explaining which value could not be converted.

5. Performance Optimization
   
Running type.GetProperties() repeatedly with reflection is slow. To solve this, I added a simple cache (TypeCache.cs) using ConcurrentDictionary.
Property metadata is read once per type and reused for all future calls, which makes repeated serialization much faster.

● Performance Benchmark
Running the serializer inside a loop on Linux Mint:

Iterations: 10,000 runs

Total Time: ~28 ms

Average per run: ~0.0028 ms

Limitations
Dictionaries must use string keys (following the standard JSON specification).

Target classes must have a public parameterless constructor for deserialization.
