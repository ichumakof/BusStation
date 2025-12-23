// DAL.Repositories/BusRepository.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace DAL.Repositories
{
    public class BusRepository : IDisposable
    {
        private BusStationEntities _context;

        public BusRepository()
        {
            _context = new BusStationEntities();
        }

        public List<DAL.Buses> GetAll()
        {
            return _context.Buses.ToList();
        }

        public DAL.Buses GetById(int id)
        {
            var found = _context.Buses.Find(id);
            if (found != null) return found;
            return _context.Buses.FirstOrDefault(b => b.BusID == id);
        }

        public int CreateBus(DAL.Buses bus)
        {
            _context.Buses.Add(bus);
            _context.SaveChanges();
            return bus.BusID;
        }

        public void UpdateBus(DAL.Buses bus)
        {
            if (bus == null) return;
            var existing = _context.Buses.Find(bus.BusID);
            if (existing == null) throw new InvalidOperationException($"Bus with id {bus.BusID} not found.");
            // копируем поля вручную или используйте attach+state=modified
            existing.NumberPlate = bus.NumberPlate;
            existing.Model = bus.Model;
            existing.SeatsCount = bus.SeatsCount;
            // ... другие поля
            _context.SaveChanges();
        }

        public void DeleteBus(int id)
        {
            var existing = _context.Buses.Find(id);
            if (existing == null) return;
            _context.Buses.Remove(existing);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}