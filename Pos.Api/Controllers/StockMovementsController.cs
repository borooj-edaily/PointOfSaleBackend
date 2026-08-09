using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pos.Api.Features.StockMovements.CurrentStock;
using Pos.Api.Features.StockMovements.Deduct;
using Pos.Api.Features.StockMovements.GetHistory;
using Pos.Api.Features.StockMovements.Restock;
using Pos.Api.Security;

namespace Pos.Api.Controllers
{
    [ApiController]
    [Route("api/products/{productId:int}/stock")]
    public class StockMovementsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public StockMovementsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentStock(int productId)
        {
            var result = await _mediator.Send(new GetCurrentStockQuery { ProductId = productId });
            return Ok(result);
        }

        [Authorize(Policy = Permissions.ManageInventory)]
        [HttpPost("restock")]
        public async Task<IActionResult> Restock(int productId, [FromBody] RestockCommand command)
        {
            command.ProductId = productId;
            var movementId = await _mediator.Send(command);
            return Ok(new { movementId });
        }

        [Authorize(Policy = Permissions.ManageInventory)]
        [HttpPost("deduct")]
        public async Task<IActionResult> Deduct(int productId, [FromBody] DeductStockCommand command)
        {
            command.ProductId = productId;
            var movementId = await _mediator.Send(command);
            return Ok(new { movementId });
        }

        [Authorize(Policy = Permissions.ManageInventory)]
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(int productId)
        {
            var result = await _mediator.Send(new GetStockHistoryQuery { ProductId = productId });
            return Ok(result);
        }
    }
}