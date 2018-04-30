using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnAuth.Web.Areas.Users.Home
{
    public partial class HomeController : UsersController
    {
        UserManager<IdentityUser> _userManager;

        public HomeController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public virtual IActionResult Index()
        {
            return View();
        }

        public virtual IActionResult ChangePassword()
        {
            var model = new ChangePasswordViewModel();
            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);

            await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            return RedirectToAction("Index");
        }

    }

    public class ChangePasswordViewModel
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
