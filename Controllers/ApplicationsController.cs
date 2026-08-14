using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using UmbiloRentals.Models;

namespace UmbiloRentals.Controllers
{
    public class ApplicationsController : Controller
    {
        private BuildingManagementDBEntities db = new BuildingManagementDBEntities();

        // GET: Applications
        public ActionResult Index()
        {
            return View(db.Applications.ToList());
        }

        // GET: Applications/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Application application = db.Applications.Find(id);

            if (application == null)
            {
                return HttpNotFound();
            }

            return View(application);
        }

        // GET: Applications/Create
        // Room ID is received from the Rooms page
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

            ViewBag.RoomNumber = room.RoomNumber;
            ViewBag.RoomID = room.RoomID;

            return View();
        }

        // POST: Applications/Create
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

            if (room.Status != "Available")
            {
                ViewBag.ErrorMessage = "This room is no longer available.";
                ViewBag.RoomNumber = room.RoomNumber;
                ViewBag.RoomID = room.RoomID;

                return View();
            }

            // Document is required
            if (document == null || document.ContentLength == 0)
            {
                ViewBag.ErrorMessage = "Please select a document to upload.";
                ViewBag.RoomNumber = room.RoomNumber;
                ViewBag.RoomID = room.RoomID;

                return View();
            }

            // Allow common document types
            string extension =
                System.IO.Path.GetExtension(document.FileName)
                .ToLower();

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

            // Maximum file size: 10 MB
            if (document.ContentLength > 10 * 1024 * 1024)
            {
                ViewBag.ErrorMessage =
                    "The document must be smaller than 10 MB.";

                ViewBag.RoomNumber = room.RoomNumber;
                ViewBag.RoomID = room.RoomID;

                return View();
            }

            // Create upload folder
            string uploadFolder =
                Server.MapPath("~/UploadedDocuments");

            if (!System.IO.Directory.Exists(uploadFolder))
            {
                System.IO.Directory.CreateDirectory(uploadFolder);
            }

            // Generate a unique filename
            string fileName =
                Guid.NewGuid().ToString() + extension;

            string filePath =
                System.IO.Path.Combine(uploadFolder, fileName);

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
            db.SaveChanges();

            TempData["SuccessMessage"] =
                "Your application has been submitted successfully.";

            return RedirectToAction("Index");
        }

        // GET: Applications/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Application application = db.Applications.Find(id);

            if (application == null)
            {
                return HttpNotFound();
            }

            return View(application);
        }

        // POST: Applications/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            [Bind(Include =
                "ApplicationID,UserID,RoomID,DocumentPath,Status,DateApplied")]
            Application application)
        {
            if (ModelState.IsValid)
            {
                db.Entry(application).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(application);
        }

        // GET: Applications/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Application application = db.Applications.Find(id);

            if (application == null)
            {
                return HttpNotFound();
            }

            return View(application);
        }

        // POST: Applications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Application application = db.Applications.Find(id);

            if (application != null)
            {
                db.Applications.Remove(application);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}