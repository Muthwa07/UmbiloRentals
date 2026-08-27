using System;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using UmbiloRentals.Models;

namespace UmbiloRentals.Controllers
{
    public class ApplicationsController : Controller
    {
        private BuildingManagementDBEntities db =
            new BuildingManagementDBEntities();


        // =========================================================
        // GET: Applications
        // =========================================================
        public ActionResult Index()
        {
            // Make sure the user is logged in
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserID"];

            // Only retrieve applications belonging
            // to the logged-in user
            var applications = db.Applications
                                 .Where(a => a.UserID == userId)
                                 .ToList();

            return View(applications);
        }


        // =========================================================
        // GET: Applications/Details/5
        // =========================================================
        public ActionResult Details(int? id)
        {
            // Make sure the user is logged in
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);
            }

            int userId = (int)Session["UserID"];

            // Only find the application if it belongs
            // to the logged-in user
            Application application = db.Applications
                .FirstOrDefault(a =>
                    a.ApplicationID == id &&
                    a.UserID == userId);

            if (application == null)
            {
                return HttpNotFound();
            }

            return View(application);
        }


        // =========================================================
        // GET: Applications/Create
        // Room ID is received from the Rooms page
        // =========================================================
        public ActionResult Create(int? id)
        {
            // Make sure the applicant is logged in
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest,
                    "Room ID is required."
                );
            }

            // Check that the room exists
            Room room = db.Rooms.Find(id);

            if (room == null)
            {
                return HttpNotFound();
            }

            // Only available rooms can be applied for
            if (room.Status != "Available")
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest,
                    "This room is not available."
                );
            }
            int userId = (int)Session["UserID"];

            bool alreadyApplied = db.Applications.Any(a =>
                a.UserID == userId &&
                a.RoomID == room.RoomID
            );

            if (alreadyApplied)
            {
                TempData["ErrorMessage"] =
                    "You have already applied for this room.";

                return RedirectToAction("Index", "Rooms");
            }

            ViewBag.RoomNumber = room.RoomNumber;
            ViewBag.RoomID = room.RoomID;

            return View();
        }


        // =========================================================
        // POST: Applications/Create
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            int roomID,
            HttpPostedFileBase document)
        {
            // Make sure the applicant is logged in
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check room
            Room room = db.Rooms.Find(roomID);

            if (room == null)
            {
                return HttpNotFound();
            }

            // Check room availability
            if (room.Status != "Available")
            {
                ViewBag.ErrorMessage =
                    "This room is no longer available.";

                ViewBag.RoomNumber = room.RoomNumber;
                ViewBag.RoomID = room.RoomID;

                return View();
            }
            int userId = (int)Session["UserID"];

            bool alreadyApplied = db.Applications.Any(a =>
                a.UserID == userId &&
                a.RoomID == roomID
            );

            if (alreadyApplied)
            {
                ViewBag.ErrorMessage =
                    "You have already applied for this room.";

                ViewBag.RoomNumber = room.RoomNumber;
                ViewBag.RoomID = room.RoomID;

                return View();
            }


            // =====================================================
            // DOCUMENT VALIDATION
            // =====================================================

            // Document is required
            if (document == null ||
                document.ContentLength == 0)
            {
                ViewBag.ErrorMessage =
                    "Please select a document to upload.";

                ViewBag.RoomNumber = room.RoomNumber;
                ViewBag.RoomID = room.RoomID;

                return View();
            }


            // Allowed document types
            string extension =
                System.IO.Path.GetExtension(
                    document.FileName).ToLower();

            string[] allowedExtensions =
            {
                ".pdf",
                ".doc",
                ".docx",
                ".jpg",
                ".jpeg",
                ".png"
            };


            if (!allowedExtensions.Contains(extension))
            {
                ViewBag.ErrorMessage =
                    "Please upload a PDF, Word document, JPG or PNG file.";

                ViewBag.RoomNumber = room.RoomNumber;
                ViewBag.RoomID = room.RoomID;

                return View();
            }


            // =====================================================
            // MAXIMUM FILE SIZE: 5 MB
            // =====================================================

            if (document.ContentLength >
                5 * 1024 * 1024)
            {
                ViewBag.ErrorMessage =
                    "The document must be smaller than 5 MB.";

                ViewBag.RoomNumber = room.RoomNumber;
                ViewBag.RoomID = room.RoomID;

                return View();
            }


            // =====================================================
            // CREATE UPLOAD FOLDER
            // =====================================================

            string uploadFolder =
                Server.MapPath("~/UploadedDocuments");

            if (!System.IO.Directory.Exists(uploadFolder))
            {
                System.IO.Directory.CreateDirectory(
                    uploadFolder);
            }


            // =====================================================
            // GENERATE UNIQUE FILE NAME
            // =====================================================

            string fileName =
                Guid.NewGuid().ToString() +
                extension;

            string filePath =
                System.IO.Path.Combine(
                    uploadFolder,
                    fileName);


            // =====================================================
            // Save document
            document.SaveAs(filePath);

            // Create application
            Application application = new Application();

            application.UserID = Convert.ToInt32(Session["UserID"]);
            application.RoomID = roomID;
            application.DocumentPath =
                "~/UploadedDocuments/" + fileName;

            application.Status = "Pending";
            application.DateApplied = DateTime.Now;

            db.Applications.Add(application);

            try
            {
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage =
                    "Database error: " + ex.Message;

                ViewBag.RoomNumber = room.RoomNumber;
                ViewBag.RoomID = room.RoomID;

                return View();
            }


            TempData["SuccessMessage"] =
                "Your application has been submitted successfully.";

            return RedirectToAction("Index");
        }


        // =========================================================
        // GET: Applications/Edit/5
        // =========================================================
        public ActionResult Edit(int? id)
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            int userId = (int)Session["UserID"];

            var application = db.Applications.FirstOrDefault(a =>
                a.ApplicationID == id &&
                a.UserID == userId);

            if (application == null)
                return HttpNotFound();

            if (application.Status != "Pending")
            {
                TempData["ErrorMessage"] =
                    "Only pending applications can be edited.";

                return RedirectToAction("Index");
            }

            ViewBag.RoomNumber =
                db.Rooms.Find(application.RoomID).RoomNumber;

            return View(application);
        }


        // =========================================================
        // POST: Applications/Edit/5
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            int ApplicationID,
            HttpPostedFileBase document)
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];

            var application = db.Applications.FirstOrDefault(a =>
                a.ApplicationID == ApplicationID &&
                a.UserID == userId);

            if (application == null)
                return HttpNotFound();

            if (application.Status != "Pending")
            {
                TempData["ErrorMessage"] =
                    "Only pending applications can be edited.";

                return RedirectToAction("Index");
            }

            if (document != null &&
                document.ContentLength > 0)
            {
                string extension =
                    Path.GetExtension(document.FileName).ToLower();

                string[] allowed =
                {
            ".pdf",".doc",".docx",".jpg",".jpeg",".png"
        };

                if (allowed.Contains(extension))
                {
                    string folder =
                        Server.MapPath("~/UploadedDocuments");

                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    string fileName =
                        Guid.NewGuid() + extension;

                    document.SaveAs(
                        Path.Combine(folder, fileName));

                    application.DocumentPath =
                        "~/UploadedDocuments/" + fileName;
                }
            }

            db.SaveChanges();

            TempData["SuccessMessage"] =
                "Application updated successfully.";

            return RedirectToAction("Index");
        }


        // =========================================================
        // GET: Applications/Delete/5
        // =========================================================
        public ActionResult Delete(int? id)
        {
            // Make sure the user is logged in
            if (Session["UserID"] == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(
                    HttpStatusCode.BadRequest);
            }

            int userId =
                (int)Session["UserID"];


            // Only retrieve the application if it belongs
            // to the logged-in user
            Application application =
                db.Applications
                .FirstOrDefault(a =>
                    a.ApplicationID == id &&
                    a.UserID == userId);


            if (application == null)
            {
                return HttpNotFound();
            }

            return View(application);
        }


        // =========================================================
        // POST: Applications/Delete/5
        // SECURE DELETE
        // =========================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            // Make sure the user is logged in
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserID"];

            // Only find the application belonging to the logged-in user
            Application application = db.Applications
                .FirstOrDefault(a =>
                    a.ApplicationID == id &&
                    a.UserID == userId);

            if (application == null)
            {
                return HttpNotFound();
            }

            // Delete the uploaded document from the server
            if (!string.IsNullOrEmpty(application.DocumentPath))
            {
                string filePath = Server.MapPath(application.DocumentPath);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            // Delete the application from the database
            db.Applications.Remove(application);
            db.SaveChanges();

            TempData["SuccessMessage"] =
                "Your application has been deleted successfully.";

            return RedirectToAction("Index");
        }


        // =========================================================
        // DISPOSE DATABASE
        // =========================================================
        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}