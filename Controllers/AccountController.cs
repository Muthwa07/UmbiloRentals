using System;
using System.Linq;
using System.Web.Mvc;
using UmbiloRentals.Helpers;
using UmbiloRentals.Models;

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
            string ConfirmPassword)
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
            if (!System.Text.RegularExpressions.Regex.IsMatch(
                phone ?? "",
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
                Password = password,
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
                     u.Password == password &&
                     u.Status == "Active");

            if (user == null)
            {
                ViewBag.ErrorMessage = "Invalid email or password.";
                return View();
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

            ViewBag.NotificationCount = 0;

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
                }
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

            return View(user);
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

            // These fields only save if you've added them to the Users table.
            user.Occupation = model.Occupation;
            user.EmergencyContactName = model.EmergencyContactName;
            user.EmergencyContactPhone = model.EmergencyContactPhone;

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

            NotificationHelper.CreateNotification(
                db,
                userId,
                "🏠 You have successfully moved out.");

            db.SaveChanges();

            TempData["SuccessMessage"] =
                "You have successfully moved out and your room is now available.";

            return RedirectToAction("Dashboard");
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