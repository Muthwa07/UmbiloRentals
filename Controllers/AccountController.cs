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

        // GET: Account/Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
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
            // Keep entered values if validation fails
            User model = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Phone = phone
            };

            // First name validation
            if (!System.Text.RegularExpressions.Regex.IsMatch(
                firstName ?? "",
                @"^[A-Za-z\s]+$"))
            {
                ModelState.AddModelError(
                    "",
                    "First name may contain letters only.");
            }

            // Last name validation
            if (!System.Text.RegularExpressions.Regex.IsMatch(
                lastName ?? "",
                @"^[A-Za-z\s]+$"))
            {
                ModelState.AddModelError(
                    "",
                    "Last name may contain letters only.");
            }

            // South African phone validation
            string cleanPhone = (phone ?? "").Replace(" ", "");

            if (!System.Text.RegularExpressions.Regex.IsMatch(
                cleanPhone,
                @"^[0-9]{9}$"))
            {
                ModelState.AddModelError(
                    "",
                    "Enter a valid South African phone number.");
            }

            // Confirm password
            if (password != ConfirmPassword)
            {
                ModelState.AddModelError(
                    "",
                    "Passwords do not match.");
            }

            // Email exists
            if (db.Users.Any(u => u.Email == email))
            {
                ModelState.AddModelError(
                    "",
                    "An account with this email already exists.");
            }

            // Privacy consent must be given
            if (!consentGiven)
            {
                ModelState.AddModelError(
                    "",
                    "You must agree to the Privacy Policy and Terms & Conditions to create an account.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            User user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Phone = "+27" + phone,
                Password = PasswordHelper.HashPassword(password),
                RoleID = 1,
                Status = "Active",
                DateCreated = DateTime.Now
            };

            db.Users.Add(user);
            db.SaveChanges();

            NotificationHelper.CreateNotification(
                db,
                user.UserID,
                "Welcome to Umbilo Rentals! Your account has been created successfully.");

            TempData["SuccessMessage"] =
                "Account created successfully. You can now log in.";

            return RedirectToAction("Login");
        }

        // GET: Account/Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password)
        {
            User user = db.Users.FirstOrDefault(
                u => u.Email == email &&
                     u.Status == "Active");

            if (user == null || !PasswordHelper.VerifyPassword(password, user.Password))
            {
                ViewBag.ErrorMessage = "Invalid email or password.";
                return View();
            }

            // Silently migrate legacy plaintext passwords to a proper
            // hash now that we know the password is correct.
            if (PasswordHelper.IsLegacyPlaintext(user.Password))
            {
                user.Password = PasswordHelper.HashPassword(password);
                db.SaveChanges();
            }

            // Store logged-in user's details
            Session["UserID"] = user.UserID;
            Session["UserName"] = user.FirstName + " " + user.LastName;
            Session["RoleID"] = user.RoleID;

            // Admin goes to Admin Dashboard
            if (user.RoleID == 2)
            {
                return RedirectToAction("Index", "Admin");
            }

            if (user.RoleID == 3)
            {
                return RedirectToAction("Index", "Maintenance");
            }

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

        // Cryptographically random 6-digit code, e.g. "042951"
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
            var user = db.Users.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                TempData["SuccessMessage"] =
                    "If that email exists, a reset code has been sent.";

                return RedirectToAction("Login");
            }

            string code = GenerateResetCode();

            user.ResetCode = code;
            user.ResetCodeExpiry = DateTime.Now.AddMinutes(2);

            db.SaveChanges();

            try
            {
                MailMessage message = new MailMessage();
                message.To.Add(user.Email);
                message.Subject = "Your Umbilo Rentals reset code";
                message.Body =
                    "Your password reset code is: " + code + "\n\n" +
                    "This code expires in 2 minutes. If you didn't request " +
                    "this, you can safely ignore this email.";

                SmtpClient smtp = new SmtpClient();
                smtp.Send(message);
            }
            catch
            {
                // Prevent exposing email errors to users.
            }

            TempData["ResetEmail"] = email;

            TempData["SuccessMessage"] =
                "If that email exists, a 6-digit code has been sent. " +
                "It expires in 2 minutes.";

            return RedirectToAction("ResetPassword");
        }

        // GET: Account/ResetPassword
        public ActionResult ResetPassword()
        {
            string email = TempData["ResetEmail"] as string;

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] =
                    "Please request a reset code first.";

                return RedirectToAction("ForgotPassword");
            }

            // Keep it alive so it survives the POST back on this same page
            TempData.Keep("ResetEmail");

            ViewBag.Email = email;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(string email, string code, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match.";
                TempData["ResetEmail"] = email;

                return RedirectToAction("ResetPassword");
            }

            var user = db.Users.FirstOrDefault(u =>
                u.Email == email &&
                u.ResetCode == code &&
                u.ResetCodeExpiry > DateTime.Now);

            if (user == null)
            {
                TempData["ErrorMessage"] =
                    "That code is invalid or has expired. Please request a new one.";

                TempData["ResetEmail"] = email;

                return RedirectToAction("ResetPassword");
            }

            user.Password = PasswordHelper.HashPassword(password);

            // Invalidate the code so it can't be reused
            user.ResetCode = null;
            user.ResetCodeExpiry = null;

            db.SaveChanges();

            TempData["SuccessMessage"] =
                "Password reset successfully. You can now log in.";

            return RedirectToAction("Login");
        }

        // GET: Account/Dashboard
        public ActionResult Dashboard()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login");

            int userId = (int)Session["UserID"];

            var applications = db.Applications
                                 .Where(a => a.UserID == userId)
                                 .ToList();

            ViewBag.TotalApplications = applications.Count;
            ViewBag.PendingApplications =
                applications.Count(a => a.Status == "Pending");

            ViewBag.AvailableRooms =
                db.Rooms.Count(r => r.Status == "Available");

            // -----------------------------
            // TENANT MODE
            // -----------------------------

            var approved = applications
                .FirstOrDefault(a => a.Status == "Approved");

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
                // Not a tenant yet - show progress for their most recent application
                var latestApplication = applications
                    .OrderByDescending(a => a.DateApplied)
                    .FirstOrDefault();

                ViewBag.LatestApplicationStatus =
                    latestApplication?.Status ?? "Pending";

                ViewBag.HasApplication = latestApplication != null;
            }

            var payment = db.Payments
                            .Where(p => p.UserID == userId)
                            .OrderByDescending(p => p.PaymentDate)
                            .FirstOrDefault();

            ViewBag.PaymentStatus =
                payment != null ? payment.Status : "No Payment";

            return View();
        }

        // GET: Account/Profile
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

        // GET: Account/MyProfile - old URL, kept as a redirect so
        // existing bookmarks/links don't 404
        [ActionName("MyProfile")]
        public ActionResult MyProfileRedirect()
        {
            return RedirectToActionPermanent("Profile");
        }

        // POST: Account/Profile
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
            user.Email = model.Email;
            user.Phone = model.Phone;
            user.Occupation = model.Occupation;
            user.EmergencyContactName = model.EmergencyContactName;
            user.EmergencyContactPhone = model.EmergencyContactPhone;

            // Roommate compatibility profile
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

        // ==========================================
        // GET: Account/MoveOut
        // ==========================================
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
                TempData["ErrorMessage"] =
                    "You do not currently have an allocated room.";

                return RedirectToAction("Dashboard");
            }

            var room = db.Rooms.Find(application.RoomID);

            ViewBag.RoomNumber = room?.RoomNumber;

            return View();
        }


        // ==========================================
        // POST: Account/MoveOut
        // ==========================================
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
            {
                room.Status = "Available";
            }

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
                // No Allocation record exists for this stay (most likely
                // this application was approved before allocation
                // tracking was added). Create one now, retroactively,
                // so the tenant can still leave a review.
                db.Allocations.Add(new Allocation
                {
                    UserID = userId,
                    RoomID = application.RoomID.Value,
                    AllocationDate = application.DateApplied ?? DateTime.Now,
                    MoveOutDate = DateTime.Now,
                    Status = "Completed"
                });
            }

            NotificationHelper.CreateNotification(
                db,
                userId,
                "You have successfully moved out.");

            db.SaveChanges();

            TempData["SuccessMessage"] =
                "You have successfully moved out and your room is now available. " +
                "Please take a moment to leave a review for other students.";

            return RedirectToAction("Create", "Reviews", new { roomId = application.RoomID });
        }

        // GET: Account/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Index", "Home");
        }
    }
}