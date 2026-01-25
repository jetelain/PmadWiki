using Pmad.Wiki.Services;

namespace Pmad.Wiki.Demo.Entities
{
    public class DemoUser : IWikiUser
    {
        public int DemoUserId { get; set; }

        public required string SteamId { get; set; }

        public required string GitEmail { get; set; }

        public required string GitName { get; set; }

        public required string DisplayName { get; set; }
    }
}