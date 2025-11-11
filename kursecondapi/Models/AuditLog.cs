using System;
using System.Collections.Generic;

namespace kursecondapi.Models;

public partial class AuditLog
{
    public long Id { get; set; }

    public string? UserId { get; set; }

    public string? UserName { get; set; }

    public string TableName { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? RecordId { get; set; }

    public DateTime Timestamp { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }
}
