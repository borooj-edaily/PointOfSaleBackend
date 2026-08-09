using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Api.Features.Categories.Create;
using Pos.Api.Features.Categories.Deactivate;
using Pos.Api.Features.Categories.GetAll;
using Pos.Api.Features.Categories.GetById;
using Pos.Api.Features.Categories.Update;
using Pos.Api.Security;

namespace Pos.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CategoriesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Policy = Permissions.ManageProducts)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryCommand command)
        {
            command.CreatedByUserId = CurrentUserId();

            var newId = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId });
        }

        // القراءة متاحة لأي مستخدم مسجّل دخول (الكاشير محتاجها لعرض الكاتيجوريز بشاشة البيع).
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool onlyActive = false)
        {
            var result = await _mediator.Send(new GetAllCategoriesQuery { OnlyActive = onlyActive });
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _mediator.Send(new GetCategoryByIdQuery { Id = id });

            if (result is null)
                return NotFound(new { message = $"لا يوجد كاتيجوري بالمعرف {id}" });

            return Ok(result);
        }

        [Authorize(Policy = Permissions.ManageProducts)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryCommand command)
        {
            command.Id = id;
            command.UpdatedByUserId = CurrentUserId();
            await _mediator.Send(command);
            return NoContent();
        }

        [Authorize(Policy = Permissions.ManageProducts)]
        [HttpPatch("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id, [FromBody] DeactivateCategoryCommand command)
        {
            command.Id = id;
            command.UpdatedByUserId = CurrentUserId();
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