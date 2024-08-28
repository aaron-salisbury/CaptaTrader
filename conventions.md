Architecture
================

1. Libraries will target the LTS version (highest even number) of .NET. 
2. Application projects can target either the LTS or STS  versions of .NET.
3. Follow a [Three-Tier Architecture](https://www.ibm.com/topics/three-tier-architecture) (Data, Business, Presentation).
4. Name projects with the postfix of their tier (e.g., MyCoolApp.Data).
5. There can be more than one presentation for the set of given business functionality (i.e., a mobile app and a desktop application). Name each presentation project with the target platform postfix (e.g., MyCoolApp.Presentation.Web).
6. Any GUI projects for desktop and mobile targets should follow the [MVVM Design Pattern](https://dev.to/mochafreddo/mastering-mvvm-a-comprehensive-guide-to-the-model-view-viewmodel-architecture-221g).


Business Classes
================

Generally follow the definitions described within this [article](https://medium.com/@mishraabhinn/understanding-domain-objects-entities-dtos-and-models-in-c-207bb5c1d97c).

1. **Entities** - Uniquely identifiable objects. This includes everything that would get mapped to a database table.
2. **DTOs** - Lightweight objects for data transfer. All should be considered serializable.
3. **Domains** - Real-world concepts and business logic. Will have an access modifier of Internal.
4. **Models** - Can represent a view of either domains or DTOs, with the intention that these are what will be consumed by any presentation layer. In MVVM and MVC, models are the objects used for data binding. Will have an access modifier of Public.

Coding
================

1. Prefer `Foo fooInstance = new();` over `var fooInstance = new Foo();`.
2. Name constants is all caps and snake case (underscores between words) (e.g., `const string API_ENDPOINT = "https://rest.example.com/v1";`).
3. Name private fields as camel case with a leading undercore (e.g., `private int _counter = 0;`).
3. Name public and internal members as pascal case (e.g., `public required DateTime TimeStamp { get; set; }`).
4. Generally follow [this](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) for C#.
5. Generally follow [this](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/) for libraries.
6. Generally follow [this](https://www.infoq.com/articles/Exceptions-API-Design/) for exceptions.
