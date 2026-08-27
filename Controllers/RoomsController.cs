using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using UmbiloRentals.Models;

namespace UmbiloRentals.Controllers
{
    public class RoomsController : Controller
    {
        private BuildingManagementDBEntities db =
            new BuildingManagementDBEntities();

        // GET: Rooms (Public)
        public ActionResult Index()
        {
            return View(db.Rooms.ToList());
        }

        // GET: Rooms/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Room room = db.Rooms.Find(id);

            if (room == null)
                return HttpNotFound();

            return View(room);
        }

        // GET: Rooms/Create (Admin)
        public ActionResult Create()
        {
            return View();
        }

        // POST: Rooms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(
            Room room,
            HttpPostedFileBase imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null &&
                    imageFile.ContentLength > 0)
                {
                    string extension =
                        Path.GetExtension(imageFile.FileName)
                        .ToLower();

                    string[] allowed =
                    {
                        ".jpg",
                        ".jpeg",
                        ".png"
                    };

                    if (allowed.Contains(extension))
                    {
                        string folder =
                            Server.MapPath("~/Content/RoomImages");

                        if (!Directory.Exists(folder))
                            Directory.CreateDirectory(folder);

                        string fileName =
                            Guid.NewGuid() + extension;

                        imageFile.SaveAs(
                            Path.Combine(folder, fileName));

                        room.Photo = fileName;
                    }
                }

                db.Rooms.Add(room);
                db.SaveChanges();

                return RedirectToAction(
                    "ManageRooms",
                    "Admin");
            }

            return View(room);
        }

        // GET: Rooms/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Room room = db.Rooms.Find(id);

            if (room == null)
                return HttpNotFound();

            return View(room);
        }

        // POST: Rooms/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(
            Room room,
            HttpPostedFileBase imageFile)
        {
            if (ModelState.IsValid)
            {
                Room existing =
                    db.Rooms.Find(room.RoomID);

                if (existing == null)
                    return HttpNotFound();

                existing.RoomNumber = room.RoomNumber;
                existing.MonthlyRent = room.MonthlyRent;
                existing.Description = room.Description;
                existing.Status = room.Status;

                if (imageFile != null &&
                    imageFile.ContentLength > 0)
                {
                    string extension =
                        Path.GetExtension(imageFile.FileName)
                        .ToLower();

                    string[] allowed =
                    {
                        ".jpg",
                        ".jpeg",
                        ".png"
                    };

                    if (allowed.Contains(extension))
                    {
                        string folder =
                            Server.MapPath("~/Content/RoomImages");

                        if (!Directory.Exists(folder))
                            Directory.CreateDirectory(folder);

                        string fileName =
                            Guid.NewGuid() + extension;

                        imageFile.SaveAs(
                            Path.Combine(folder, fileName));

                        existing.Photo = fileName;
                    }
                }

                db.SaveChanges();

                return RedirectToAction(
                    "ManageRooms",
                    "Admin");
            }

            return View(room);
        }

        // GET: Rooms/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Room room = db.Rooms.Find(id);

            if (room == null)
                return HttpNotFound();

            return View(room);
        }

        // POST: Rooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Room room = db.Rooms.Find(id);

            if (room != null)
            {
                db.Rooms.Remove(room);
                db.SaveChanges();
            }

            return RedirectToAction(
                "ManageRooms",
                "Admin");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}