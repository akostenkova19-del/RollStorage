using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace RollStorage.Controllers;

[ApiController]
[Route("api/[controller]")]

public class RollsController(Db context) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Roll>> Add(decimal length, decimal weight)
    {
        if (length <= 0 || weight <= 0)
            return BadRequest("Длина и вес должны быть больше нуля.");

        var roll = new Roll
        {
            Id = Guid.NewGuid(),
            Length = length,
            Weight = weight,
            ArrivalDate = DateTime.Now
        };

        context.Rolls.Add(roll);
        await context.SaveChangesAsync();
        return Ok(roll);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<Roll>> Delete(Guid id)
    {
        var roll = await context.Rolls.FindAsync(id);
        if (roll == null) return NotFound("Рулон не найден");

        roll.DepartureDate = DateTime.Now;
        await context.SaveChangesAsync();
        return Ok(roll);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Roll>>> GetList(
        Guid? id, decimal? minWeight, decimal? maxWeight,
        decimal? minLength, decimal? maxLength,
        DateTime? arrivalFrom, DateTime? arrivalTo)
    {
        var query = context.Rolls.AsQueryable();

        if (id.HasValue) query = query.Where(r => r.Id == id);
        if (minWeight.HasValue) query = query.Where(r => r.Weight >= minWeight);
        if (maxWeight.HasValue) query = query.Where(r => r.Weight <= maxWeight);
        if (minLength.HasValue) query = query.Where(r => r.Length >= minLength);
        if (maxLength.HasValue) query = query.Where(r => r.Length <= maxLength);
        if (arrivalFrom.HasValue) query = query.Where(r => r.ArrivalDate >= arrivalFrom);
        if (arrivalTo.HasValue) query = query.Where(r => r.ArrivalDate <= arrivalTo);

        return await query.ToListAsync();
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(DateTime start, DateTime end)
    {
        var allRolls = await context.Rolls.ToListAsync();
        var rollsInPeriod = allRolls.Where(r =>
            r.ArrivalDate <= end && (r.DepartureDate == null || r.DepartureDate >= start)
        ).ToList();

        if (!rollsInPeriod.Any()) return NotFound("За указанный период данных нет.");

        var stayDurations = rollsInPeriod
            .Where(r => r.DepartureDate.HasValue)
            .Select(r => r.DepartureDate.Value - r.ArrivalDate)
            .ToList();

        var daysInRange = Enumerable.Range(0, (end.Date - start.Date).Days + 1)
            .Select(d => start.Date.AddDays(d))
            .Select(day => new {
                Date = day,
                Count = rollsInPeriod.Count(r => r.ArrivalDate.Date <= day && (r.DepartureDate == null || r.DepartureDate.Value.Date >= day)),
                TotalWeight = rollsInPeriod.Where(r => r.ArrivalDate.Date <= day && (r.DepartureDate == null || r.DepartureDate.Value.Date >= day)).Sum(r => r.Weight)
            }).ToList();

        var stats = new
        {
            AddedCount = rollsInPeriod.Count(r => r.ArrivalDate >= start && r.ArrivalDate <= end),
            RemovedCount = rollsInPeriod.Count(r => r.DepartureDate >= start && r.DepartureDate <= end),
            AverageLength = rollsInPeriod.Average(r => r.Length),
            AverageWeight = rollsInPeriod.Average(r => r.Weight),
            MaxWeight = rollsInPeriod.Max(r => r.Weight),
            MinWeight = rollsInPeriod.Min(r => r.Weight),
            MaxLength = rollsInPeriod.Max(r => r.Length),
            MinLength = rollsInPeriod.Min(r => r.Length),
            TotalWeightInStock = rollsInPeriod.Sum(r => r.Weight),

            MaxStayDuration = stayDurations.Any() ? stayDurations.Max().ToString() : "N/A",
            MinStayDuration = stayDurations.Any() ? stayDurations.Min().ToString() : "N/A",

            DayWithMaxRolls = daysInRange.OrderByDescending(d => d.Count).First().Date,
            DayWithMinRolls = daysInRange.OrderBy(d => d.Count).First().Date,
            DayWithMaxWeight = daysInRange.OrderByDescending(d => d.TotalWeight).First().Date,
            DayWithMinWeight = daysInRange.OrderBy(d => d.TotalWeight).First().Date
        };

        return Ok(stats);
    }
}