using System;
using System.Collections.Generic;

namespace PowerDocu.Common
{
    public static class NotificationHelper
    {
        private static readonly List<NotificationReceiverBase> NotificationReceivers = new List<NotificationReceiverBase>();

        public static void SendNotification(string notification)
        {
            foreach (NotificationReceiverBase notificationReceiver in NotificationReceivers)
            {
                notificationReceiver.Notify(notification);
            }
        }

        public static void AddNotificationReceiver(NotificationReceiverBase notificationReceiver)
        {
            NotificationReceivers.Add(notificationReceiver);
        }

        public static void SendStatusUpdate(string notification)
        {
            foreach (NotificationReceiverBase notificationReceiver in NotificationReceivers)
            {
                notificationReceiver.NotifyStatus(notification);
            }
        }

        public static void SendPhaseUpdate(string notification)
        {
            foreach (NotificationReceiverBase notificationReceiver in NotificationReceivers)
            {
                notificationReceiver.NotifyPhase(notification);
            }
        }
    }

    public abstract class NotificationReceiverBase
    {
        public abstract void Notify(string notification);
        public virtual void NotifyStatus(string notification) { }
        public virtual void NotifyPhase(string notification) { }
    }

    public class ConsoleNotificationReceiver : NotificationReceiverBase
    {
        public override void Notify(string notification)
        {
            Console.WriteLine(notification);
        }

        public override void NotifyStatus(string notification)
        {
            Console.WriteLine(notification);
        }

        public override void NotifyPhase(string notification)
        {
            Console.WriteLine(notification);
        }
    }
}