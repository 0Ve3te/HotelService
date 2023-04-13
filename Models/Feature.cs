#nullable disable
using System.ComponentModel.DataAnnotations;

namespace HotelService.Models
{
    public class Feature
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }

        public ICollection<FeatureHotel> FeatureHotels { get; set; } = new List<FeatureHotel>();
    }
}
