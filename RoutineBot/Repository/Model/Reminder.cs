using System;
using System.ComponentModel.DataAnnotations;

namespace RoutineBot.Repository.Model
{
    public class Reminder : ICloneable
    {
        public long ReminderId { get; set; }
        [Required]
        public long ChatId { get; set; }
        public Chat Chat { get; set; }
        [Required]
        public string MessageText { get; set; }
        [Required]
        public TimeSpan DayTime { get; set; }
        [Required]
        public WeekDays WeekDays { get; set; }

        public object Clone()
        {
            return new Reminder()
            {
                ReminderId = this.ReminderId,
                ChatId = this.ChatId,
                MessageText = this.MessageText,
                DayTime = this.DayTime,
                WeekDays = this.WeekDays
            };
        }
    }
}