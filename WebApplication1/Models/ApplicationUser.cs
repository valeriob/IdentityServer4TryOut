using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer4
{
    public class ApplicationUser : IdentityUser
    {
    
    }

    public class ApplicationRole : IdentityRole
    {

    }

    public class ApplicationDbContext : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
             : base(options)
        {
           
        }
    }
}
