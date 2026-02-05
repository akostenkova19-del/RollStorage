namespace RollStorage;

public class Roll
{
    public Guid Id { get; set; }
    public decimal Length { get; set; }
    public decimal Weight { get; set; }
    public DateTime ArrivalDate { get; set; }
    public DateTime? DepartureDate { get; set; }
}