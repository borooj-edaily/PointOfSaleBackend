using MediatR;
using Microsoft.AspNetCore.Mvc;
using Pos.Api.Features.Products.Create;
using Pos.Api.Features.Products.Deactivate;
using Pos.Api.Features.Products.GetAll;
using Pos.Api.Features.Products.GetById;
using Pos.Api.Features.Products.LowStock;

namespace Pos.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProductsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductCommand command)
        {
            var newId = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? categoryId = null,
            [FromQuery] bool onlyActive = false,
            [FromQuery] string? search = null)
        {
            var result = await _mediator.Send(new GetAllProductsQuery
            {
                CategoryId = categoryId,
                OnlyActive = onlyActive,
                SearchTerm = search
            });

            return Ok(result);
        }
        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStock(
             [FromQuery] int threshold = 10,
             [FromQuery] bool onlyOutOfStock = false)
        {
            var result = await _mediator.Send(new GetLowStockProductsQuery
            {
                Threshold = threshold,
                OnlyOutOfStock = onlyOutOfStock
            });

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _mediator.Send(new GetProductByIdQuery { Id = id });

            if (result is null)
                return NotFound(new { message = $"لا يوجد صنف بالمعرف {id}" });

            return Ok(result);
        }

        [HttpPatch("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id, [FromBody] DeactivateProductCommand command)
        {
            command.Id = id;
            await _mediator.Send(command);
            return NoContent();
        }

    }
}