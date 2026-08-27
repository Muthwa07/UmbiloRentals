using System;
using System.Linq;
using System.Web.Mvc;
using UmbiloRentals.Models;

namespace UmbiloRentals.Controllers
{
    public class MaintenanceController : Controller
    {
        private BuildingManagementDBEntities db =
            new BuildingManagementDBEntities();

        // GET: Maintenance/Create
        public ActionResult Create()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];

            // Get the applicant's most recent approved room
            var roomId = db.Applications
                .Where(a => a.UserID == userId && a.Status == "Approved")
                .OrderByDescending(a => a.DateApplied)
                .Select(a => a.RoomID)
                .FirstOrDefault();

            ViewBag.RoomID = roomId;

            return View();
        }

        // POST: Maintenance/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            string title,
            string description,
            string priority)
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];

            var roomId = db.Applications
                .Where(a => a.UserID == userId && a.Status == "Approved")
                .OrderByDescending(a => a.DateApplied)
                .Select(a => a.RoomID)
                .FirstOrDefault();

            MaintenanceRequest request =
                new MaintenanceRequest();

            request.TenantID = userId;
            request.RoomID = roomId;
            request.Title = title;
            request.Description = description;
            request.Priority = priority;
            request.Status = "Pending";
            request.DateReported = DateTime.Now;

            db.MaintenanceRequests.Add(request);
            db.SaveChanges();

            TempData["SuccessMessage"] =
                "Maintenance request submitted successfully.";

            return RedirectToAction("Index", "Dashboard");
        }

        public ActionResult MyRequests()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];

            var requests = db.MaintenanceRequests
                .Where(r => r.TenantID == userId)
                .OrderByDescending(r => r.DateReported)
                .ToList();

            return View(requests);
        }
    }
}