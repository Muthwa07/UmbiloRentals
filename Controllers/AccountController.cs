using System;
using System.Linq;
using System.Web.Mvc;
using UmbiloRentals.Helpers;
using UmbiloRentals.Models;
using System.Net.Mail;
using System.Security.Cryptography;

namespace UmbiloRentals.Controllers
{
    public class AccountController : BaseController
    {
        private readonly BuildingManagementDBEntities db =
            new BuildingManagementDBEntities();

        private static string NormalizeEmail(string email)
        {
            return (email ?? string.Empty).Trim();
        }

        private static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;

            var digits = phone
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("+", "");

            if (digits.StartsWith("27", StringComparison.Ordinal))
                digits = digits.Substring(2);

            if (digits.StartsWith("0", StringComparison.Ordinal))
                digits = digits.Substring(1);

            return digits;
        }

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(
            string firstName,
            string lastName,
            string email,
            string phone,
            string password,
            string ConfirmPassword,
            bool consentGiven)
        {
            string normalizedEmail = NormalizeEmail(email);
            string normalizedPhone = NormalizePhone(phone);

            User model = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = normalizedEmail,
                Phone = normalizedPhone
            };

            if (string.IsNullOrWhiteSpace(firstName) ||
                !System.Text.RegularExpressions.Regex.IsMatch(firstName, @"^[A-Za-z\s]+$"))
            {
                ModelState.AddModelError("", "First name may contain letters only.");
            }

            if (string.IsNullOrWhiteSpace(lastName) ||
                !System.Text.RegularExpressions.Regex.IsMatch(lastName, @"^[A-Za-z\s]+$"))
            {
                ModelState.AddModelError("", "Last name may contain letters only.");
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(normalizedPhone, @"^[0-9]{9}$"))
            {
                ModelState.AddModelError("", "Enter a valid South African phone number.");
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            {
                ModelState.AddModelError("", "Password must be at least 8 characters long.");
            }

            if (password != ConfirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
            }

            if (db.Users.Any(u => u.Email != null && u.Email.ToLower() == normalizedEmail.ToLower()))
            {
                ModelState.AddModelError("", "An account with this email already exists.");
            }

            if (!consentGiven)
            {
                ModelState.AddModelError("", "You must agree to the Privacy Policy and Terms & Conditions to create an account.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            User user = new User
            {
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                Email = normalizedEmail,
                Phone = "+27" + normalizedPhone,
                Password = PasswordHelper.HashPassword(password),
                RoleID = 1,
                Status = "Active",
                DateCreated = DateTime.Now
            };

            db.Users.Add(user);
            db.SaveChanges();

            NotificationHelper.CreateNotification(db, user.UserID, "Welcome to Umbilo Rentals! Your account has been created successfully.");

            TempData["SuccessMessage"] = "Account created successfully. You can now log in.";
            return RedirectToAction("Login");
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password)
        {
            string normalizedEmail = NormalizeEmail(email);

            User user = db.Users.FirstOrDefault(
                u => u.Email != null &&
                     u.Email.ToLower() == normalizedEmail.ToLower() &&
                     u.Status == "Active");

            if (user == null || !PasswordHelper.VerifyPassword(password, user.Password))
            {
                ViewBag.ErrorMessage = "Invalid email or password.";
                return View();
            }

            if (PasswordHelper.IsLegacyPlaintext(user.Password))
            {
                user.Password = PasswordHelper.HashPassword(password);
                db.SaveChanges();
            }

            Session["UserID"] = user.UserID;
            Session["UserName"] = user.FirstName + " " + user.LastName;
            Session["RoleID"] = user.RoleID;

            if (user.RoleID == 2)
                return RedirectToAction("Index", "Admin");

            if (user.RoleID == 3)
                return RedirectToAction("Index", "Maintenance");

            if (user.RoleID == 4)
                return RedirectToAction("Visitors", "Security");

            return RedirectToAction("Dashboard", "Account");
        }

        private string GenerateResetToken()
        {
            byte[] tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }

            return Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        private string GenerateResetCode()
        {
            byte[] bytes = new byte[4];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            uint value = BitConverter.ToUInt32(bytes, 0);
            return (value % 1000000).ToString("D6");
        }

        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(string email)
        {
            string normalizedEmail = NormalizeEmail(email);

            var user = db.Users.FirstOrDefault(u =>
                u.Email != null &&
                u.Email.ToLower() == normalizedEmail.ToLower());

            if (user == null)
            {
                TempData["SuccessMessage"] = "If that email exists, a reset code has been sent.";
                return RedirectToAction("Login");
            }

            string code = GenerateResetCode();
            user.ResetCode = code;
            user.ResetCodeExpiry = DateTime.Now.AddMinutes(2);
            db.SaveChanges();

            bool emailSent = false;

            try
            {
                MailMessage message = new MailMessage();
                message.To.Add(user.Email);
                message.Subject = "Your Umbilo Rentals reset code";
                message.Body =
                    "Your password reset code is: " + code + "\n\n" +
                    "This code expires in 2 minutes. If you didn't request this, you can safely ignore this email.";

                SmtpClient smtp = new SmtpClient();
                smtp.Send(message);
                emailSent = true;
            }
            catch
            {
                // Intentionally swallow SMTP failures so the app does not expose email server details.
            }

            TempData["ResetEmail"] = normalizedEmail;

            if (emailSent)
            {
                TempData["SuccessMessage"] = "If that email exists, a 6-digit code has been sent. It expires in 2 minutes.";
            }
            else
            {
                TempData["ErrorMessage"] = "Password reset email is currently unavailable. Please contact support or try again shortly.";
            }

            return RedirectToAction("ResetPassword");
        }

        public ActionResult ResetPassword()
        {
            string email = TempData["ResetEmail"] as string;

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Please request a reset code first.";
                return RedirectToAction("ForgotPassword");
            }

            TempData.Keep("ResetEmail");
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(string email, string code, string password, string confirmPassword)
        {
            string normalizedEmail = NormalizeEmail(email);

            if (password != confirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match.";
                TempData["ResetEmail"] = normalizedEmail;
                return RedirectToAction("ResetPassword");
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            {
                TempData["ErrorMessage"] = "Password must be at least 8 characters long.";
                TempData["ResetEmail"] = normalizedEmail;
                return RedirectToAction("ResetPassword");
            }

            var user = db.Users.FirstOrDefault(u =>
                u.Email != null &&
                u.Email.ToLower() == normalizedEmail.ToLower() &&
                u.ResetCode == code &&
                u.ResetCodeExpiry > DateTime.Now);

            if (user == null)
            {
                TempData["ErrorMessage"] = "That code is invalid or has expired. Please request a new one.";
                TempData["ResetEmail"] = normalizedEmail;
                return RedirectToAction("ResetPassword");
            }

            user.Password = PasswordHelper.HashPassword(password);
            user.ResetCode = null;
            user.ResetCodeExpiry = null;
            db.SaveChanges();

            TempData["SuccessMessage"] = "Password reset successfully. You can now log in.";
            return RedirectToAction("Login");
        }

        public ActionResult Dashboard()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login");

            int userId = (int)Session["UserID"];

            var applications = db.Applications
                                 .Where(a => a.UserID == userId)
                                 .ToList();

            ViewBag.TotalApplications = applications.Count;
            ViewBag.PendingApplications = applications.Count(a => a.Status == "Pending");
            ViewBag.AvailableRooms = db.Rooms.Count(r => r.Status == "Available");

            var approved = applications.FirstOrDefault(a => a.Status == "Approved");
            ViewBag.IsTenant = false;

            if (approved != null)
            {
                var room = db.Rooms.Find(approved.RoomID);

                if (room != null)
                {
                    ViewBag.IsTenant = true;
                    ViewBag.CurrentRoom = room.RoomNumber;
                    ViewBag.CurrentRent = room.MonthlyRent;
                    ViewBag.RoomStatus = "Allocated";
                    ViewBag.MoveInDate = approved.DateApplied;
                    ViewBag.ApprovedApplicationID = approved.ApplicationID;

                    string currentMonth = DateTime.Now.ToString("yyyy-MM");

                    bool paidThisMonth = db.Payments.Any(p =>
                        p.UserID == userId &&
                        p.RoomID == room.RoomID &&
                        p.PaymentMonth == currentMonth &&
                        p.Status == "Paid");

                    bool pendingThisMonth = db.Payments.Any(p =>
                        p.UserID == userId &&
                        p.RoomID == room.RoomID &&
                        p.PaymentMonth == currentMonth &&
                        p.Status == "Pending");

                    ViewBag.RentPaidThisMonth = paidThisMonth;
                    ViewBag.RentPendingThisMonth = pendingThisMonth;
                }
            }
            else
            {
                var latestApplication = applications
                    .OrderByDescending(a => a.DateApplied)
                    .FirstOrDefault();

                ViewBag.LatestApplicationStatus = latestApplication?.Status ?? "Pending";
                ViewBag.HasApplication = latestApplication != null;
            }

            var payment = db.Payments
                            .Where(p => p.UserID == userId)
                            .OrderByDescending(p => p.PaymentDate)
                            .FirstOrDefault();

            ViewBag.PaymentStatus = payment != null ? payment.Status : "No Payment";
            return View();
        }

        [ActionName("Profile")]
        public ActionResult MyProfile()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login");

            int userId = (int)Session["UserID"];
            User user = db.Users.Find(userId);

            if (user == null)
                return HttpNotFound();

            return View("Profile", user);
        }

        [ActionName("MyProfile")]
        public ActionResult MyProfileRedirect()
        {
            return RedirectToActionPermanent("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Profile")]
        public ActionResult MyProfilePost(User model)
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login");

            int userId = (int)Session["UserID"];
            User user = db.Users.Find(userId);

            if (user == null)
                return HttpNotFound();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = NormalizeEmail(model.Email);
            user.Phone = "+27" + NormalizePhone(model.Phone);
            user.Occupation = model.Occupation;
            user.EmergencyContactName = model.EmergencyContactName;
            user.EmergencyContactPhone = model.EmergencyContactPhone;
            user.SleepSchedule = model.SleepSchedule;
            user.CleanlinessLevel = model.CleanlinessLevel;
            user.NoiseTolerance = model.NoiseTolerance;
            user.IsSmoker = model.IsSmoker;
            user.StudyHabit = model.StudyHabit;
            user.GuestsPreference = model.GuestsPreference;

            db.SaveChanges();
            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction("Profile");
        }

        public ActionResult MoveOut()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login");

            int userId = (int)Session["UserID"];
            var application = db.Applications
                                .FirstOrDefault(a =>
                                    a.UserID == userId &&
                                    a.Status == "Approved");

            if (application == null)
            {
                TempData["ErrorMessage"] = "You do not currently have an allocated room.";
                return RedirectToAction("Dashboard");
            }

            var room = db.Rooms.Find(application.RoomID);
            ViewBag.RoomNumber = room?.RoomNumber;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("MoveOut")]
        public ActionResult MoveOutConfirmed()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login");

            int userId = (int)Session["UserID"];

            var application = db.Applications
                                .FirstOrDefault(a =>
                                    a.UserID == userId &&
                                    a.Status == "Approved");

            if (application == null)
                return RedirectToAction("Dashboard");

            var room = db.Rooms.Find(application.RoomID);
            if (room != null)
                room.Status = "Available";

            application.Status = "Moved Out";

            var allocation = db.Allocations
                .Where(a =>
                    a.UserID == userId &&
                    a.RoomID == application.RoomID &&
                    a.Status == "Active")
                .OrderByDescending(a => a.AllocationDate)
                .FirstOrDefault();

            if (allocation != null)
            {
                allocation.MoveOutDate = DateTime.Now;
                allocation.Status = "Completed";
            }
            else
            {
                db.Allocations.Add(new Allocation
                {
                    UserID = userId,
                    RoomID = application.RoomID.Value,
                    AllocationDate = application.DateApplied ?? DateTime.Now,
                    MoveOutDate = DateTime.Now,
                    Status = "Completed"
                });
            }

            NotificationHelper.CreateNotification(db, userId, "You have successfully moved out.");
            db.SaveChanges();

            TempData["SuccessMessage"] = "You have successfully moved out and your room is now available. Please take a moment to leave a review for other students.";
            return RedirectToAction("Create", "Reviews", new { roomId = application.RoomID });
        }

        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Home");
        }
    }
}
