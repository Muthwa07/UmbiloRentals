using System;
using System.Linq;
using System.Web.Mvc;
using UmbiloRentals.Models;

namespace UmbiloRentals.Controllers
{
    public class ReviewsController : BaseController
    {
        // GET: Reviews/Create?roomId=5
        public ActionResult Create(int roomId)
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];

            var room = db.Rooms.Find(roomId);

            if (room == null)
                return HttpNotFound();

            // Only someone who actually completed a stay in this room can review it
            bool hasCompletedStay = db.Allocations.Any(a =>
                a.UserID == userId &&
                a.RoomID == roomId &&
                a.Status == "Completed");

            if (!hasCompletedStay)
            {
                TempData["ErrorMessage"] =
                    "You can only review a room after moving out of it.";

                return RedirectToAction("Dashboard", "Account");
            }

            bool alreadyReviewed = db.Reviews.Any(r =>
                r.UserID == userId &&
                r.RoomID == roomId);

            if (alreadyReviewed)
            {
                TempData["SuccessMessage"] =
                    "You've already reviewed this room. Thank you!";

                return RedirectToAction("Dashboard", "Account");
            }

            ViewBag.RoomNumber = room.RoomNumber;
            ViewBag.RoomID = room.RoomID;

            return View();
        }

        // POST: Reviews/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(int roomId, int rating, string comment)
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "Account");

            int userId = (int)Session["UserID"];

            var room = db.Rooms.Find(roomId);

            if (room == null)
                return HttpNotFound();

            bool hasCompletedStay = db.Allocations.Any(a =>
                a.UserID == userId &&
                a.RoomID == roomId &&
                a.Status == "Completed");

            bool alreadyReviewed = db.Reviews.Any(r =>
                r.UserID == userId &&
                r.RoomID == roomId);

            if (!hasCompletedStay || alreadyReviewed || rating < 1 || rating > 5)
            {
                TempData["ErrorMessage"] =
                    "Your review couldn't be submitted.";

                return RedirectToAction("Dashboard", "Account");
            }

            db.Reviews.Add(new Review
            {
                RoomID = roomId,
                UserID = userId,
                Rating = rating,
                Comment = comment,
                DatePosted = DateTime.Now
            });

            db.SaveChanges();

            TempData["SuccessMessage"] = "Thanks for leaving a review!";

            return RedirectToAction("Dashboard", "Account");
        }
    }
}
