using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonAppointmentApi.Data;
using SalonAppointmentApi.Dtos;
using SalonAppointmentApi.Models;

namespace SalonAppointmentApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReviewsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateReview(
        [FromBody] CreateReviewRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(
                a => a.Id == request.AppointmentId
            );

        if (appointment == null)
        {
            return NotFound("Randevu bulunamadı.");
        }

        if (appointment.Status != "Completed")
        {
            return BadRequest(
                "Sadece tamamlanan randevular için yorum yapılabilir."
            );
        }

        if (!appointment.CustomerUserId.HasValue)
        {
            return BadRequest(
                "Bu randevuya bağlı müşteri hesabı bulunamadı."
            );
        }

        var existingReview = await _context.Reviews
            .AnyAsync(
                r => r.AppointmentId == request.AppointmentId
            );

        if (existingReview)
        {
            return BadRequest(
                "Bu randevu için zaten yorum yapılmış."
            );
        }

        var review = new Review
        {
            AppointmentId = appointment.Id,
            BusinessId = appointment.BusinessId,

            // Appointment modelindeki CustomerUserId,
            // Reviews tablosundaki CustomerId alanına yazılıyor.
            CustomerId = appointment.CustomerUserId.Value,

            EmployeeId = appointment.EmployeeId,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment)
                ? null
                : request.Comment.Trim(),
            CreatedAt = DateTime.Now
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Yorum başarıyla eklendi.",
            reviewId = review.Id
        });
    }

    [HttpGet("business/{businessId:int}")]
    public async Task<IActionResult> GetReviewsByBusiness(
        int businessId)
    {
        var businessExists = await _context.Businesses
            .AnyAsync(b => b.Id == businessId);

        if (!businessExists)
        {
            return NotFound("İşletme bulunamadı.");
        }

        var reviews = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.BusinessId == businessId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.AppointmentId,
                r.Rating,
                r.Comment,
                r.CreatedAt,

                CustomerName = r.Customer != null
                    ? r.Customer.FullName
                    : "Müşteri",

                EmployeeName = r.Employee != null
                    ? r.Employee.FullName
                    : null
            })
            .ToListAsync();

        return Ok(reviews);
    }

    [HttpGet("business/{businessId:int}/average")]
    public async Task<IActionResult> GetBusinessAverageRating(
        int businessId)
    {
        var ratingData = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.BusinessId == businessId)
            .GroupBy(r => r.BusinessId)
            .Select(group => new
            {
                AverageRating = group.Average(r => r.Rating),
                ReviewCount = group.Count()
            })
            .FirstOrDefaultAsync();

        if (ratingData == null)
        {
            return Ok(new
            {
                averageRating = 0,
                reviewCount = 0
            });
        }

        return Ok(new
        {
            averageRating = Math.Round(
                ratingData.AverageRating,
                1
            ),
            reviewCount = ratingData.ReviewCount
        });
    }
}