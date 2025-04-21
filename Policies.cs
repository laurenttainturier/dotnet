using System.Reflection;
using Microsoft.OpenApi.Extensions;

namespace backend;

public enum CrudOperations
{
    Create,
    Read,
    Update,
    Delete
}

public class AppResource(string name)
{
    public virtual IEnumerable<string> Operations => Enum.GetNames<CrudOperations>();
    public string Policy(CrudOperations operation) => PolicyName(operation.GetDisplayName());
    public IEnumerable<string> Policies => Operations.Select(PolicyName);

    protected string PolicyName(string operation) => $"{name}:{operation}".ToLower();
}

public class AppResource<TAdditionalOperation>(string name)
    : AppResource(name) where TAdditionalOperation : struct, Enum
{
    public override IEnumerable<string> Operations => Enum.GetNames<CrudOperations>()
        .Concat(Enum.GetNames<TAdditionalOperation>());

    public string Policy(TAdditionalOperation operation) => PolicyName(operation.GetDisplayName());
}

public enum RfqOperations
{
    Acknowledge
}

[AttributeUsage(AttributeTargets.Class)]
public class AppResourcesAttribute : Attribute;

[AppResources]
public class TechDocResources
{
    public static readonly AppResource TechDoc = new("tech-doc");
}

[AppResources]
public class SparesRepairsResources
{
    public static readonly AppResource Contract = new("contract");
    public static readonly AppResource<RfqOperations> Rfq = new("rfq");

    public static void Test()
    {
        Console.WriteLine(Contract.Policy(CrudOperations.Create));
        Console.WriteLine(Rfq.Policy(RfqOperations.Acknowledge));
        Console.WriteLine(Rfq.Policy(CrudOperations.Create));
        Console.WriteLine(Rfq.Policy(RfqOperations.Acknowledge));
    }

    public static void GetRes()
    {
        var policies = PoliciesDiscovery.GetAllPolicies();

        foreach (var policy in policies)
        {
            Console.WriteLine(policy);
        }
    }
}

public static class PoliciesDiscovery
{
    public static IEnumerable<string> GetAllPolicies()
    {
        return ReflectionUtils
            .GetClassesWithAttribute<AppResourcesAttribute>()
            .SelectMany(type => type.GetPublicStaticObjects<AppResource>())
            .SelectMany(resource => resource.Policies);
    }
}

public static class ReflectionUtils
{
    public static IEnumerable<T> GetPublicStaticObjects<T>(this Type type)
    {
        return type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType.IsAssignableTo(typeof(T)))
            .Select(field => field.GetValue(null))
            .Cast<T>();
    }

    public static IEnumerable<Type> GetClassesWithAttribute<TAttribute>() where TAttribute : Attribute
    {
        return Assembly.GetExecutingAssembly().GetTypes()
            .Where(type => type.IsDefined(typeof(TAttribute)));
    }
}
