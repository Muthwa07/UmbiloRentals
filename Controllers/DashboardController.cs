using System.Linq;
using System.Web.Mvc;
using UmbiloRentals.Models;

namespace UmbiloRentals.Controllers
{
    public class DashboardController : Controller
    {
        private BuildingManagementDBEntities db =
            new BuildingManagementDBEntities();

        // GET: Dashboard
        public ActionResult Index()
        {
            // Make sure the user is logged in
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserID"];

            // Get logged-in user
            var user = db.Users.Find(userId);

            // Get the logged-in user's applications
            var applications = db.Applications
                                 .Where(a => a.UserID == userId)
                                 .ToList();

            // Dashboard statistics
            ViewBag.FirstName = user?.FirstName ?? "Resident";

            ViewBag.TotalApplications = applications.Count;

            ViewBag.PendingApplications =
                applications.Count(a => a.Status == "Pending");

            ViewBag.ApprovedApplications =
                applications.Count(a => a.Status == "Approved");

            ViewBag.AvailableRooms =
                db.Rooms.Count(r => r.Status == "Available");

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}