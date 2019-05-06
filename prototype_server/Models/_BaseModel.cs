using System;

namespace prototype_server.Models
{
    public class _BaseModel
    {
        public int Id { get; set; }
        
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        protected _BaseModel()
        {}
    }
}