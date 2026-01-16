using System;

namespace Parking.Core.Entities
{
    public class Incident
    {
        public string IncidentId { get; set; }
        public DateTime ReportedDate { get; set; } = DateTime.Now;
        public string Type { get; set; }        // "LostTicket", "Damage", "Other"
        public string Title { get; set; }       // VD: Mất vé xe 30A-12345
        public string Description { get; set; } // Chi tiết sự việc
        public string Status { get; set; }      // "Open", "Processing", "Resolved"
        public string ReportedBy { get; set; }  // Username của bảo vệ báo cáo
        public string ReferenceId { get; set; } // Biển số xe hoặc Mã vé liên quan
        public string ResolutionNotes { get; set; } // Ghi chú cách giải quyết
    }
}
