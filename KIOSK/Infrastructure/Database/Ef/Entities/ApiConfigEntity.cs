using System;

namespace KIOSK.Infrastructure.Database.Ef.Entities;

public sealed class ApiConfigEntity
{
    public long Id { get; set; }
    public string? KioskId { get; set; }
    public string? ServerName { get; set; }
    public string? ServerUrl { get; set; }
    public string? ServerKey { get; set; }
    public int? TimeoutSeconds { get; set; }
    public bool IsValid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
