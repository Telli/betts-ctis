using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BettsTax.Web.Controllers;

[Authorize]
public class ChatViewController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
