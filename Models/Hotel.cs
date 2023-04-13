#nullable disable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelService.Models
{
    public class Hotel
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Nazwa hotelu")]
        [RegularExpression("^[^/\\./~!@#$%^&*]*$", ErrorMessage = "Wykryto niedozwolone znaki.")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Opis hotelu")]
        public string Description { get; set; }
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Kraj")]
        public string Country { get; set; }
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Region")]
        public string Region { get; set; }
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Miasto")]
        public string City { get; set; }
        [Display(Name = "Lokalizacja na mapie google")]
        public string? GoogleMapsLocation { get; set; }
        public string FolderName { get; set; }
        [NotMapped]
        public int RoomLowestPrice { get; set; }
        [NotMapped]
        [Display(Name = "Zdjęcie główne hotelu")]
        public IFormFile? HotelPhoto { get; set; }
        [NotMapped]
        [Display(Name = "Zdjęcia obiektu")]
        public List<IFormFile>? HotelPhotos { get; set; }
        [NotMapped]
        [Required(ErrorMessage = "Pole {0} jest wymagane.")]
        [Display(Name = "Udogodnienia")]
        public List<int> FeaturesIdList { get; set; } = new List<int>();
        [NotMapped]
        public List<string> ImagesSrc { get; set; } = new List<string>();
        [NotMapped]
        public List<Room> RoomList { get; set; } = new List<Room>();
        [NotMapped]
        public List<Feature> FeatureList { get; set; } = new List<Feature>();
        [NotMapped]
        public List<Review> Reviews { get; set; }
        [NotMapped]
        public double AvgReviews { get; set; } = 0;

        public ICollection<FeatureHotel> FeatureHotels { get; set; } = new List<FeatureHotel>();
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
