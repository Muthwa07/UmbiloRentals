using System;
using System.Linq;
using System.Web.Mvc;
using UmbiloRentals.Helpers;
using UmbiloRentals.Models;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;

namespace UmbiloRentals.Controllers
{
    public class PaymentsController : Controller
    {
        private BuildingManagementDBEntities db =
            new BuildingManagementDBEntities();

        public ActionResult PayNow(int applicationId)
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];

            var application = db.Applications.FirstOrDefault(a =>
                a.ApplicationID == applicationId &&
                a.UserID == userId &&
                a.Status == "Approved");

            if (application == null)
                return HttpNotFound();

            var room = db.Rooms.Find(application.RoomID);
            var user = db.Users.Find(userId);

            if (room == null || user == null)
                return HttpNotFound();

            decimal amount = room.MonthlyRent ?? 0;
            string currentMonth = DateTime.Now.ToString("yyyy-MM");

            bool alreadyPaidThisMonth = db.Payments.Any(p =>
                p.UserID == user.UserID &&
                p.RoomID == room.RoomID &&
                p.PaymentMonth == currentMonth &&
                p.Status == "Paid");

            if (alreadyPaidThisMonth)
            {
                TempData["SuccessMessage"] = "You've already paid rent for this month.";
                return RedirectToAction("History");
            }

            var existingPayment = db.Payments.FirstOrDefault(p =>
                p.UserID == user.UserID &&
                p.RoomID == room.RoomID &&
                p.PaymentMonth == currentMonth &&
                p.Status == "Pending");

            string reference;

            if (existingPayment != null)
            {
                reference = existingPayment.TransactionReference;
            }
            else
            {
                reference = string.Format(
                    "UR-{0}-{1:yyyyMMddHHmmss}",
                    application.ApplicationID,
                    DateTime.Now);

                Payment payment = new Payment
                {
                    UserID = user.UserID,
                    RoomID = room.RoomID,
                    Amount = amount,
                    PaymentDate = DateTime.Now,
                    PaymentMonth = currentMonth,
                    Status = "Pending",
                    Gateway = "PayFast",
                    TransactionReference = reference,
                    ProofOfPayment = null,
                    VerifiedBy = null
                };

                db.Payments.Add(payment);
                db.SaveChanges();
            }

            var paymentData = PaymentService.CreatePaymentData(
                reference,
                amount,
                "Umbilo Rentals Reservation",
                user.FirstName,
                user.LastName,
                user.Email);

            var model = new UmbiloRentals.ViewModels.PaymentCheckoutViewModel
            {
                ApplicationID = application.ApplicationID,
                ApplicantName = user.FirstName + " " + user.LastName,
                RoomNumber = room.RoomNumber,
                Amount = amount,
                Reference = reference,
                CheckoutUrl = PaymentService.GetPayFastUrl() + "?" +
                              PaymentService.BuildQueryString(paymentData)
            };

            return View(model);
        }

        public ActionResult History()
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

        public ActionResult Success()
        {
            ViewBag.Message = "Your payment was received. We are confirming it.";
            return View();
        }

        public ActionResult Cancel()
        {
            ViewBag.Message = "Your payment was cancelled.";
            return View();
        }

        [HttpPost]
        public ActionResult ITN()
        {
            NameValueCollection form = Request.Form;

            if (form.Count == 0)
                return new HttpStatusCodeResult(400);

            string paymentStatus = form["payment_status"];
            string paymentId = form["m_payment_id"];
            string pfPaymentId = form["pf_payment_id"];

            if (string.IsNullOrEmpty(paymentId))
                return new HttpStatusCodeResult(400);

            bool verified = VerifyWithPayFast(form);

            if (!verified)
                return new HttpStatusCodeResult(400);

            if (paymentStatus == "COMPLETE")
            {
                string[] parts = paymentId.Split('-');

                if (parts.Length >= 2)
                {
                    int applicationId;

                    if (int.TryParse(parts[1], out applicationId))
                    {
                        var application = db.Applications.Find(applicationId);

                        if (application != null)
                        {
                            var room = db.Rooms.Find(application.RoomID);
                            var payment = db.Payments.FirstOrDefault(p =>
                                p.TransactionReference == paymentId &&
                                p.UserID == application.UserID &&
                                p.RoomID == application.RoomID);

                            if (payment != null && payment.Status != "Paid")
                            {
                                payment.Status = "Paid";
                                payment.PaymentDate = DateTime.Now;
                                payment.PayFastPaymentID = pfPaymentId;

                                if (room != null)
                                    room.Status = "Occupied";

                                if (application.UserID.HasValue)
                                {
                                    NotificationHelper.CreateNotification(
                                        db,
                                        application.UserID.Value,
                                        "Payment received successfully. Your room has now been officially allocated.");
                                }

                                db.SaveChanges();
                            }
                        }
                    }
                }
            }

            return new HttpStatusCodeResult(200);
        }

        private bool VerifyWithPayFast(NameValueCollection form)
        {
            StringBuilder builder = new StringBuilder();

            foreach (string key in form.AllKeys)
            {
                if (key != "signature")
                {
                    builder.Append(key)
                           .Append("=")
                           .Append(Uri.EscapeDataString(form[key]))
                           .Append("&");
                }
            }

            string passphrase =
                ConfigurationManager.AppSettings["PayFastPassphrase"];

            if (!string.IsNullOrEmpty(passphrase))
            {
                builder.Append("passphrase=")
                       .Append(Uri.EscapeDataString(passphrase));
            }
            else if (builder.Length > 0)
            {
                builder.Length--;
            }

            string verifyUrl =
                ConfigurationManager.AppSettings["PayFastMode"] == "Live"
                ? "https://www.payfast.co.za/eng/query/validate"
                : "https://sandbox.payfast.co.za/eng/query/validate";

            HttpWebRequest request =
                (HttpWebRequest)WebRequest.Create(verifyUrl);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            byte[] bytes = Encoding.UTF8.GetBytes(builder.ToString());

            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
            }

            using (HttpWebResponse response =
                   (HttpWebResponse)request.GetResponse())
            using (StreamReader reader =
                   new StreamReader(response.GetResponseStream()))
            {
                string result = reader.ReadToEnd();
                return string.Equals(result.Trim(), "VALID", StringComparison.OrdinalIgnoreCase);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
