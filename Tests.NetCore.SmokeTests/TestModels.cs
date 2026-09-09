using Saule;

namespace Tests.NetCore.SmokeTests
{
    public class Person
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class PersonResource : ApiResource
    {
        public PersonResource()
        {
            Attribute(nameof(Person.Name));
        }
    }
}
