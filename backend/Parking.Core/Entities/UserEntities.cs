using System.Text.Json.Serialization;

namespace Parking.Core.Entities
{
    // [OOP] Polymorphic user accounts stored together with type discriminator
    public abstract class UserAccount
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Status { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }

        public abstract string Role { get; }

        public abstract bool CanAccessReports();
        public abstract bool CanManageUsers();
    }

    public class AdminAccount : UserAccount
    {
        public override string Role => "ADMIN";
        public override bool CanAccessReports() => true;
        public override bool CanManageUsers() => true;
    }

    public class AttendantAccount : UserAccount
    {
        public override string Role => "ATTENDANT";
        public override bool CanAccessReports() => false;
        public override bool CanManageUsers() => false;
    }
}
