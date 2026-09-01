using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UmbiloRentals.Models;

namespace UmbiloRentals.Controllers
{
    public class AdminController : Controller
    {
        private BuildingManagementDBEntities db =
            new BuildingManagementDBEntities();

        private bool IsAdmin()
        {
            return Session["UserID"] != null &&
                   Session["RoleID"] != null &&
                   (int)Session["RoleID"] == 2;
        }

        // Dashboard
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

            return View();
        }

        // View every application
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
                     RoomID = a.RoomID,
                     Status = a.Status,
                     DateApplied = a.DateApplied,
                     DocumentPath = a.DocumentPath
                 }).ToList();

            return View(applications);
        }

        // APPROVE
        public ActionResult Approve(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var application = db.Applications.Find(id);

            if (application == null)
                return HttpNotFound();

            application.Status = "Approved";

            var room = db.Rooms.Find(application.RoomID);

            if (room != null)
                room.Status = "Occupied";

            // Reject every other pending application for the same room
            var others = db.Applications.Where(a =>
                a.RoomID == application.RoomID &&
                a.ApplicationID != application.ApplicationID &&
                a.Status == "Pending");

            foreach (var app in others)
            {
                app.Status = "Rejected";
            }

            db.SaveChanges();

            return RedirectToAction("Applications");
        }

        // REJECT
        public ActionResult Reject(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var application = db.Applications.Find(id);

            if (application == null)
                return HttpNotFound();

            application.Status = "Rejected";

            db.SaveChanges();

            return RedirectToAction("Applications");
        }

        // ADMIN ROOM LIST
        public ActionResult Rooms()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            return View(db.Rooms.OrderBy(r => r.RoomNumber).ToList());
        }

        // EDIT ROOM
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

            var existingRoom = db.Rooms.Find(room.RoomID);

            if (existingRoom == null)
                return HttpNotFound();

            existingRoom.RoomNumber = room.RoomNumber;
            existingRoom.MonthlyRent = room.MonthlyRent;
            existingRoom.Description = room.Description;
            existingRoom.Status = room.Status;

            if (photoFile != null && photoFile.ContentLength > 0)
            {
                string extension =
                    System.IO.Path.GetExtension(photoFile.FileName);

                string fileName =
                    Guid.NewGuid().ToString() + extension;

                string folder =
                    Server.MapPath("~/Content/RoomImages");

                if (!System.IO.Directory.Exists(folder))
                {
                    System.IO.Directory.CreateDirectory(folder);
                }

                photoFile.SaveAs(
                    System.IO.Path.Combine(folder, fileName));

                existingRoom.Photo = fileName;
            }

            db.SaveChanges();

            TempData["SuccessMessage"] =
                "Room updated successfully.";

            return RedirectToAction("Rooms");
        }

        // CREATE ROOM
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
                    string extension = System.IO.Path
                        .GetExtension(photoFile.FileName);

                    string fileName =
                        Guid.NewGuid().ToString() + extension;

                    string folder =
                        Server.MapPath("~/Content/RoomImages");

                    if (!System.IO.Directory.Exists(folder))
                    {
                        System.IO.Directory.CreateDirectory(folder);
                    }

                    photoFile.SaveAs(
                        System.IO.Path.Combine(folder, fileName));

                    room.Photo = fileName;
                }

                db.Rooms.Add(room);
                db.SaveChanges();

                TempData["SuccessMessage"] =
                    "Room added successfully.";

                return RedirectToAction("Rooms");
            }

            return View(room);
        }

        // DELETE ROOM
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

            TempData["SuccessMessage"] = "Room deleted successfully.";

            return RedirectToAction("Rooms");
        }
    }
}