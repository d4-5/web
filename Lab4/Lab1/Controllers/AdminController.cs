using Lab1.Constants;
using Lab1.Models;
using Lab1.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lab1.Controllers;

[Authorize(Roles = UserRoles.Administrator)]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.OrderBy(user => user.Email).ToListAsync();
        var model = new AdminIndexViewModel
        {
            AvailableRoles = await _roleManager.Roles
                .OrderBy(role => role.Name)
                .Select(role => role.Name!)
                .ToListAsync(),
            Users = await BuildUsersAsync(users)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(string id, string role)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (string.Equals(user.Id, _userManager.GetUserId(User), StringComparison.Ordinal))
        {
            TempData["AdminError"] = "Власну роль змінювати не можна.";
            return RedirectToAction(nameof(Index));
        }

        if (!await _roleManager.RoleExistsAsync(role))
        {
            TempData["AdminError"] = "Обрана роль не існує.";
            return RedirectToAction(nameof(Index));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
        {
            TempData["AdminError"] = "Не вдалося оновити роль користувача.";
            return RedirectToAction(nameof(Index));
        }

        var addResult = await _userManager.AddToRoleAsync(user, role);
        TempData[addResult.Succeeded ? "AdminSuccess" : "AdminError"] =
            addResult.Succeeded
                ? "Роль користувача оновлено."
                : "Не вдалося призначити нову роль.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (string.Equals(user.Id, _userManager.GetUserId(User), StringComparison.Ordinal))
        {
            TempData["AdminError"] = "Власний акаунт видалити не можна.";
            return RedirectToAction(nameof(Index));
        }

        if (await _userManager.IsInRoleAsync(user, UserRoles.Administrator))
        {
            var admins = await _userManager.GetUsersInRoleAsync(UserRoles.Administrator);
            if (admins.Count <= 1)
            {
                TempData["AdminError"] = "Не можна видалити останнього адміністратора.";
                return RedirectToAction(nameof(Index));
            }
        }

        var result = await _userManager.DeleteAsync(user);
        TempData[result.Succeeded ? "AdminSuccess" : "AdminError"] =
            result.Succeeded
                ? "Користувача видалено."
                : "Не вдалося видалити користувача.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<IReadOnlyCollection<AdminUserViewModel>> BuildUsersAsync(IEnumerable<ApplicationUser> users)
    {
        var currentUserId = _userManager.GetUserId(User);
        var result = new List<AdminUserViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new AdminUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? "N/A",
                RegisteredAt = user.RegisteredAt,
                Roles = roles.ToArray(),
                SelectedRole = roles.FirstOrDefault() ?? UserRoles.User,
                IsCurrentUser = string.Equals(user.Id, currentUserId, StringComparison.Ordinal)
            });
        }

        return result;
    }
}
