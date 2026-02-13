using System.Security.Claims;
using System.Text.Json;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
/* Controller to manage travel destinations */
public class DestinationsController(AppDbContext context) : ControllerBase
{
    /** Database context */
    private readonly AppDbContext _context = context;

    /* Helper to get the current user's ID */
    private string? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim?.Value;
    }

    /* Endpoint to get all destinations for the current user */
    [HttpGet]
    public async Task<IEnumerable<Destination>> GetAll()
    {
        /* Basic validation */
        var userId = GetUserId() ?? throw new UnauthorizedAccessException();

        /* Fetch destinations for the user */
        return await _context.Destinations
            .Where(d => d.UserId == userId)
            .ToListAsync();
    }

    /* Endpoint to create a new destination */
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Destination d)
    {
        User.Claims.ToList().ForEach(c =>
        Console.WriteLine($"Claim: {c.Type} = {c.Value}")
        );

        /* Basic validation */
        var userId = GetUserId();
        Console.WriteLine(userId ?? "No user ID found");
        if (userId == null)
            return Unauthorized();

        /* Add the new destination */
        d.UserId = userId;
        _context.Destinations.Add(d);
        await _context.SaveChangesAsync();
        return Ok(d);
    }

    /* Endpoint to update a destination */
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Destination updated)
    {
        /* Basic validation */
        var userId = GetUserId();
        var existing = await _context.Destinations.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
        if (existing == null) return NotFound();

        /* Update fields */
        existing.City = updated.City;
        existing.Country = updated.Country;
        existing.Notes = updated.Notes;
        existing.Visited = updated.Visited;
        existing.Latitude = updated.Latitude;
        existing.Longitude = updated.Longitude;

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    /* Endpoint to delete a destination */
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        /* Basic validation */
        var userId = GetUserId();
        var dest = await _context.Destinations.FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);
        if (dest == null) return NotFound();

        /* Delete the destination */
        _context.Destinations.Remove(dest);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /* Endpoint to get statistics */
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        /* Basic validation */
        var userId = GetUserId();
        var total = await _context.Destinations.CountAsync(d => d.UserId == userId);
        var visited = await _context.Destinations.CountAsync(d => d.UserId == userId && d.Visited);
        /* Calculate wishlist count */
        var wishlist = total - visited;
        return Ok(new { total, visited, wishlist });
    }

    /* Endpoint to export data as CSV or JSON */
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string format = "csv")
    {
        var userId = GetUserId();
        var destinations = await _context.Destinations
            .Where(d => d.UserId == userId)
            .ToListAsync();

        if (destinations.Count == 0)
            return BadRequest("No destinations to export.");

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var json = JsonSerializer.Serialize(destinations);
            return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", "travel-wishlist.json");
        }

        if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("City,Country,Notes,Visited,Latitude,Longitude");

            foreach (var d in destinations)
            {
                var notes = d.Notes?.Replace(",", " ") ?? "";
                csv.AppendLine($"{d.City},{d.Country},{notes},{d.Visited},{d.Latitude},{d.Longitude}");
            }

            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "travel-wishlist.csv");
        }

        return BadRequest("Unsupported format. Use csv or json.");
    }
}
