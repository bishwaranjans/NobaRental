namespace NobaRental.Backend.WebApi.Auth;

public static class AuthConstants
{
    public const string PermissionsClaimType = "permissions";
    public const string ScopeClaimType = "scope";

    public static class Permissions
    {
        public const string RentalsPickup = "rentals:pickup";
        public const string RentalsReturn = "rentals:return";
        public const string RentalsRead = "rentals:read";
        public const string FleetManage = "fleet:manage";
    }

    public static class Policies
    {
        public const string RentalsPickup = "RentalsPickup";
        public const string RentalsReturn = "RentalsReturn";
        public const string RentalsRead = "RentalsRead";
        public const string FleetManage = "FleetManage";
    }
}
