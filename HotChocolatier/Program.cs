using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using HotChocolatier;
using HotChocolatier.Adnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

var arguments = Environment.GetCommandLineArgs();
Options options = null;
Dictionary<string, string> propertyQueryFiles = new();
Dictionary<string, string> propertySubscriptionFiles = new();

Parser.Default.ParseArguments<Options>(args)
    .WithParsed<Options>(o =>
    {
        options = o;
        Log("Getting assembly");

        Assembly assembly = Assembly.LoadFrom(o.Assembly);
        Type? contextType = null;
        if (!string.IsNullOrEmpty(o.ContextName))
        {
            Log("Context with a predefined name specified");
            contextType = assembly.GetType(o.ContextName);
            if (contextType == null)
            {
                Log("Context was not found, quitting", ConsoleColor.Red);
                Environment.Exit(-100);
            }
        }

        contextType = assembly.GetTypes().FirstOrDefault(x => x.IsSubclassOf(typeof(DbContext)) && !x.IsAbstract);
        if (contextType == null)
        {
            Log("Context was not found, quitting", ConsoleColor.Red);
            Environment.Exit(-100);
        }

        Log("Context found, proceeding to analyze it", ConsoleColor.Green);

        var properties = contextType.GetProperties();

        Log($"Found {properties.Count()} properties, searching for ListAttributes");

        var propertiesWithListAttribute = properties.Where(x => x.GetCustomAttribute<GraphQlListAttribute>() != null);
        var propertiesWithPagedListAttribute = properties.Where(x => x.GetCustomAttribute<GraphQlPagedListAttribute>() != null);
        var propertiesGetByIdsAttribute = properties.Where(x => x.GetCustomAttribute<GraphQlGetByIdAttribute>() != null);
        var propertiesSubscriptionAttribute = properties.Where(x => x.GetCustomAttribute<GraphQlSubscriptionAttribute>() != null);
        Log($"Found {propertiesWithListAttribute.Count()} propertiesWithListAttribute, searching for ListAttributes");
        Log($"Found {propertiesWithPagedListAttribute.Count()} propertiesWithPagedListAttribute, searching for ListAttributes");
        Log($"Found {propertiesGetByIdsAttribute.Count()} propertiesGetByIdsAttribute, searching for ListAttributes");
        Log($"Found {propertiesSubscriptionAttribute.Count()} propertiesSubscriptionAttribute, searching for ListAttributes ended!.", ConsoleColor.Green);
        Log("Generating Queries...");
        GenerateQueryLists(propertiesWithListAttribute.ToList(), contextType);
        GenerateQueryPagedLists(propertiesWithPagedListAttribute.ToList(), contextType);
        GenerateQueryByIds(propertiesGetByIdsAttribute.ToList(), contextType);
        GenerateSubscription(propertiesSubscriptionAttribute.ToList(), contextType);
        foreach (var propertyQuery in propertyQueryFiles)
        {
            var path = o.Output != null ? Path.Combine(o.Output, "Schema", "Query") : Path.Combine("Schema", "Query");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            File.WriteAllText(Path.Combine(path, $"Query.{propertyQuery.Key}.cs"), $@"
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;
namespace {o.Namespace};

public partial class Query{{
    {propertyQuery.Value}
}}
            ");
        }

        foreach (var subscription in propertySubscriptionFiles)
        {
            var path = o.Output != null ? Path.Combine(o.Output, "Schema", "Subscription") : Path.Combine("Schema", "Subscription");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            File.WriteAllText(Path.Combine(path, $"Subscription.{subscription.Key}.cs"), $@"
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;
namespace {o.Namespace};

public partial class Subscription{{
    {subscription.Value}
}}
            ");
        }
    });

void GenerateQueryPagedLists(List<PropertyInfo> listInfo, Type contextType)
{
    Log("Generating query methods for paged lists");

    foreach (PropertyInfo property in listInfo)
    {
        Log($"Generating query method for {property.Name}");

        var attribute = property.GetCustomAttribute<GraphQlPagedListAttribute>();
        var genericType = property.PropertyType.GenericTypeArguments[0];
        var propertyName = property.Name;
        StringBuilder query = new();
        query.AppendLine();
        List<string> attributes = new();

        if (attribute.Authorize)
        {
            attributes.Add("Authorize");
        }

        attributes.Add(
            $"UseOffsetPaging(DefaultPageSize = {attribute.DefaultPageSize}, IncludeTotalCount = {attribute.IncludeTotalCount.ToString().ToLower()}, MaxPageSize = {attribute.MaxPageSize})");

        if (attribute.UseProjection)
        {
            attributes.Add("UseProjection");
        }

        if (attribute.UseFiltering)
        {
            attributes.Add("UseFiltering");
        }

        if (attribute.UseSorting)
        {
            attributes.Add("UseSorting");
        }

        if (attributes.Any())
        {
            query.AppendLine($"[{string.Join(", ", attributes)}]");
        }
        query.AppendLine(
            $"public IQueryable<{genericType.FullName}> Get{propertyName}WithPaging([Service(ServiceKind.Synchronized)] {contextType.FullName} context)");
        query.AppendLine($" => context.{propertyName};");
        query.AppendLine();

        if (propertyQueryFiles.TryGetValue(propertyName, out var file))
        {
            propertyQueryFiles[propertyName] += query.ToString();
            continue;
        }

        propertyQueryFiles.Add(propertyName, query.ToString());
    }
}

void GenerateQueryLists(List<PropertyInfo> listInfo, Type contextType)
{
    Log("Generating query methods for lists");

    foreach (PropertyInfo property in listInfo)
    {
        Log($"Generating query method for {property.Name}");

        var attribute = property.GetCustomAttribute<GraphQlListAttribute>();
        var genericType = property.PropertyType.GenericTypeArguments[0];
        var propertyName = property.Name;
        StringBuilder query = new();
        query.AppendLine();
        List<string> attributes = new();
        if (attribute.Authorize)
        {
            attributes.Add("Authorize");
        }

        if (attribute.UseProjection)
        {
            attributes.Add("UseProjection");
        }

        if (attribute.UseFiltering)
        {
            attributes.Add("UseFiltering");
        }

        if (attribute.UseSorting)
        {
            attributes.Add("UseSorting");
        }

        if (attributes.Any())
        {
            query.AppendLine($"[{string.Join(", ", attributes)}]");
        }
        query.AppendLine(
            $"public IQueryable<{genericType.FullName}> Get{propertyName}([Service(ServiceKind.Synchronized)] {contextType.FullName} context)");
        query.AppendLine($" => context.{propertyName};");
        query.AppendLine();

        if (propertyQueryFiles.TryGetValue(propertyName, out var file))
        {
            propertyQueryFiles[propertyName] += query.ToString();
            continue;
        }

        propertyQueryFiles.Add(propertyName, query.ToString());
    }
}

void GenerateQueryByIds(List<PropertyInfo> listInfo, Type contextType)
{
    Log("Generating query methods for GetByIds");

    foreach (PropertyInfo property in listInfo)
    {
        Log($"Generating query method for {property.Name}");

        var attribute = property.GetCustomAttribute<GraphQlGetByIdAttribute>();
        var genericType = property.PropertyType.GenericTypeArguments[0];
        var propertyName = property.Name;
        StringBuilder query = new();
        query.AppendLine();
        if (attribute.Authorize)
        {
            query.AppendLine($"[Authorize]");
        }
        query.AppendLine(
            $"public async ValueTask<{genericType.FullName}?> Get{genericType.Name}([Service(ServiceKind.Synchronized)] {contextType.FullName} context, int id)");
        query.AppendLine($"=> await context.{propertyName}.FirstOrDefaultAsync(x => x.Id == id);");
        query.AppendLine();

        if (propertyQueryFiles.TryGetValue(propertyName, out var file))
        {
            propertyQueryFiles[propertyName] += query.ToString();
            continue;
        }

        propertyQueryFiles.Add(propertyName, query.ToString());
    }
}

void GenerateSubscription(List<PropertyInfo> listInfo, Type contextType)
{
    Log("Generating query methods for GetByIds");

    foreach (PropertyInfo property in listInfo)
    {
        Log($"Generating query method for {property.Name}");
        StringBuilder query = new();
        StringBuilder attributes = new();
        var attribute = property.GetCustomAttribute<GraphQlSubscriptionAttribute>();
        if (attribute.Authorize)
        {
            attributes.Append($"[Subscribe, Authorize]");
        }
        else
        {
            attributes.Append($"[Subscribe]");

        }
        var genericType = property.PropertyType.GenericTypeArguments[0];
        var propertyName = property.Name;
        query.Append($@"
    {attributes.ToString()}
    public {genericType.FullName} {genericType.Name}Added([EventMessage] {genericType.FullName} {genericType.Name.ToString().ToLower()}) => {genericType.Name.ToString().ToLower()};
    {attributes.ToString()}
    public {genericType.FullName} {genericType.Name}Changed([EventMessage] {genericType.FullName} {genericType.Name.ToString().ToLower()}) => {genericType.Name.ToString().ToLower()};
       ");
        propertySubscriptionFiles.Add(propertyName, query.ToString());
    }
}

void Log(string message, ConsoleColor? messageColor = null)
{
    if (options.Verbose)
    {
        Console.ForegroundColor = messageColor ?? ConsoleColor.White;
        Console.WriteLine(message);
        Console.ForegroundColor = ConsoleColor.White;
    }
}
