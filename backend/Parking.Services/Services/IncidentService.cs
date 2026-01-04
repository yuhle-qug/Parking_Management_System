using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Services.Services
{
    public interface IIncidentService
    {
        Task<Incident> ReportIncidentAsync(string title, string description, string reportedBy, string referenceId);
        Task<bool> ResolveIncidentAsync(string incidentId, string notes);
        Task<IEnumerable<Incident>> GetAllIncidentsAsync();
    }

    public class IncidentService : IIncidentService
    {
        private readonly IIncidentRepository _incidentRepo;

        public IncidentService(IIncidentRepository incidentRepo)
        {
            _incidentRepo = incidentRepo;
        }

        public async Task<Incident> ReportIncidentAsync(string title, string description, string reportedBy, string referenceId)
        {
            var incident = new Incident
            {
                IncidentId = "INC-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                ReportedDate = DateTime.Now,
                Title = title,
                Description = description,
                Status = "Open",
                ReportedBy = reportedBy,
                ReferenceId = referenceId
            };

            await _incidentRepo.AddAsync(incident);
            return incident;
        }

        public async Task<bool> ResolveIncidentAsync(string incidentId, string notes)
        {
            var incident = await _incidentRepo.GetByIdAsync(incidentId);
            if (incident == null) return false;

            incident.Status = "Resolved";
            incident.ResolutionNotes = notes;

            await _incidentRepo.UpdateAsync(incident);
            return true;
        }

        public async Task<IEnumerable<Incident>> GetAllIncidentsAsync()
        {
            return await _incidentRepo.GetAllAsync();
        }
    }
}
