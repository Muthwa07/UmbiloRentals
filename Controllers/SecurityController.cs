using System;
using System.Linq;
using System.Web.Mvc;
using UmbiloRentals.Helpers;
using UmbiloRentals.Models;

namespace UmbiloRentals.Controllers
{
    public class SecurityController : BaseController
    {
        private bool IsSecurity()
        {
            return Session["UserID"] != null &&
                   Session["RoleID"] != null &&
                   (int)Session["RoleID"] == 4;
        }

        private bool IsTenant()
        {
            return Session["UserID"] != null &&
                   Session["RoleID"] != null &&
                   (int)Session["RoleID"] == 1;
        }

        // =========================================================
        // SECURITY STAFF - VISITORS
        // =========================================================

        public ActionResult Visitors()
        {
            if (!IsSecurity())
                return RedirectToAction("Login", "Account");

            var visitors = db.VisitorRequests
                .OrderBy(v => v.Status == "Expected" ? 0 : 1)
                .ThenBy(v => v.VisitDate)
                .ThenBy(v => v.ArrivalTime)
                .ToList();

            return View(visitors);
        }

        public ActionResult CheckInVisitor(int id)
        {
            if (!IsSecurity())
                return RedirectToAction("Login", "Account");

            var visitor = db.VisitorRequests.Find(id);

            if (visitor == null)
                return HttpNotFound();

            visitor.Status = "Checked In";
            visitor.CheckInTime = DateTime.Now;

            db.SaveChanges();

            NotificationHelper.CreateNotification(
                db,
                visitor.TenantID,
                visitor.VisitorName + " has checked in.");

            TempData["SuccessMessage"] = visitor.VisitorName + " checked in.";

            return RedirectToAction("Visitors");
        }

        public ActionResult CheckOutVisitor(int id)
        {
            if (!IsSecurity())
                return RedirectToAction("Login", "Account");

            var visitor = db.VisitorRequests.Find(id);

            if (visitor == null)
                return HttpNotFound();

            visitor.Status = "Checked Out";
            visitor.CheckOutTime = DateTime.Now;

            db.SaveChanges();

            TempData["SuccessMessage"] = visitor.VisitorName + " checked out.";

            return RedirectToAction("Visitors");
        }

        // =========================================================
        // SECURITY STAFF - COMPLAINTS (minor security issues)
        // =========================================================

        public ActionResult Complaints()
        {
            if (!IsSecurity())
                return RedirectToAction("Login", "Account");

            var complaints = db.Complaints
                .OrderBy(c => c.Status == "Open" ? 0 : 1)
                .ThenByDescending(c => c.DateSubmitted)
                .ToList();

            return View(complaints);
        }

        public ActionResult ResolveComplaint(int id)
        {
            if (!IsSecurity())
                return RedirectToAction("Login", "Account");

            var complaint = db.Complaints.Find(id);

            if (complaint == null)
                return HttpNotFound();

            complaint.Status = "Resolved";

            db.SaveChanges();

            if (complaint.TenantID.HasValue)
            {
                NotificationHelper.CreateNotification(
                    db,
                    complaint.TenantID.Value,
                    "Your reported issue has been resolved.");
            }

            TempData["SuccessMessage"] = "Marked as resolved.";

            return RedirectToAction("Complaints");
        }

        // =========================================================
        // SECURITY STAFF - LOST & FOUND
        // =========================================================

        public ActionResult LostFoundBoard()
        {
            if (!IsSecurity())
                return RedirectToAction("Login", "Account");

            var items = db.LostFounds
                .OrderByDescending(i => i.DateFound)
                .ToList();

            return View(items);
        }

        public ActionResult LogFoundItem()
        {
            if (!IsSecurity())
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogFoundItem(string itemName, string description)
        {
            if (!IsSecurity())
                return RedirectToAction("Login", "Account");

            int securityUserId = (int)Session["UserID"];

            db.LostFounds.Add(new LostFound
            {
                ItemName = itemName,
                Description = description,
                DateFound = DateTime.Now,
                ReportedBy = securityUserId,
                Status = "Found - Unclaimed"
            });

            db.SaveChanges();

            TempData["SuccessMessage"] = "Item logged.";

            return RedirectToAction("LostFoundBoard");
        }

        public ActionResult ResolveLostFoundItem(int id)
        {
            if (!IsSecurity())
                return RedirectToAction("Login", "Account");

            var item = db.LostFounds.Find(id);

            if (item == null)
                return HttpNotFound();

            item.Status = "Resolved";

            db.SaveChanges();

            NotificationHelper.CreateNotification(
                db,
                item.ReportedBy,
                "Your lost & found report for \"" + item.ItemName + "\" has been resolved.");

            TempData["SuccessMessage"] = "Marked as resolved.";

            return RedirectToAction("LostFoundBoard");
        }

        // =========================================================
        // TENANT - SCHEDULE A VISITOR
        // =========================================================

        public ActionResult ScheduleVisitor()
        {
            if (!IsTenant())
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ScheduleVisitor(
            string visitorName,
            string phone,
            DateTime visitDate,
            string arrivalTime)
        {
            if (!IsTenant())
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(visitorName))
            {
                ViewBag.ErrorMessage = "Visitor name is required.";
                return View();
            }

            int tenantId = (int)Session["UserID"];

            TimeSpan? parsedArrival = null;

            if (TimeSpan.TryParse(arrivalTime, out TimeSpan arrival))
            {
                parsedArrival = arrival;
            }

            db.VisitorRequests.Add(new VisitorRequest
            {
                TenantID = tenantId,
                VisitorName = visitorName,
                Phone = phone,
                VisitDate = visitDate,
                ArrivalTime = parsedArrival,
                Status = "Expected"
            });

            db.SaveChanges();

            TempData["SuccessMessage"] =
                "Visitor scheduled. Security will be expecting them.";

            return RedirectToAction("MyVisitors");
        }

        public ActionResult MyVisitors()
        {
            if (!IsTenant())
                return RedirectToAction("Login", "Account");

            int tenantId = (int)Session["UserID"];

            var visitors = db.VisitorRequests
                .Where(v => v.TenantID == tenantId)
                .OrderByDescending(v => v.VisitDate)
                .ToList();

            return View(visitors);
        }

        // =========================================================
        // TENANT - REPORT A MINOR ISSUE (complaint)
        // =========================================================

        public ActionResult ReportComplaint()
        {
            if (!IsTenant())
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReportComplaint(string complaintType, string description)
        {
            if (!IsTenant())
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(description))
            {
                ViewBag.ErrorMessage = "Please describe the issue.";
                return View();
            }

            int tenantId = (int)Session["UserID"];

            db.Complaints.Add(new Complaint
            {
                TenantID = tenantId,
                ComplaintType = complaintType,
                Description = description,
                Status = "Open",
                DateSubmitted = DateTime.Now
            });

            db.SaveChanges();

            TempData["SuccessMessage"] =
                "Issue reported. Security will review it.";

            return RedirectToAction("MyComplaints");
        }

        public ActionResult MyComplaints()
        {
            if (!IsTenant())
                return RedirectToAction("Login", "Account");

            int tenantId = (int)Session["UserID"];

            var complaints = db.Complaints
                .Where(c => c.TenantID == tenantId)
                .OrderByDescending(c => c.DateSubmitted)
                .ToList();

            return View(complaints);
        }

        // =========================================================
        // TENANT - LOST & FOUND
        // =========================================================

        public ActionResult LostFoundTenant()
        {
            if (!IsTenant())
                return RedirectToAction("Login", "Account");

            var items = db.LostFounds
                .OrderByDescending(i => i.DateFound)
                .ToList();

            return View(items);
        }

        public ActionResult ReportLostItem()
        {
            if (!IsTenant())
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReportLostItem(string itemName, string description)
        {
            if (!IsTenant())
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(itemName))
            {
                ViewBag.ErrorMessage = "Please describe what you lost.";
                return View();
            }

            int tenantId = (int)Session["UserID"];

            db.LostFounds.Add(new LostFound
            {
                ItemName = itemName,
                Description = description,
                DateFound = null,
                ReportedBy = tenantId,
                Status = "Lost - Searching"
            });

            db.SaveChanges();

            TempData["SuccessMessage"] =
                "Reported. We'll let you know if it turns up.";

            return RedirectToAction("LostFoundTenant");
        }
    }
}
