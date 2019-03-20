using System.ComponentModel.DataAnnotations;

namespace prototype_server.Models
{
    public class _BaseModel
    {
        [Key]
        public int Id { get; set; }
        
        protected _BaseModel()
        {}
    }
}