namespace FitCycle.Core.Models;

public class BodyMeasurement
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime MeasuredAt { get; set; }
    public decimal? Weight { get; set; }       // kg
    public decimal? Height { get; set; }       // cm
    public decimal? Chest { get; set; }        // cm
    public decimal? Waist { get; set; }        // cm
    public decimal? Hips { get; set; }         // cm
    public decimal? BicepLeft { get; set; }    // cm
    public decimal? BicepRight { get; set; }   // cm
    public decimal? ThighLeft { get; set; }    // cm
    public decimal? ThighRight { get; set; }   // cm
    public decimal? CalfLeft { get; set; }     // cm
    public decimal? CalfRight { get; set; }    // cm
    public decimal? Neck { get; set; }         // cm
    public decimal? BodyFat { get; set; }      // percentage
    public string? Notes { get; set; }
}
