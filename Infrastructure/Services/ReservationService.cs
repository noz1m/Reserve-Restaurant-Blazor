using System.Net;
using AutoMapper;
using Domain.DTOs.ReservationDTOs;
using Domain.Entities;
using Domain.Filters;
using Domain.Response;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class ReservationService(IDbContextFactory<DataContext> dbFactory, IMapper mapper, ICustomerService customerService) : IReservationService
{
    public async Task<Response<GetReservationDTO>> CreateAsync(CreateReservationDTO createReservation)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        createReservation.ReservationDateFrom = createReservation.ReservationDateFrom.ToUniversalTime();
        createReservation.ReservationDateTo = createReservation.ReservationDateTo.ToUniversalTime();

        var customerResult = await customerService.GetByIdAsync(createReservation.CustomerId);
        if (!customerResult.IsSuccess)
        {
            return new Response<GetReservationDTO>(HttpStatusCode.BadRequest, "Customer does not exist");
        }

        if (createReservation.ReservationDateFrom >= createReservation.ReservationDateTo)
        {
            return new Response<GetReservationDTO>(HttpStatusCode.BadRequest, "Reservation start time must be before end time");
        }

        if (createReservation.ReservationDateFrom < DateTime.UtcNow)
            return new Response<GetReservationDTO>(HttpStatusCode.BadRequest, "Reservation cannot start in the past");

        if (!await context.Tables.AnyAsync(t => t.Id == createReservation.TableId))
            return new Response<GetReservationDTO>(HttpStatusCode.NotFound, "Table does not exist");

        // Intervals [start, end) overlap exactly when each starts before the other ends.
        if (await context.Reservations.AnyAsync(r =>
            r.TableId == createReservation.TableId &&
            r.ReservationDateFrom < createReservation.ReservationDateTo &&
            r.ReservationDateTo > createReservation.ReservationDateFrom))
            return new Response<GetReservationDTO>(HttpStatusCode.Conflict,
                "This table is already reserved for the selected time");

        var reservation = mapper.Map<Reservation>(createReservation);

        await context.Reservations.AddAsync(reservation);
        var result = await context.SaveChangesAsync();

        if (result == 0)
            return new Response<GetReservationDTO>(HttpStatusCode.InternalServerError, "Reservation couldn't be added");

        var getReservation = mapper.Map<GetReservationDTO>(reservation);

        return new Response<GetReservationDTO>(getReservation);
    }

    public async Task<Response<string>> DeleteAsync(int reservationId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var reservation = await context.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId);
        if (reservation == null)
            return new Response<string>(HttpStatusCode.NotFound, "Reservation not found");

        context.Reservations.Remove(reservation);
        var result = await context.SaveChangesAsync();

        return result == 0
            ? new Response<string>(HttpStatusCode.InternalServerError, "Reservation couldn't be deleted")
            : new Response<string>("Reservation deleted successfully");
    }

    public async Task<PagedResponse<List<GetReservationDTO>>> GetAllAsync(ReservationFilter filter)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var query = context.Reservations.AsNoTracking().AsQueryable();

        if (filter.TableId != null)
        {
            query = query.Where(r => r.TableId == filter.TableId);
        }

        if (filter.CustomerId != null)
        {
            query = query.Where(r => r.CustomerId == filter.CustomerId);
        }

        if (filter.ReservationDate != null)
        {
            var date = DateTime.SpecifyKind(filter.ReservationDate.Value.Date, DateTimeKind.Utc);
            var nextDate = date.AddDays(1);

            query = query.Where(r => r.ReservationDateFrom >= date && r.ReservationDateFrom < nextDate);
        }   

        var totalRecords = await query.CountAsync();

        var reservations = await query
            .OrderBy(r => r.ReservationDateFrom)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var getReservations = mapper.Map<List<GetReservationDTO>>(reservations);

        return new PagedResponse<List<GetReservationDTO>>(getReservations, pageNumber, pageSize, totalRecords);
    }
}