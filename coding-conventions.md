Architecture
================

1. Follow a [Three-Tier Architecture](https://www.ibm.com/topics/three-tier-architecture) (Data, Business, Presentation).
2. Name projects with the postfix of their tier (e.g., MyCoolApp.Data).
3. There can be more than one presentation for the set of given business functionality (i.e. a mobile app and a desktop application). Name each presentation project with the target platform postfix (e.g., MyCoolApp.Presentation.Web).
4. Any GUI projects for desktop and mobile targets should follow the [MVVM Design Pattern](https://dev.to/mochafreddo/mastering-mvvm-a-comprehensive-guide-to-the-model-view-viewmodel-architecture-221g).


Business Classes
================

Generally follow the definitions described within this [article](https://medium.com/@mishraabhinn/understanding-domain-objects-entities-dtos-and-models-in-c-207bb5c1d97c).

1. Entities - Uniquely identifiable objects. This includes everything that would get mapped to a database table.
2. DTOs - Lightweight objects for data transfer. All should be considered serializable.
3. Domains - Real-world concepts and business logic. Will have an access modifier of Internal.
4. Models - Can represent a view of either domains or DTOs, with the intention that these are what will be consumed by any presentation layer. In MVVM and MVC, models are the objects used for data binding. Will have an access modifier of Public.
