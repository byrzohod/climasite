using ClimaSite.Application.Features.Gdpr.Commands;
using ClimaSite.Application.Features.Gdpr.DTOs;
using ClimaSite.Application.Features.Gdpr.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClimaSite.Api.Controllers;

/// <summary>
/// GDPR compliance endpoints for user data management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GdprController : ControllerBase
{
    private readonly IMediator _mediator;

    public GdprController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Export all user data (GDPR Article 20 - Right to data portability)
    /// </summary>
    /// <returns>Complete export of all user data in JSON format</returns>
    [HttpGet("export")]
    [Authorize]
    [ProducesResponseType(typeof(UserDataExportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExportData()
    {
        var result = await _mediator.Send(new ExportUserDataQuery());

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        // Set filename for download
        Response.Headers.Append("Content-Disposition", 
            $"attachment; filename=\"climasite-data-export-{DateTime.UtcNow:yyyy-MM-dd}.json\"");

        return Ok(result.Value);
    }

    /// <summary>
    /// Delete user account and all associated data (GDPR Article 17 - Right to erasure)
    /// </summary>
    /// <param name="request">Deletion request with password confirmation</param>
    /// <returns>Confirmation of deletion</returns>
    [HttpDelete("delete")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        var command = new DeleteUserDataCommand
        {
            Password = request.Password,
            Reason = request.Reason,
            ConfirmDeletion = request.ConfirmDeletion
        };

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new 
        { 
            message = "Your account and data have been successfully deleted. A confirmation email has been sent.",
            deletedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get list of data categories collected (GDPR Article 13/14 - Right to be informed)
    /// </summary>
    /// <returns>List of data categories with purposes and retention periods</returns>
    [HttpGet("data-categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<DataCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDataCategories()
    {
        var categories = await _mediator.Send(new GetDataCategoriesQuery());
        return Ok(categories);
    }

    /// <summary>
    /// Get GDPR policy summary
    /// </summary>
    /// <returns>Summary of GDPR rights and how to exercise them</returns>
    [HttpGet("rights")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetGdprRights()
    {
        return Ok(new
        {
            rights = new[]
            {
                new
                {
                    right = "Right to Access",
                    article = "Article 15",
                    description = "You can request a copy of all personal data we hold about you.",
                    howToExercise = "Use the GET /api/gdpr/export endpoint while logged in."
                },
                new
                {
                    right = "Right to Rectification",
                    article = "Article 16",
                    description = "You can update or correct your personal data at any time.",
                    howToExercise = "Update your profile in account settings or contact support."
                },
                new
                {
                    right = "Right to Erasure",
                    article = "Article 17",
                    description = "You can request deletion of your personal data.",
                    howToExercise = "Use the DELETE /api/gdpr/delete endpoint while logged in."
                },
                new
                {
                    right = "Right to Data Portability",
                    article = "Article 20",
                    description = "You can receive your data in a structured, machine-readable format.",
                    howToExercise = "Use the GET /api/gdpr/export endpoint to download your data as JSON."
                },
                new
                {
                    right = "Right to Object",
                    article = "Article 21",
                    description = "You can object to processing of your data for marketing purposes.",
                    howToExercise = "Update your notification preferences or contact support."
                },
                new
                {
                    right = "Right to Restrict Processing",
                    article = "Article 18",
                    description = "You can request that we limit how we use your data.",
                    howToExercise = "Contact our support team with your specific request."
                }
            },
            dataProtectionOfficer = new
            {
                email = "dpo@climasite.com",
                responseTime = "We will respond to your request within 30 days."
            },
            lastUpdated = "2024-01-01"
        });
    }
}
