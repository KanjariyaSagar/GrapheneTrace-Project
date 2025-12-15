using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GrapheneTrace.Controllers
{
    [Authorize(Roles = "Clinician")]
    public class ClinicianController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
