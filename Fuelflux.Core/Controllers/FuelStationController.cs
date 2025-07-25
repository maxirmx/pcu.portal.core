using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Fuelflux.Core.Authorization;
using Fuelflux.Core.RestModels;
using Fuelflux.Core.Data;
using Fuelflux.Core.Models;
using Fuelflux.Core.Services;

namespace Fuelflux.Core.Controllers;

[ApiController]
[Authorize(AuthorizationType.User)]
[Route("api/[controller]")]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrMessage))]
public class FuelStationController(
    IHttpContextAccessor httpContextAccessor,
    AppDbContext db,
    IUserInformationService userInformationService,
    ILogger<FuelStationController> logger) : FuelfluxControllerBase(httpContextAccessor, db, logger)
{
    private readonly IUserInformationService _uis = userInformationService;

    #region FuelStation
    [HttpGet("stations")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FuelStationItem>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    public async Task<ActionResult<IEnumerable<FuelStationItem>>> GetStations()
    {
        if (!await _uis.CheckAdmin(_curUserId)) return _403();
        var items = await _db.FuelStations.AsNoTracking()
            .Select(fs => new FuelStationItem(fs)).ToListAsync();
        return items;
    }

    [HttpGet("stations/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FuelStationItem))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrMessage))]
    public async Task<ActionResult<FuelStationItem>> GetStation(int id)
    {
        if (!await _uis.CheckAdmin(_curUserId)) return _403();
        var fs = await _db.FuelStations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (fs == null) return _404FuelStation(id);
        return new FuelStationItem(fs);
    }

    [HttpPost("stations")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Reference))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    public async Task<ActionResult<Reference>> PostStation(FuelStationCreateItem item)
    {
        if (!await _uis.CheckAdmin(_curUserId)) return _403();
        var fs = new FuelStation { Name = item.Name };
        _db.FuelStations.Add(fs);
        await _db.SaveChangesAsync();
        var reference = new Reference { Id = fs.Id };
        return CreatedAtAction(nameof(PostStation), new { id = fs.Id }, reference);
    }

    [HttpPut("stations/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrMessage))]
    public async Task<IActionResult> PutStation(int id, FuelStationUpdateItem item)
    {
        if (!await _uis.CheckAdmin(_curUserId)) return _403();
        var fs = await _db.FuelStations.FindAsync(id);
        if (fs == null) return _404FuelStation(id);
        if (item.Name != null) fs.Name = item.Name;
        _db.Entry(fs).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("stations/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrMessage))]
    public async Task<IActionResult> DeleteStation(int id)
    {
        if (!await _uis.CheckAdmin(_curUserId)) return _403();
        var fs = await _db.FuelStations.FindAsync(id);
        if (fs == null) return _404FuelStation(id);
        _db.FuelStations.Remove(fs);
        await _db.SaveChangesAsync();
        return NoContent();
    }
    #endregion

    #region FuelTank
    [HttpGet("tanks")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FuelTankViewItem>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    public async Task<ActionResult<IEnumerable<FuelTankViewItem>>> GetTanks()
    {
        if (!await _uis.CheckAdmin(_curUserId)) return _403();
        var items = await _db.FuelTanks.AsNoTracking()
            .Select(t => new FuelTankViewItem(t)).ToListAsync();
        return items;
    }

    [HttpGet("tanks/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FuelTankViewItem))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrMessage))]
    public async Task<ActionResult<FuelTankViewItem>> GetTank(int id)
    {
        if (!await _uis.CheckAdmin(_curUserId)) return _403();
        var tank = await _db.FuelTanks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (tank == null) return _404FuelTank(id);
        return new FuelTankViewItem(tank);
    }

    [HttpPost("tanks")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Reference))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    public async Task<ActionResult<Reference>> PostTank(FuelTankCreateItem item)
    {
        if (!await _uis.CheckAdmin(_curUserId)) return _403();
        var tank = new FuelTank
        {
            Number = item.Number,
            Volume = item.Volume,
            FuelStationId = item.FuelStationId
        };
        _db.FuelTanks.Add(tank);
        await _db.SaveChangesAsync();
        var reference = new Reference { Id = tank.Id };
        return CreatedAtAction(nameof(PostTank), new { id = tank.Id }, reference);
    }

    [HttpPut("tanks/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrMessage))]
    public async Task<IActionResult> PutTank(int id, FuelTankUpdateItem item)
    {
        if (!await _uis.CheckAdmin(_curUserId)) return _403();
        var tank = await _db.FuelTanks.FindAsync(id);
        if (tank == null) return _404FuelTank(id);
        if (item.Number != null) tank.Number = item.Number.Value;
        if (item.Volume != null) tank.Volume = item.Volume.Value;
        if (item.FuelStationId != null) tank.FuelStationId = item.FuelStationId.Value;
        _db.Entry(tank).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("tanks/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrMessage))]
    public async Task<IActionResult> DeleteTank(int id)
    {
        if (!await _uis.CheckAdmin(_curUserId)) return _403();
        var tank = await _db.FuelTanks.FindAsync(id);
        if (tank == null) return _404FuelTank(id);
        _db.FuelTanks.Remove(tank);
        await _db.SaveChangesAsync();
        return NoContent();
    }
    #endregion

    #region PumpController
    [HttpGet("pumps")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PumpControllerItem>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    public async Task<ActionResult<IEnumerable<PumpControllerItem>>> GetPumps()
    {
        if (!await _uis.CheckAdmin(_curUserId)) return _403();
        var items = await _db.PumpControllers.AsNoTracking()
            .Select(p => new PumpControllerItem(p)).ToListAsync();
        return items;
    }

    [HttpGet("pumps/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PumpControllerItem))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrMessage))]
    public async Task<ActionResult<PumpControllerItem>> GetPump(int id)
    {
        if (!await _uis.CheckAdmin(_curUserId)) return _403();
        var pump = await _db.PumpControllers.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (pump == null) return _404PumpController(id);
        return new PumpControllerItem(pump);
    }

    [HttpPost("pumps")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Reference))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    public async Task<ActionResult<Reference>> PostPump(PumpControllerCreateItem item)
    {
        if (!await _uis.CheckAdmin(_curUserId)) return _403();
        var pump = new PumpCntrl
        {
            Uid = item.Uid,
            FuelStationId = item.FuelStationId
        };
        _db.PumpControllers.Add(pump);
        await _db.SaveChangesAsync();
        var reference = new Reference { Id = pump.Id };
        return CreatedAtAction(nameof(PostPump), new { id = pump.Id }, reference);
    }

    [HttpPut("pumps/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrMessage))]
    public async Task<IActionResult> PutPump(int id, PumpControllerUpdateItem item)
    {
        if (!await _uis.CheckAdmin(_curUserId)) return _403();
        var pump = await _db.PumpControllers.FindAsync(id);
        if (pump == null) return _404PumpController(id);
        if (item.Uid != null) pump.Uid = item.Uid;
        if (item.FuelStationId != null) pump.FuelStationId = item.FuelStationId.Value;
        _db.Entry(pump).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("pumps/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrMessage))]
    public async Task<IActionResult> DeletePump(int id)
    {
        if (!await _uis.CheckAdmin(_curUserId)) return _403();
        var pump = await _db.PumpControllers.FindAsync(id);
        if (pump == null) return _404PumpController(id);
        _db.PumpControllers.Remove(pump);
        await _db.SaveChangesAsync();
        return NoContent();
    }
    #endregion
}
