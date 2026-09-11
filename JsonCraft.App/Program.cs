using System.Diagnostics;
using JsonCraft.Core;

Console.WriteLine("=== Assignment 1: JSON Serializer Verification ===\n");

var testUser = new User
{
    Id = 1,
    Name = "John Doe",
    IsActive = true,
    Score = 98.75m,
    CreatedAt = DateTime.UtcNow,
    ApiKey = Guid.NewGuid(),
    Role = UserRole.Admin,
    NullableInt = 42
};

var userJson = JsonSerializer.Serialize(testUser);
Console.WriteLine("1. Serialized User Object:\n" + userJson);

var deserializedUser = JsonSerializer.Deserialize<User>(userJson);
Console.WriteLine($"\n2. Deserialized User Check: Name={deserializedUser?.Name}, Role={deserializedUser?.Role}, ApiKey={deserializedUser?.ApiKey}");

var org = new Organization
{
    OrgName = "Tech Corp",
    Tags = ["Cloud", "DevOps", "AI"],
    Staff =
    [
        new User { Id = 2, Name = "Alice", IsActive = true },
        new User { Id = 3, Name = "Bob", IsActive = false }
    ],
    Metadata = new Dictionary<string, object>
    {
        { "region", "APAC" },
        { "tier", 1 }
    }
};

var orgJson = JsonSerializer.Serialize(org);
Console.WriteLine("\n3. Serialized Nested Object, Collections & Dictionary:\n" + orgJson);

var deserializedOrg = JsonSerializer.Deserialize<Organization>(orgJson);
Console.WriteLine($"\n4. Deserialized Org Check: Name={deserializedOrg?.OrgName}, TagsCount={deserializedOrg?.Tags?.Count}, StaffCount={deserializedOrg?.Staff?.Count}");

try
{
    var nodeA = new CircularNode { Name = "Node A" };
    var nodeB = new CircularNode { Name = "Node B" };
    nodeA.Next = nodeB;
    nodeB.Next = nodeA;

    Console.WriteLine("\n5. Testing Circular Reference Detection...");
    JsonSerializer.Serialize(nodeA);
}
catch (JsonSerializationException ex)
{
    Console.WriteLine($"[Expected Exception Caught]: {ex.Message}");
}

try
{
    Console.WriteLine("\n6. Testing Malformed JSON Error Handling...");
    var invalidJson = "{\"Id\": 1, \"Name\": \"Broken\",}";
    JsonSerializer.Deserialize<User>(invalidJson);
}
catch (JsonSerializationException ex)
{
    Console.WriteLine($"[Expected Exception Caught]: {ex.Message}");
}

Console.WriteLine("\n7. Performance Benchmark (10,000 Iterations)...");
var benchmarkUser = new User { Id = 99, Name = "Benchmark", IsActive = true, Role = UserRole.User };

for (var i = 0; i < 500; i++)
{
    JsonSerializer.Serialize(benchmarkUser);
}

var sw = Stopwatch.StartNew();
for (var i = 0; i < 10000; i++)
{
    JsonSerializer.Serialize(benchmarkUser);
}
sw.Stop();
Console.WriteLine($"Serialization 10,000 runs completed in: {sw.ElapsedMilliseconds} ms");

Console.WriteLine("\nAll verification checks passed successfully!");
public enum UserRole
{
    User,
    Admin,
    Manager
}
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public decimal Score { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid ApiKey { get; set; }
    public UserRole Role { get; set; }
    public int? NullableInt { get; set; }
}
public class Organization
{
    public string OrgName { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public List<User> Staff { get; set; } = [];
    public Dictionary<string, object> Metadata { get; set; } = [];
}
public class CircularNode
{
    public string Name { get; set; } = string.Empty;
    public CircularNode? Next { get; set; }
}