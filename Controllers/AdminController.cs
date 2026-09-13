using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UmbiloRentals.Helpers;
using UmbiloRentals.Models;

namespace UmbiloRentals.Controllers
{
    public class AdminController : BaseController
    {
        private readonly BuildingManagementDBEntities db =
            new BuildingManagementDBEntities();

        private bool IsAdmin()
        {
            return Session["UserID"] != null &&
                   Session["RoleID"] != null &&
                   (int)Session["RoleID"] == 2;
        }

        // ==========================
        // ADMIN DASHBOARD
        // ==========================
        public ActionResult Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            ViewBag.TotalUsers =
                db.Users.Count(u => u.RoleID == 1);

            ViewBag.PendingApplications =
                db.Applications.Count(a => a.Status == "Pending");

            ViewBag.ApprovedApplications =
                db.Applications.Count(a => a.Status == "Approved");

            ViewBag.AvailableRooms =
                db.Rooms.Count(r => r.Status == "Available");

            ViewBag.OccupiedRooms =
                db.Rooms.Count(r => r.Status == "Occupied");

            ViewBag.PendingPayments =
                db.Payments.Count(p => p.Status == "Pending");


            return View();
        }

        // ==========================
        // APPLICATIONS
        // ==========================
        public ActionResult Applications()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var applications =
                (from a in db.Applications
                 join u in db.Users on a.UserID equals u.UserID
                 join r in db.Rooms on a.RoomID equals r.RoomID
                 orderby a.DateApplied descending
                 select new AdminApplicationViewModel
                 {
                     ApplicationID = a.ApplicationID,
                     ApplicantName = u.FirstName + " " + u.LastName,
                     RoomNumber = r.RoomNumber,
                     RoomPhoto = r.Photo,
                     RoomID = a.RoomID,
                     Status = a.Status,
                     DateApplied = a.DateApplied,
                     DocumentPath = a.DocumentPath
                 }).ToList();

            return View(applications);
        }

        public ActionResult Approve(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var application = db.Applications.Find(id);

            if (application == null)
                return HttpNotFound();

            // Approve application
            application.Status = "Approved";

            // Allocate the room
            var room = db.Rooms.Find(application.RoomID);

            string allocationLetter = null;

            if (room != null)
            {
                room.Status = "Occupied";

                // Generate branded allocation letter
                var applicant = db.Users.Find(application.UserID);

                string applicantName = applicant != null
                    ? applicant.FirstName + " " + applicant.LastName
                    : "Applicant";

                allocationLetter = PdfHelper.GenerateAllocationLetter(
                    applicantName,
                    room.RoomNumber,
                    room.MonthlyRent ?? 0);
            }

            // Reject other pending applications for the same room
            var others = db.Applications.Where(a =>
                a.RoomID == application.RoomID &&
                a.ApplicationID != application.ApplicationID &&
                a.Status == "Pending");

            foreach (var app in others)
            {
                app.Status = "Rejected";

                NotificationHelper.CreateNotification(
                    db,
                    app.UserID.Value,
                    "This room is no longer available because it has been allocated to another applicant.");
            }

            db.SaveChanges();

            // Notify successful applicant
            NotificationHelper.CreateNotification(
                db,
                application.UserID.Value,
                "Congratulations! Your application has been approved. Please upload your proof of payment to continue.");

            // Make the download button appear
            TempData["AllocationLetter"] = allocationLetter;
            TempData["SuccessMessage"] = "Room allocated successfully.";

            return RedirectToAction("Applications");
        }

        public ActionResult Reject(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var application = db.Applications.Find(id);

            if (application == null)
                return HttpNotFound();

            application.Status = "Rejected";

            db.SaveChanges();

            NotificationHelper.CreateNotification(
                db,
                application.UserID.Value,
                "❌ Unfortunately your application wasn't successful.");

            return RedirectToAction("Applications");
        }

        // ==========================
        // ROOMS
        // ==========================
        public ActionResult Rooms()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            return View(db.Rooms.OrderBy(r => r.RoomNumber).ToList());
        }

        public ActionResult CreateRoom()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateRoom(Room room, HttpPostedFileBase photoFile)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                if (photoFile != null && photoFile.ContentLength > 0)
                {
                    string extension =
                        System.IO.Path.GetExtension(photoFile.FileName);

                    string fileName =
                        Guid.NewGuid() + extension;

                    string folder =
                        Server.MapPath("~/Content/RoomImages");

                    if (!System.IO.Directory.Exists(folder))
                        System.IO.Directory.CreateDirectory(folder);

                    photoFile.SaveAs(
                        System.IO.Path.Combine(folder, fileName));

                    room.Photo = fileName;
                }

                db.Rooms.Add(room);
                db.SaveChanges();

                TempData["SuccessMessage"] =
                    "Room created successfully.";

                return RedirectToAction("Rooms");
            }

            return View(room);
        }

        public ActionResult EditRoom(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            Room room = db.Rooms.Find(id);

            if (room == null)
                return HttpNotFound();

            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditRoom(Room room, HttpPostedFileBase photoFile)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            Room existing = db.Rooms.Find(room.RoomID);

            if (existing == null)
                return HttpNotFound();

            existing.RoomNumber = room.RoomNumber;
            existing.MonthlyRent = room.MonthlyRent;
            existing.Description = room.Description;
            existing.Status = room.Status;

            if (photoFile != null && photoFile.ContentLength > 0)
            {
                string extension =
                    System.IO.Path.GetExtension(photoFile.FileName);

                string fileName =
                    Guid.NewGuid() + extension;

                string folder =
                    Server.MapPath("~/Content/RoomImages");

                if (!System.IO.Directory.Exists(folder))
                    System.IO.Directory.CreateDirectory(folder);

                photoFile.SaveAs(
                    System.IO.Path.Combine(folder, fileName));

                existing.Photo = fileName;
            }

            db.SaveChanges();

            TempData["SuccessMessage"] =
                "Room updated successfully.";

            return RedirectToAction("Rooms");
        }

        public ActionResult DeleteRoom(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            Room room = db.Rooms.Find(id);

            if (room == null)
                return HttpNotFound();

            return View(room);
        }

        [HttpPost, ActionName("DeleteRoom")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteRoomConfirmed(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            Room room = db.Rooms.Find(id);

            if (room != null)
            {
                db.Rooms.Remove(room);
                db.SaveChanges();
            }

            return RedirectToAction("Rooms");
        }

        // ==========================
        // PAYMENTS
        // ==========================
        public ActionResult Payments()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var payments = db.Payments
                .OrderByDescending(p => p.PaymentDate)
                .ToList();

            ViewBag.TotalRevenue = payments
                .Where(p => p.Status == "Paid")
                .Sum(p => p.Amount ?? 0);

            ViewBag.PendingCount = payments
                .Count(p => p.Status == "Pending");

            ViewBag.PaidCount = payments
                .Count(p => p.Status == "Paid");

            ViewBag.CancelledCount = payments
                .Count(p => p.Status == "Cancelled");

            return View(payments);
        }

        public ActionResult VerifyPayment(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            Payment payment = db.Payments.Find(id);

            if (payment == null)
                return HttpNotFound();

            payment.Status = "Processed";
            payment.VerifiedBy = (int)Session["UserID"];

            db.SaveChanges();

            NotificationHelper.CreateNotification(
                db,
                payment.UserID,
                "💳 Your payment has been verified successfully.");

            return RedirectToAction("Payments");
        }

        // ==========================
        // MAINTENANCE
        // ==========================
        public ActionResult Maintenance()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            return View(db.MaintenanceRequests
                          .OrderByDescending(m => m.DateReported)
                          .ToList());
        }

        public ActionResult StartMaintenance(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var request = db.MaintenanceRequests.Find(id);

            if (request == null)
                return HttpNotFound();

            request.Status = "In Progress";

            db.SaveChanges();

            return RedirectToAction("Maintenance");
        }

        public ActionResult CompleteMaintenance(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var request = db.MaintenanceRequests.Find(id);

            if (request == null)
                return HttpNotFound();

            request.Status = "Completed";

            db.SaveChanges();

            return RedirectToAction("Maintenance");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}