#nullable disable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelService.Models
{
    public class Room
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Nazwa pokoju/standardu")]
        [RegularExpression("^[^/\\./~!@#$%^&*]*$", ErrorMessage = "Wykryto niedozwolone znaki.")]

        public string Name { get; set; } = "";
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Opis")]
        [MaxLength(800)]
        public string Description { get; set; } = "";
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Cena za noc")]
        public int Price { get; set; }
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Maksymalna ilość osób")]
        public int People { get; set; }
        public string FolderName { get; set; } = "";
        [NotMapped]
        [Display(Name = "Zdjęcie pokoju")]
        public IFormFile? RoomPhoto { get; set; }
        [NotMapped]
        public string Folder { get; set; }
        public int HotelId { get; set; }
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Ilość takich pokojów w hotelu")]
        public int CountOfRooms { get; set; } = 1;

        public Hotel hotel { get; set; }
        public ICollection<Reservation> Reservations { get; set; }

    }
}
