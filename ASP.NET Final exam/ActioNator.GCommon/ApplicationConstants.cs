namespace ActioNator.GCommon
{
    /// <summary>
    /// Application role constants and a collection for quick lookup operations.
    /// </summary>
    public static class RoleConstants
    {
        /// <summary>
        /// Administrator role.
        /// </summary>
        public const string Admin = "Admin";

        /// <summary>
        /// Coach role.
        /// </summary>
        public const string Coach = "Coach";

        /// <summary>
        /// Regular user role.
        /// </summary>
        public const string User = "User";

        /// <summary>
        /// A read-only set of all role names for fast lookups.
        /// </summary>
        public static readonly HashSet<string> AllRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            Admin,
            Coach,
            User
        };
    }

    /// <summary>
    /// Redirect paths for different roles in the application.
    /// </summary>
    public static class RedirectPathConstants
    {
        /// <summary>
        /// Path to the Admin home page.
        /// </summary>
        public const string AdminHome = "/Admin/Home/Index";

        /// <summary>
        /// Path to the Coach home page.
        /// </summary>
        public const string CoachHome = "/Coach/Home/Index";

        /// <summary>
        /// Path to the User home page.
        /// </summary>
        public const string UserHome = "/User/Home/Index";

        /// <summary>
        /// A read-only dictionary mapping each role to its home redirect path.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> RoleRedirects =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { RoleConstants.Admin, AdminHome },
                { RoleConstants.Coach, CoachHome },
                { RoleConstants.User, UserHome }
            };
    }
}
