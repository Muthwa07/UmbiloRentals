using System;
using UmbiloRentals.Models;

namespace UmbiloRentals.Helpers
{
    public static class NotificationHelper
    {
        public static void CreateNotification(
            BuildingManagementDBEntities db,
            int userId,
            string message)
        {
            Notification notification =
                new Notification();

            notification.UserID = userId;
            notification.Message = message;
            notification.DateSent = DateTime.Now;
            notification.IsRead = false;

            db.Notifications.Add(notification);
            db.SaveChanges();
        }
    }
}