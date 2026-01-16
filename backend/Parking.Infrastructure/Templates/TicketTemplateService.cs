using System;
using System.IO;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parking.Core.Configuration;
using Parking.Core.Entities;
using Parking.Core.Interfaces;

namespace Parking.Infrastructure.Templates
{
    public class TicketTemplateService : ITicketTemplateService
    {
        private readonly ILogger<TicketTemplateService> _logger;
        private readonly TicketTemplateOptions _options;
        private readonly Lazy<string> _template;

        public TicketTemplateService(IOptions<TicketTemplateOptions> options, ILogger<TicketTemplateService> logger)
        {
            _logger = logger;
            _options = options?.Value ?? new TicketTemplateOptions();
            _template = new Lazy<string>(LoadTemplate);
        }

        public TicketPrintResult RenderHtml(TicketPrintData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var formattedTime = data.EntryTime.ToString(_options.DateTimeFormat ?? "dd/MM/yyyy HH:mm");
            var template = _template.Value;

            var html = template
                .Replace("{{BrandName}}", HtmlEncode(_options.BrandName))
                .Replace("{{GateName}}", HtmlEncode(string.IsNullOrWhiteSpace(data.GateName) ? data.GateId : data.GateName))
                .Replace("{{GateId}}", HtmlEncode(data.GateId))
                .Replace("{{PlateNumber}}", HtmlEncode(data.PlateNumber))
                .Replace("{{TicketId}}", HtmlEncode(data.TicketId))
                .Replace("{{EntryTime}}", HtmlEncode(formattedTime))
                .Replace("{{VehicleType}}", HtmlEncode(data.VehicleType))
                .Replace("{{TimezoneLabel}}", HtmlEncode(_options.TimezoneLabel));

            var safeFile = string.IsNullOrWhiteSpace(data.TicketId) ? "ticket.html" : $"ticket-{SanitizeFileName(data.TicketId)}.html";

            return new TicketPrintResult
            {
                Html = html,
                ContentType = "text/html",
                FileName = safeFile
            };
        }

        private string LoadTemplate()
        {
            try
            {
                var path = _options.GetAbsolutePath();
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    return File.ReadAllText(path);
                }

                _logger.LogWarning("Ticket template not found at {Path}; fallback to embedded template.", path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load ticket template; fallback to embedded template.");
            }

            return DefaultTemplate;
        }

        private static string HtmlEncode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        private static string SanitizeFileName(string value)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '-');
            }

            return value.Replace(' ', '-');
        }

        // 80mm thermal-style layout with inline CSS for portability.
                private string DefaultTemplate => @"<!doctype html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>Parking Ticket</title>
    <style>
        * { box-sizing: border-box; }
        body { margin: 0; font-family: 'Segoe UI', Arial, sans-serif; background: #f5f5f5; }
        .ticket { width: 80mm; margin: 8px auto; background: #fff; color: #111; padding: 14px; border: 1px solid #e2e2e2; border-radius: 6px; }
        .title { text-align: center; font-size: 16px; font-weight: 700; letter-spacing: 0.6px; margin-bottom: 8px; }
        .meta { text-align: center; font-size: 11px; color: #444; margin-bottom: 10px; }
        .section { border-top: 1px dashed #ccc; padding-top: 10px; margin-top: 10px; }
        .row { display: flex; justify-content: space-between; font-size: 13px; padding: 4px 0; }
        .label { color: #666; }
        .value { font-weight: 600; color: #111; }
        .highlight { font-size: 18px; font-weight: 700; letter-spacing: 1px; text-align: center; padding: 10px 0 4px; }
        .footer { text-align: center; font-size: 11px; color: #555; margin-top: 12px; }
        .barcode { text-align: center; font-size: 12px; letter-spacing: 2px; margin-top: 6px; }
    </style>
</head>
<body>
    <div class=""ticket"">
        <div class=""title"">{{BrandName}}</div>
        <div class=""meta"">{{TimezoneLabel}} · {{EntryTime}}</div>

        <div class=""section"">
            <div class=""row""><span class=""label"">Gate</span><span class=""value"">{{GateName}}</span></div>
            <div class=""row""><span class=""label"">Plate</span><span class=""value"">{{PlateNumber}}</span></div>
            <div class=""row""><span class=""label"">Vehicle</span><span class=""value"">{{VehicleType}}</span></div>
            <div class=""row""><span class=""label"">Ticket ID</span><span class=""value"">{{TicketId}}</span></div>
        </div>

        <div class=""section"">
            <div class=""highlight"">ENTRY TICKET</div>
            <div style=""text-align: center; margin-top: 10px;"">
                <!-- QR Code representing TicketId for scanning -->
                <img src=""https://api.qrserver.com/v1/create-qr-code/?size=150x150&data={{TicketId}}"" style=""width: 150px; height: 150px;"" alt=""QR Code"" />
            </div>
            <div class=""barcode"">{{TicketId}}</div>
        </div>

        <div class=""footer"">Please keep this ticket for checkout.</div>
    </div>
</body>
</html>";
    }
}
