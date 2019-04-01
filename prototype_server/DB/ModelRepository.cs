using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using prototype_server.Models;

namespace prototype_server.DB
{
    public interface IRepository<T> where T : _BaseModel
    {
        IEnumerable<T> GetAll();
        T Get(long id);
        void Create(T model, bool async = false);
        void Update(T model, bool async = false);
        void Delete(T model, bool async = false);
    }
    
    public class ModelRepository<T> : IRepository<T> where T : _BaseModel 
    {
        
        private readonly GameDbContext _context;
        private readonly DbSet<T> _models;
        
        string errorMessage = string.Empty;
        
        public ModelRepository(GameDbContext context) {
            _context = context;
            _models = context.Set<T>();
        }
        
        public IEnumerable<T> GetAll() {
            return _models.AsEnumerable();
        }
        
        public T Get(long id) {
            return _models.SingleOrDefault(model => model.Id == id);
        }
        
        public void Create(T model, bool async = false) {
            
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            
            _models.Add(model);

            if (!async)
            {
                _context.SaveChanges();
            }
            else
            {
                _context?.SaveChangesAsync();
            }
        }
        
        public void Update(T model, bool async = false) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }

            _models.Update(model);
            
            if (!async)
            {
                _context.SaveChanges();
            }
            else
            {
                _context?.SaveChangesAsync();
            }
        }
        
        public void Delete(T model, bool async = false) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            
            _models.Remove(model);
            
            if (!async)
            {
                _context.SaveChanges();
            }
            else
            {
                _context?.SaveChangesAsync();
            }
        }
    }
}