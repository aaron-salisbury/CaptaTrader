Architecture
================

1. Projects will target the LTS version (highest even number) of .NET.
2. Follow a [Three-Tier Architecture](https://www.ibm.com/topics/three-tier-architecture) (Data, Business, Presentation). Logical boundaries can be further seperated as needed.
3. Name projects with the postfix of their tier (e.g., MyCoolApp.Data).
4. There can be more than one presentation for the set of given business functionality (i.e., a mobile app and a desktop application). Name each presentation project with the target platform postfix (e.g., MyCoolApp.Presentation.Web).
5. GUI app projects that target desktop or mobile should follow the [MVVM Design Pattern](https://dev.to/mochafreddo/mastering-mvvm-a-comprehensive-guide-to-the-model-view-viewmodel-architecture-221g).
6. CLI app projects should follow a Model–View–Presenter design pattern.


Business Classes
================

Generally follow the definitions described within this [article](https://medium.com/@mishraabhinn/understanding-domain-objects-entities-dtos-and-models-in-c-207bb5c1d97c).

1. **Entities** - Uniquely identifiable objects. This includes everything that would get mapped to a database table.
2. **DTOs** - Lightweight objects for data transfer. All should be considered serializable and made as records. Always a [POCO](https://ardalis.com/dto-or-poco/).
3. **Domains** - Real-world concepts used internally within business logic and don't need to persist.
4. **Models** - Can represent a view of either entities, domains, or DTOs. With the intention that these are what will be consumed by any presentation layer and may have user validations. In MVVM and MVC, models are the objects used for data binding.

Coding
================

1. Prefer `Foo fooInstance = new();` over `var fooInstance = new Foo();`.
2. Name constants in all caps and snake case (underscores between words) (e.g., `const string API_ENDPOINT = "https://rest.example.com/v1";`).
3. Name private fields as camel case with a leading undercore (e.g., `private int _counter = 0;`).
3. Name public and internal members as pascal case (e.g., `public required DateTime TimeStamp { get; set; }`).
4. Generally follow [this](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) for C#.
5. Generally follow [this](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/) for libraries.
6. Generally follow [this](https://www.infoq.com/articles/Exceptions-API-Design/) for exceptions.
