using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Synquid.API.Extensions;
using Synquid.Domain.Entities;
using Synquid.Infrastructure.Data;
using Synquid.Application.Interfaces;

namespace Synquid.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroupController : ControllerBase
{
    private readonly SynquidDbContext _context;
    private readonly IConfiguration _config;

    public GroupController(SynquidDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpGet]
    public async Task<ActionResult<List<Group>>> GetAll()
    {
        try
        {
            return await _context.Groups
                .Where(g => g.IsActive)
                .OrderBy(g => g.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Group>> GetById(Guid id)
    {
        try
        {
            Group? group = await _context.Groups.FindAsync(id);
            if (group == null) return NotFound();
            return group;
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Group>> Create(Group group)
    {
        try
        {
            group.Id = Guid.NewGuid();
            group.CreatedAt = DateTime.UtcNow;
            group.IsActive = true;

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = group.Id }, group);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, Group updated)
    {
        try
        {
            Group? group = await _context.Groups.FindAsync(id);
            if (group == null) return NotFound();

            group.Name = updated.Name;
            group.Level = updated.Level;
            group.InstitutionId = updated.InstitutionId;
            group.ProfessorId = updated.ProfessorId;
            group.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            Group? group = await _context.Groups.FindAsync(id);
            if (group == null) return NotFound();

            group.IsActive = false;
            group.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpGet("{id}/members")]
    public async Task<ActionResult> GetGroupMembers(Guid id)
    {
        try
        {
            Group? group = await _context.Groups.FindAsync(id);
            if (group == null) return NotFound("Grupo no encontrado");

            var users = await _context.GroupMembers
                .Include(gm => gm.User)
                .Where(gm => gm.GroupId == id && gm.IsActive)
                .Select(gm => gm.User)
                .OrderBy(u => u.FirstName)
                .ToListAsync();

            return Ok(new
            {
                codigoError = 0,
                groupId = id,
                totalMembers = users.Count,
                members = users,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }
    [HttpPost("{id}/members")]
    public async Task<ActionResult> AddMemberToGroup(Guid id, [FromBody] assignUserToGroup request)
    {
        try
        {
            Group? group = await _context.Groups.FindAsync(id);
            if (group == null) return NotFound("Grupo no encontrado");

            User? user = await _context.Users.FindAsync(Guid.Parse(request.userId));
            if (user == null) return NotFound("Usuario no encontrado");

            // Verificar si ya existe en el grupo y está activo
            bool alreadyExists = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == id && gm.UserId == Guid.Parse(request.userId) && gm.IsActive);

            if (alreadyExists)
                return BadRequest("El usuario ya es miembro de este grupo");

            // Comprobar si existe inactivo para reactivarlo
            GroupMember? existingMember = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == id && gm.UserId == Guid.Parse(request.userId) && !gm.IsActive);

            if (existingMember != null)
            {
                existingMember.IsActive = true;
                existingMember.JoinedAt = DateTime.UtcNow;
            }
            else
            {
                GroupMember newMember = new GroupMember
                {
                    Id = Guid.NewGuid(),
                    GroupId = id,
                    UserId = Guid.Parse(request.userId),
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.GroupMembers.Add(newMember);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                codigoError = 0,
                message = "Usuario añadido al grupo correctamente",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<ActionResult> RemoveMemberFromGroup(Guid id, Guid userId)
    {
        try
        {
            GroupMember? member = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == id && gm.UserId == userId && gm.IsActive);

            if (member == null) return NotFound("El usuario no es miembro de este grupo");

            member.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                codigoError = 0,
                message = "Usuario eliminado del grupo correctamente",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
        }
    }
}

public class assignUserToGroup
{
    public string userId { get; set; } = string.Empty;
}
