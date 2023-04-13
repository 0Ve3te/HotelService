#nullable disable
using HotelService.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HotelService.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Feature> Features { get; set; }
        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<FeatureHotel> FeatureHotels { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Feature>().HasData(new Feature[]
            {
                new Feature { Id = 1, Name = "Parking", Icon = "fas fa-parking"},
                new Feature { Id = 2, Name = "Basen", Icon = "fas fa-swimmer"},
                new Feature { Id = 3, Name = "WiFi", Icon = "fas fa-wifi"},
                new Feature { Id = 4, Name = "Bar", Icon = "fas fa-glass-martini-alt"},
                new Feature { Id = 5, Name = "Restauracja", Icon = "fas fa-utensils"},
                new Feature { Id = 6, Name = "Siłownia", Icon = "fas fa-dumbbell"},
                new Feature { Id = 7, Name = "Kort tenisowy", Icon = "fas fa-baseball-ball"},
                new Feature { Id = 8, Name = "Sala bankietowa", Icon = "fas fa-crown"},
                new Feature { Id = 9, Name = "Pralnia", Icon = "fas fa-tshirt"},
                new Feature { Id = 10, Name = "Transport lotniskowy", Icon = "fas fa-taxi"}
            });
        }
    }
}