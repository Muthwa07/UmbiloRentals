using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UmbiloRentals.Models;

namespace UmbiloRentals.Controllers
{
    public class PaymentsController : Controller
    {
        private BuildingManagementDBEntities db =
            new BuildingManagementDBEntities();

        // Applicant payment history
        public ActionResult Index()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];

            var payments = db.Payments
                .Where(p => p.UserID == userId)
                .OrderByDescending(p => p.PaymentDate)
                .ToList();

            return View(payments);
        }

        // Payment form
        public ActionResult Create()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];

            var room = db.Applications
                .Where(a => a.UserID == userId && a.Status == "Approved")
                .OrderByDescending(a => a.DateApplied)
                .Select(a => a.RoomID)
                .FirstOrDefault();

            if (room == 0)
            {
                TempData["Error"] =
                    "You must have an approved room before making payments.";

                return RedirectToAction("Index", "Dashboard");
            }

            var roomDetails = db.Rooms.Find(room);

            ViewBag.RoomID = roomDetails.RoomID;
            ViewBag.RoomNumber = roomDetails.RoomNumber;
            ViewBag.Amount = roomDetails.MonthlyRent;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            int roomID,
            decimal amount,
            string paymentMonth,
            HttpPostedFileBase proof)
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            if (proof == null || proof.ContentLength == 0)
            {
                TempData["Error"] = "Please upload proof of payment.";
                return RedirectToAction("Create");
            }

            string extension =
                Path.GetExtension(proof.FileName).ToLower();

            string[] allowed ={
                ".pdf",".jpg",".jpeg",".png"
            };

            if (!allowed.Contains(extension))
            {
                TempData["Error"] = "Only PDF or image files are allowed.";
                return RedirectToAction("Create");
            }

            if (proof.ContentLength > 5 * 1024 * 1024)
            {
                TempData["Error"] = "Maximum file size is 5 MB.";
                return RedirectToAction("Create");
            }

            string folder = Server.MapPath("~/UploadedPayments");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string fileName =
                Guid.NewGuid() + extension;

            proof.SaveAs(Path.Combine(folder, fileName));

            Payment payment = new Payment();

            payment.UserID = (int)Session["UserID"];
            payment.RoomID = roomID;
            payment.Amount = amount;
            payment.PaymentMonth = paymentMonth;
            payment.ProofOfPayment = "~/UploadedPayments/" + fileName;
            payment.PaymentDate = DateTime.Now;
            payment.Status = "Pending";

            db.Payments.Add(payment);
            db.SaveChanges();

            TempData["Success"] = "Payment submitted successfully.";

            return RedirectToAction("Index");
        }
    }
}