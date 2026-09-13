using System.Linq;
using System.Web.Mvc;
using UmbiloRentals.Models;

namespace UmbiloRentals.Controllers
{
    public class TenantController : Controller
    {
        private BuildingManagementDBEntities db =
            new BuildingManagementDBEntities();

        // Tenant Dashboard
        public ActionResult Index()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserID"];

            // Check if the user's application was approved
            bool isTenant = db.Applications.Any(a =>
                a.UserID == userId &&
                a.Status == "Approved");

            if (!isTenant)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        // My Room
        public ActionResult MyRoom()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserID"];

            var application = db.Applications
                .FirstOrDefault(a =>
                    a.UserID == userId &&
                    a.Status == "Approved");

            if (application == null)
            {
                return View();
            }

            var room = db.Rooms.Find(application.RoomID);

            if (room == null)
            {
                return View();
            }

            return View(room);
        }
        // Save Profile Changes

        public ActionResult profile()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserID"];

            User user = db.Users.Find(userId);

            if (user == null)
            {
                return HttpNotFound();
            }

            return View(user);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult profile(User user)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserID"];

            User existingUser = db.Users.Find(userId);

            if (existingUser == null)
            {
                return HttpNotFound();
            }

            existingUser.FirstName = user.FirstName;
            existingUser.LastName = user.LastName;
            existingUser.Email = user.Email;
            existingUser.Phone = user.Phone;

            if (!string.IsNullOrEmpty(user.Password))
            {
                existingUser.Password = user.Password;
            }

            db.SaveChanges();

            Session["UserName"] =
                existingUser.FirstName + " " +
                existingUser.LastName;

            TempData["SuccessMessage"] =
                "Your profile has been updated successfully.";

            return RedirectToAction("profile");
        }
        // Announcements
        public ActionResult Announcements()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var announcements = db.Announcements
                .Where(a => a.TargetRole == "Tenant")
                .OrderByDescending(a => a.DatePosted)
                .ToList();

            return View(announcements);
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