using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Api.Features.Customers.Create;
using Pos.Api.Features.Customers.Deactivate;
using Pos.Api.Features.Customers.DebtSummary;
using Pos.Api.Features.Customers.GetAll;
using Pos.Api.Features.Customers.GetById;
using Pos.Api.Features.Customers.Update;
using Pos.Api.Security;

namespace Pos.Api.Controllers
{
    // إدارة ملفات الزبائن — جزء من Dept Notebook (v2). محمية بنفس صلاحية
    // record_debt: نفس الشخص يلي يقدر يسجل دين هو يلي يقدر يفتح/يعدّل ملف زبون.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = Permissions.RecordDebt)]
    public class CustomersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CustomersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCustomerCommand command)
        {
            command.CreatedByUserId = CurrentUserId();

            var newId = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId });
        }

        /// <summary>
        /// كل الزبائن، مع فلترة اختيارية بالاسم/التلفون (search) — مستخدمة من
        /// شاشة الكاشير كـ typeahead عند تسجيل دين، ومن صفحة إدارة الزبائن.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool onlyActive = false, [FromQuery] string? search = null)
        {
            var result = await _mediator.Send(new GetAllCustomersQuery { OnlyActive = onlyActive, Search = search });
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _mediator.Send(new GetCustomerByIdQuery { Id = id });

            if (result is null)
                return NotFound(new { message = $"لا يوجد زبون بالمعرف {id}" });

            return Ok(result);
        }

        /// <summary>"ملف الزبون": كامل تاريخ ديونه (مسددة وغير مسددة).</summary>
        [HttpGet("{id:int}/debts")]
        public async Task<IActionResult> GetDebtHistory(int id, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetCustomerDebtHistoryQuery { CustomerId = id }, ct);
            return Ok(result);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerCommand command)
        {
            command.Id = id;
            command.UpdatedByUserId = CurrentUserId();
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpPost("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var command = new DeactivateCustomerCommand { Id = id, UpdatedByUserId = CurrentUserId() };
            await _mediator.Send(command);
            return NoContent();
        }

        private int CurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(value, out var userId))
                throw new UnauthorizedAccessException("The user ID claim is missing.");

            return userId;
        }
    }
}
