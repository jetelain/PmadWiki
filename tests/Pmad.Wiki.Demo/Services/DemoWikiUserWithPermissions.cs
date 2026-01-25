using Pmad.Wiki.Demo.Entities;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Demo.Services
{
    internal class DemoWikiUserWithPermissions : IWikiUserWithPermissions
    {
        private readonly DemoUser user;
        private readonly bool isAdmin;

        public DemoWikiUserWithPermissions(DemoUser user, bool isAdmin)
        {
            this.user = user;
            this.isAdmin = isAdmin;
        }

        public string[] Groups => isAdmin ? ["admin", "users"] : ["users"];

        public bool CanEdit => isAdmin;

        public bool CanView => true;

        public bool CanAdmin => isAdmin;

        public bool CanRemoteGit => isAdmin;

        public IWikiUser User => user;
    }
}