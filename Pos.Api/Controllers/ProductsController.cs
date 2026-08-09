using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Api.Features.Products.Create;
using Pos.Api.Features.Products.Deactivate;
using Pos.Api.Features.Products.GetAll;
using Pos.Api.Features.Products.GetById;
using Pos.Api.Features.Products.LowStock;
using Pos.Api.Features.Products.Update;
using Pos.Api.Security;

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

        [Authorize(Policy = Permissions.ManageProducts)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductCommand command)
        {
            // الـ CreatedByUserId بيتحدد من التوكن دايماً، مش من الـ body، حتى ما
            // يقدر حدا يسجّل الإنشاء باسم مستخدم تاني.
            command.CreatedByUserId = CurrentUserId();

            var newId = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId });
        }

        // القراءة (GetAll/GetById/GetLowStock) متاحة لأي مستخدم مسجّل دخول (الكاشير محتاجها
        // لعرض الأصناف بشاشة البيع)، بدون صلاحية إضافية.
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

        [Authorize(Policy = Permissions.ManageProducts)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductCommand command)
        {
            command.Id = id;
            command.UpdatedByUserId = CurrentUserId();
            await _mediator.Send(command);
            return NoContent();
        }

        [Authorize(Policy = Permissions.ManageProducts)]
        [HttpPatch("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id, [FromBody] DeactivateProductCommand command)
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