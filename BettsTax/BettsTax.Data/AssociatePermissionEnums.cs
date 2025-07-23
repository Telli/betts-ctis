using System;

namespace BettsTax.Data
{
    [Flags]
    public enum AssociatePermissionLevel
    {
        None = 0,
        Read = 1,
        Create = 2,
        Update = 4,
        Delete = 8,
        Submit = 16,
        Approve = 32,
        All = Read | Create | Update | Delete | Submit | Approve
    }

    public enum ClientIdSource
    {
        Route,
        Body,
        Query,
        Header
    }

    public enum DocumentSharePermission
    {
        Read = 1,
        Download = 2,
        Comment = 4,
        Edit = 8,
        All = Read | Download | Comment | Edit
    }
}