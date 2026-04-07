using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Synquid.Domain.Entities;
using Synquid.Infrastructure.Data;

namespace Synquid.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InstitutionsController : ControllerBase
{
    private readonly SynquidDbContext _context;

    public InstitutionsController(SynquidDbContext context)
    {
        _context = context;
    }

    // GET api/institutions
    [HttpGet]
    public async Task<ActionResult<List<Institution>>> GetAll()
    {
        return await _context.Institutions
            .Where(i => i.IsActive)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    // GET api/institutions/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Institution>> GetById(Guid id)
    {
        Institution? institution = await _context.Institutions.FindAsync(id);
        if (institution == null) return NotFound();
        return institution;
    }

    // POST api/institutions
    [HttpPost]
    public async Task<ActionResult<Institution>> Create(Institution institution)
    {
        institution.Id = Guid.NewGuid();
        institution.CreatedAt = DateTime.UtcNow;

        _context.Institutions.Add(institution);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = institution.Id }, institution);
    }

    // PUT api/institutions/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, Institution updated)
    {
        Institution? institution = await _context.Institutions.FindAsync(id);
        if (institution == null) return NotFound();

        institution.Name = updated.Name;
        institution.Address = updated.Address;
        institution.Phone = updated.Phone;
        institution.ContactEmail = updated.ContactEmail;
        institution.Type = updated.Type;
        institution.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE api/institutions/{id} (soft delete)
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        Institution? institution = await _context.Institutions.FindAsync(id);
        if (institution == null) return NotFound();

        institution.IsActive = false;
        institution.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }
}