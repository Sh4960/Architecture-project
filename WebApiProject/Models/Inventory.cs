using System.ComponentModel.DataAnnotations;

namespace WebApiProject.Models
{
    public class Inventory
    {
        [Key]
        public int Id { get; set; }

        // linked to Gift (product) id
        public int GiftId { get; set; }

        // current quantity available
        public int Quantity { get; set; }

        // navigation property (optional)
        public Gift Gift { get; set; }
    }
}
