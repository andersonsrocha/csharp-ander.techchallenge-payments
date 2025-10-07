using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechChallengePayments.Application.Payments.Commands;
using TechChallengePayments.Application.Payments.Interfaces;
using TechChallengePayments.Domain.Dto;

namespace TechChallengePayments.Api.Controllers;

[Route("api/[controller]")]
public class PaymentsController(IPaymentService service, ILogger<PaymentsController> logger) : BaseController(logger)
{
    [HttpGet]
    [Authorize("User")]
    [Route("{id:Guid}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Get([FromRoute] Guid id)
        => Send(service.Find(id));
    
    [HttpGet]
    [Authorize("User")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Get()
        => Send(service.Find());
    
    [HttpPost]
    [Authorize("Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] CreatePaymentRequest request)
        => await Send(service.CreateAsync(request));
    
    [HttpPut]
    [Authorize("Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Put([FromBody] UpdatePaymentRequest request)
        => await Send(service.UpdateAsync(request));
    
    [HttpDelete]
    [Authorize("Admin")]
    [Route("{id:Guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
        => await Send(service.DeleteAsync(id));
}