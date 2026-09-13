using System;
using UmbiloRentals.Models;

namespace UmbiloRentals.Helpers
{
    public static class SmsService
    {
        public static void SendSms(
            BuildingManagementDBEntities db,
            int userId,
            string message)
        {
            Notification sms = new Notification();

            sms.UserID = userId;
            sms.Message = message;
            sms.DateSent = DateTime.Now;
            sms.IsRead = false;

            db.Notifications.Add(sms);
            db.SaveChanges();
        }
    }
}