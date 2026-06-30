using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace TMDTStore.Models;

public partial class Role : IdentityRole
{
    public string? Description { get; set; }

    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}
