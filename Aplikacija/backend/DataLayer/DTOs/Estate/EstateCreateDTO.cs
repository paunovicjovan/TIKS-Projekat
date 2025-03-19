using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DataLayer.Models
{
    public class EstateCreateDTO
    {
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required double Price { get; set; }
        public required int SquareMeters { get; set; }
        public required int TotalRooms { get; set; }
        public required EstateCategory Category { get; set; }
        public int? FloorNumber { get; set; }
        [Required(ErrorMessage = "Nekretnina mora sadržati barem jednu sliku.")]
        public required IFormFile[] Images { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }
}
