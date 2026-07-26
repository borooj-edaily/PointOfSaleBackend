using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pos.Api.Features.Categories.Create;
using Pos.Api.Features.Categories.Deactivate;
using Pos.Api.Features.Categories.GetAll;
using Pos.Api.Features.Categories.GetById;

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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryCommand command)
        {
            var newId = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId });
        }

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

        [HttpPatch("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id, [FromBody] DeactivateCategoryCommand command)
        {
            command.Id = id;
            await _mediator.Send(command);
            return NoContent();
        }
    }
}