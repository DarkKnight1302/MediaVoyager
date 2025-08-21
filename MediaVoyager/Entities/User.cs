namespace MediaVoyager.Entities
{
    public class User
    {
        public string id { get; set; }

        public string name { get; set; }

        public string email { get; set; }

        public string passwordHash { get; set; }

        public bool googleLogin { get; set; }

        public DateTimeOffset lastLogin { get; set; }

        public DateTimeOffset createdAt { get; set; }

        public DateTimeOffset updatedAt { get; set; }
    }
}
