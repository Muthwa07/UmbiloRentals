using System;
using System.Linq;
using System.Data.Entity;
using System.Web.Mvc;
using UmbiloRentals.Helpers;
using UmbiloRentals.Models;

namespace UmbiloRentals.Controllers
{
    public class MaintenanceController : BaseController
    {
        // Only Maintenance Staff may enter
        private bool IsMaintenance()
        {
            return Session["UserID"] != null &&
                   Session["RoleID"] != null &&
                   (int)Session["RoleID"] == 3;
        }

        // ==========================
        // DASHBOARD
        // ==========================
        public ActionResult Index()
        {
            if (!IsMaintenance())
                return RedirectToAction("Login", "Account");

            int technicianId = (int)Session["UserID"];

            ViewBag.NewJobs = db.MaintenanceRequests.Count(m => m.Status == "Pending");

            ViewBag.InProgress = db.MaintenanceRequests.Count(m =>
                m.Status == "In Progress" &&
                m.AssignedTechnicianID == technicianId);

            ViewBag.CompletedToday = db.MaintenanceRequests.Count(m =>
    m.Status == "Completed" &&
    m.AssignedTechnicianID == technicianId &&
    m.DateCompleted.HasValue &&
    DbFunctions.TruncateTime(m.DateCompleted.Value) == DateTime.Today);

            var queue = db.MaintenanceRequests
                .Where(m =>
                    m.Status == "Pending" ||
                    (m.Status == "In Progress" &&
                     m.AssignedTechnicianID == technicianId) ||
                    (m.Status == "Completed" &&
                     m.AssignedTechnicianID == technicianId &&
                     m.DateCompleted.HasValue &&
                     DbFunctions.TruncateTime(m.DateCompleted.Value) == DateTime.Today))
                .OrderBy(m =>
                    m.Priority == "Critical" ? 1 :
                    m.Priority == "High" ? 2 :
                    m.Priority == "Medium" ? 3 : 4)
                .ThenBy(m => m.SLADueDate)
                .ToList();

            return View(queue);
        }

        // ==========================
        // TENANT - MY REQUESTS
        // ==========================
        public ActionResult MyRequests()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            if ((int)Session["RoleID"] != 1)
                return RedirectToAction("Index");

            int tenantId = (int)Session["UserID"];

            var requests = db.MaintenanceRequests
                .Where(m => m.TenantID == tenantId)
                .OrderByDescending(m => m.DateReported)
                .ToList();

            return View(requests);
        }

        // GET: Maintenance/Report
        public ActionResult Report()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            ViewBag.Rooms = new SelectList(db.Rooms
                .Where(r => r.Status == "Occupied")
                .OrderBy(r => r.RoomNumber),
                "RoomID",
                "RoomNumber");

            return View();
        }

        // POST: Maintenance/Report
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Report(MaintenanceRequest request)
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                request.TenantID = (int)Session["UserID"];
                request.DateReported = DateTime.Now;
                request.Status = "Pending";
                request.DateStarted = null;
                request.DateCompleted = null;
                request.AssignedTechnicianID = null;
                request.TechnicianNotes = null;

                db.MaintenanceRequests.Add(request);
                db.SaveChanges();

                TempData["SuccessMessage"] =
                    "Maintenance request submitted successfully.";

                return RedirectToAction("MyRequests");
            }

            ViewBag.Rooms = new SelectList(db.Rooms
                .Where(r => r.Status == "Occupied")
                .OrderBy(r => r.RoomNumber),
                "RoomID",
                "RoomNumber",
                request.RoomID);

            return View(request);
        }

        // ==========================
        // START WORK
        // ==========================
        public ActionResult Start(int id)
        {
            if (!IsMaintenance())
                return RedirectToAction("Login", "Account");

            var request = db.MaintenanceRequests.Find(id);

            if (request == null)
                return HttpNotFound();

            if (!CanStartJob(request))
            {
                TempData["ErrorMessage"] =
                    "Complete higher-priority jobs first.";

                return RedirectToAction("Index");
            }

            request.Status = "In Progress";
            request.DateStarted = DateTime.Now;
            request.AssignedTechnicianID = (int)Session["UserID"];

            db.SaveChanges();

            NotificationHelper.CreateNotification(
                db,
                request.User.UserID,
                "Your maintenance request is now being worked on.");

            return RedirectToAction("Index");
        }

        // ==========================
        // COMPLETE WORK
        // ==========================
        public ActionResult Complete(int id)
        {
            if (!IsMaintenance())
                return RedirectToAction("Login", "Account");

            var request = db.MaintenanceRequests.Find(id);

            if (request == null)
                return HttpNotFound();

            request.Status = "Completed";
            request.DateCompleted = DateTime.Now;

            db.SaveChanges();

            NotificationHelper.CreateNotification(
                db,
                request.User.UserID,
                "Your maintenance request has been completed.");

            return RedirectToAction("Index");
        }

        // ==========================
        // PRIORITY LOCK
        // ==========================
        private bool CanStartJob(MaintenanceRequest request)
        {
            var pending = db.MaintenanceRequests
                .Where(m => m.Status == "Pending");

            switch (request.Priority)
            {
                case "Critical":
                    return true;

                case "High":
                    return !pending.Any(m => m.Priority == "Critical");

                case "Medium":
                    return !pending.Any(m =>
                        m.Priority == "Critical" ||
                        m.Priority == "High");

                default:
                    return !pending.Any(m =>
                        m.Priority == "Critical" ||
                        m.Priority == "High" ||
                        m.Priority == "Medium");
            }
        }
    }
}