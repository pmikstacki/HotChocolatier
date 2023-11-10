# HotChocolatier
HotChocolate GraphQL Schema Generator for Entity Framework DbSets
It enables you to create hassle-free GraphQL APIs without having to write Schema manually (for queries and subscriptions - i still recommend using rest for writes)

# How it Works
It generates GraphQL Schema for Query and Subscription. Example use:
`-a E:\Projects\GameDesignStudio\src\Backend\GameDesignStudio.Data\bin\Debug\net7.0\GameDesignStudio.Data.dll
-n GameDesignStudio.GraphQL.Schema 
-v 
-o E:\Projects\GameDesignStudio\src\Backend\GameDesignStudio.GraphQL`

* -a is a dll holding the DbContext class
* -n is a target namespace of generated schema
* -v is used to display verbose logs
* -o defines output directory (of course it will create its' own Schema folder with subsequent Query and Subscription folders to organize each partial classes.

It automatically names partials based on DbSet name. So example output will look like: 

```CSharp
// ... inside DbContext
 [GraphQlSubscription(Authorize = true),
 GraphQlList(UseSorting = true, UseFiltering = true, UseProjection = true, Authorize = true),
 GraphQlPagedList(UseSorting = true, UseFiltering = true, UseProjection = true, Authorize = true),
 GraphQlGetById(Authorize = true)]
    public DbSet<User> Users { get; set; }
```

In order to get the attributes you need the HotChocolatier.Adnotations package. 
It contains following attributes: 
* GraphQLSubscription (creates subscriptions with Added and Updated Event - you need to trigger them manually from other services - see (https://chillicream.com/docs/hotchocolate/v13/defining-a-schema/subscriptions)
* GraphQLList (exposes raw IQueryable for projections, sorting etc - of course you can configure which one you want and which one you don't want)
* GraphQlPagedList (uses offset paging)
* GraphQLGetById (allows to get one item by integer id)

Output for Users DbSet will look like this (File Name: Query.Users.cs): 

```CSharp 
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;
namespace GameDesignStudio.GraphQL.Schema;

public partial class Query{
    
[Authorize, UseProjection, UseFiltering, UseSorting]
public IQueryable<GameDesignStudio.Data.Model.Auth.User> GetUsers([Service(ServiceKind.Synchronized)] GameDesignStudio.Data.Context.GameDesignStudioContext context)
 => context.Users;


[Authorize, UseOffsetPaging(DefaultPageSize = 50, IncludeTotalCount = true, MaxPageSize = 1000), UseProjection, UseFiltering, UseSorting]
public IQueryable<GameDesignStudio.Data.Model.Auth.User> GetUsersWithPaging([Service(ServiceKind.Synchronized)] GameDesignStudio.Data.Context.GameDesignStudioContext context)
 => context.Users;


public async ValueTask<GameDesignStudio.Data.Model.Auth.User?> GetUser([Service(ServiceKind.Synchronized)] GameDesignStudio.Data.Context.GameDesignStudioContext context, int id)
=> await context.Users.FirstOrDefaultAsync(x => x.Id == id);


}
```

## I'M SOLD WHERE TO GET THIS 

I will post here links for tool install and nuget package for adnotations soon
