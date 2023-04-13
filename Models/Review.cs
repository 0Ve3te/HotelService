#nullable disable

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelService.Models
{
    public class Review
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Imię")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Nazwisko")]
        public string LastName { get; set; }
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Ocena")]
        [Range(0, 10, ErrorMessage = "Wprowadzono nieprawidłową wartość, ocena może wynosić od 0 do 10")]
        public int Rate { get; set; }
        public string UserId { get; set; }
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Tytuł")]
        public string Title { get; set; }
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Opis")]
        public string Description { get; set; }
        [Display(Name = "Zalety")]
        public string Pluses { get; set; }
        [Display(Name = "Wady")]
        public string Minuses { get; set; }
        public int ReservationId { get; set; }

        public int HotelId { get; set; }
        public Hotel Hotel { get; set; }

        [NotMapped]
        public DateTime ReservationStart { get; set;  }
        [NotMapped]
        public DateTime ReservationEnd { get; set;  } 
    }
}

