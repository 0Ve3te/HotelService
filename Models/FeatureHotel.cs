#nullable disable
namespace HotelService.Models
{
    public class FeatureHotel
    {
        public int Id { get; set; }
        public int FeatureId { get; set; }
        public int HotelId { get; set; }

        public Hotel Hotel { get; set; }
        public Feature Feature { get; set; }
    }
}
