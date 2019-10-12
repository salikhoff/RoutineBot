using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RoutineBot.Repository.Model
{
    public class Chat : ICloneable
    {
        public long ChatId { get; set; }
        [Required]
        public System.TimeSpan TimeZone { get; set; }

        public List<Reminder> Reminders { get; set; }

        public object Clone()
        {
            Chat chat = new Chat()
            {
                ChatId = this.ChatId,
                TimeZone = this.TimeZone,
                Reminders = new List<Reminder>()
            };
            foreach (Reminder r in this.Reminders)
            {
                Reminder rclone = (Reminder)r.Clone();
                rclone.Chat = chat;
                chat.Reminders.Add(rclone);
            }
            return chat;
        }
    }
}