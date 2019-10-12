using Microsoft.EntityFrameworkCore;
using RoutineBot.Repository.Model;

namespace RoutineBot.Repository.DB
{
    public class RemindersDbContext : DbContext
    {
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Reminder> Reminders { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=Reminders.db");
        }

    }
}