#nullable disable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelService.Models
{
    public class Reservation
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Data rozpoczęcia rezerwacji")]

        public DateTime DateStart { get; set; }
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Data zakończenia rezerwacji")]
        public DateTime DateEnd { get; set; }
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Imię")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Nazwisko")]
        public string LastName { get; set; }
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Numer telefonu")]
        //[RegularExpression(@"^\(?([0-9]{3})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{3})$", ErrorMessage = "Nieprawidłowy numer telefonu")]
        [MinLength(9, ErrorMessage = "Podaj poprawny numer telefonu.")]
        [MaxLength(9, ErrorMessage = "Podaj poprawny numer telefonu.")]
        public string PhoneNumber { get; set; }
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Adres email")]
        public string Email { get; set; }
        [Display(Name = "Kwota")]
        public int? Price { get; set; }
        public string Status { get; set; } = "W trakcie realizacji";

        public Hotel Hotel { get; set; }
        public Room Room { get; set; }
        [Display(Name = "Pokój")]
        public int? RoomId { get; set; }
        public int HotelId { get; set; }
        public bool IsRated { get; set; } = false;

        [NotMapped]
        [Display(Name = "Nazwa hotelu")]
        public string HotelName { get; set; } 
        [NotMapped]
        [Display(Name = "Nazwa pokoju")]
        public string RoomName { get; set; }
        [NotMapped]
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Kod zwrotny")]
        [MinLength(6, ErrorMessage = "Długość kodu zwrotnego jest nieprawidłowa.")]
        [MaxLength(6, ErrorMessage = "Długość kodu zwrotnego jest nieprawidłowa.")]
        public string CodeSMS { get; set; }

    }
}
