using System;
using System.Linq;
using System.Web.Mvc;
using UmbiloRentals.Models;

namespace UmbiloRentals.Controllers
{
    public class AccountController : Controller
    {
        private BuildingManagementDBEntities db = new BuildingManagementDBEntities();

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
            string password)
        {
            // Check whether the email already exists
            bool emailExists = db.Users.Any(u => u.Email == email);

            if (emailExists)
            {
                ViewBag.ErrorMessage = "An account with this email already exists.";
                return View();
            }

            // Create new applicant
            User user = new User();

            user.FirstName = firstName;
            user.LastName = lastName;
            user.Email = email;
            user.Phone = phone;
            user.Password = password;

            // RoleID 1 = Applicant
            user.RoleID = 1;

            user.Status = "Active";
            user.DateCreated = DateTime.Now;

            db.Users.Add(user);
            db.SaveChanges();

            TempData["SuccessMessage"] =
                "Your account has been created successfully. You can now log in.";

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
                     u.Status == "Active"
            );

            if (user == null)
            {
                ViewBag.ErrorMessage = "Invalid email or password.";
                return View();
            }

            // Store the logged-in user's information in Session
            Session["UserID"] = user.UserID;
            Session["UserName"] = user.FirstName + " " + user.LastName;
            Session["RoleID"] = user.RoleID;

            if (user.RoleID == 2)
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Dashboard");
        }

        // GET: Account/Dashboard
        public ActionResult Dashboard()
        {
            // Make sure the user is logged in
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        // GET: Account/Logout
        public ActionResult Logout()
        {
            // Clear all session information
            Session.Clear();

            // Abandon the current session
            Session.Abandon();

            // Return to the home page
            return RedirectToAction("Index", "Home");
        }
    }
}