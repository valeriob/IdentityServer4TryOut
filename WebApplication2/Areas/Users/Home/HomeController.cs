using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication2.Models;

namespace WebApplication2.Areas.Users.Home
{
    public partial class HomeController : UsersController
    {
        static HomeController()
        {
            ModelUnbinderHelpers.ModelUnbinders.Add(typeof(IndexViewModel), new SimplePropertyModelUnbinder());
        }

        UserManager<IdentityUser> _userManager;

        public HomeController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public virtual IActionResult Index(IndexViewModel model)
        {
            var query = _userManager.Users.ApplyPaging(model.ToPaging(), u => u.UserName);

            model.Users = query.ToArray();

            return View(model);
        }

        public virtual IActionResult Create()
        {
            var model = new CreateViewModel();
            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Create(CreateViewModel model)
        {
            var user = model.ToIdentityUser();

            var result = await _userManager.CreateAsync(user, model.Password);
           
            return RedirectToAction(Actions.Index());
        }


        public virtual async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            var model = new EditViewModel();

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Edit([FromBody] EditViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);

            await _userManager.SetUserNameAsync(user, model.Username);

            return RedirectToAction(Actions.Edit(model.Id));
        }

    }

    public class IndexViewModel : PagingViewModel
    {
        public IdentityUser[] Users { get; set; }

        public override string PrevPageUrl(IUrlHelper url)
        {
            var route = MVC.Users.Home.Index(this);
            return PrevPageUrl(url, route);
        }

        public override string NextPageUrl(IUrlHelper url)
        {
            var route = MVC.Users.Home.Index(this);
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

    public class CreateViewModel
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }

        internal IdentityUser ToIdentityUser()
        {
            return new IdentityUser(Username)
            {
                 
            };
        }
    }

    public class EditViewModel
    {
        public string Id { get; set; }
        public string Username { get; set; }

    }
}
