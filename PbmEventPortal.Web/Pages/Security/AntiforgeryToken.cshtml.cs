using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PbmEventPortal.Web.Pages.Security;

public class AntiforgeryTokenModel : PageModel
{
    private readonly IAntiforgery _antiforgery;

    public AntiforgeryTokenModel(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    public IActionResult OnGet()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

        return new JsonResult(new
        {
            success = true,
            token = tokens.RequestToken
        });
    }
}
