using System;
using System.ComponentModel.DataAnnotations;

namespace ActioNator.ViewModels.Community
{
    public class PostImageViewModel
    {
        public Guid Id { get; set; }
        
        [Required]
        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; } = null!;
        
        public Guid PostId { get; set; }
    }
}
