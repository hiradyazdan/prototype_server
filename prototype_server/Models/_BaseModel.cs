using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using prototype_server.Config;

namespace prototype_server.Models
{
    public class _BaseModel
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public DateTime? CreatedAt { get; set; }
        [Required]
        public DateTime? UpdatedAt { get; set; }
        [NotMapped]
        public ActionTypes ActionType { get; set; }
        [NotMapped]
        public ObjectTypes ObjectType { get; set; }
        
        protected _BaseModel()
        {}
    }
}