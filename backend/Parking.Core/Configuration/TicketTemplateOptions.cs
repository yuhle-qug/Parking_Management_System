using System;
using System.IO;

namespace Parking.Core.Configuration
{
    public class TicketTemplateOptions
    {
        public const string SectionName = "TicketTemplate";

        public string TemplatePath { get; set; } = Path.Combine("Templates", "ticket-template.html");

        public string DateTimeFormat { get; set; } = "dd/MM/yyyy HH:mm";

        public string BrandName { get; set; } = "Parking System";

        public string TimezoneLabel { get; set; } = "Local Time";

        public string GetAbsolutePath()
        {
            if (Path.IsPathRooted(TemplatePath))
            {
                return TemplatePath;
            }

            return Path.Combine(AppContext.BaseDirectory, TemplatePath);
        }
    }
}
