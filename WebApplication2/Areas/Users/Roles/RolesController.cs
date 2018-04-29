using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication2.Models;

namespace WebApplication2.Areas.Users.Roles
{
    public partial class RolesController : UsersController
    {
        static RolesController()
        {
            ModelUnbinderHelpers.ModelUnbinders.Add(typeof(IndexViewModel), new SimplePropertyModelUnbinder());
        }

        RoleManager<IdentityRole> _roleManager;

        public RolesController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public virtual IActionResult Index(IndexViewModel model)
        {

            var query = _roleManager.Roles.ApplyPaging(model.ToPaging(), u => u.Name);

            model.Roles = query.ToArray();

            return View(model);
        }
    }

    public class IndexViewModel : PagingViewModel
    {
        public IdentityRole[] Roles { get; set; }

        public override string PrevPageUrl(IUrlHelper url)
        {
            var route = MVC.Users.Roles.Index(this);
            return PrevPageUrl(url, route);
        }

        public override string NextPageUrl(IUrlHelper url)
        {
            var route = MVC.Users.Roles.Index(this);
            return NextPageUrl(url, route);
        }

        public Paging ToPaging()
        {
            return new Paging
            {
                Page = Page,
                PageSize = PageSize,
                OrderBy = OrderBy,
                OrderByDescending = OrderByDescending,
            };
        }
    }

}
