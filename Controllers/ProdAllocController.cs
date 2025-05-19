using Microsoft.AspNetCore.Mvc;
using WisVestAPI.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;
using WisVestAPI.Models.DTOs;
using WisVestAPI.Constants;
using System.Linq;
using System;

namespace WisVestAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductAllocationController : ControllerBase
    {
        private readonly ProductAllocationService _productAllocationService;
        private readonly ILogger<ProductAllocationController> _logger;

        public ProductAllocationController(ProductAllocationService productAllocationService, ILogger<ProductAllocationController> logger)
        {
            _productAllocationService = productAllocationService;
            _logger = logger;
        }

        [HttpGet("get-product-allocations")]
        public async Task<IActionResult> GetProductAllocations()
        {
            try
            {
                var (productAllocations, totalInvestment) = await _productAllocationService.GetProductAllocationsAsync();
                
                return Ok(new
                {
                    ProductAllocations = productAllocations,
                    TotalInvestment = totalInvestment
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ResponseMessages.ProductAllocationsFetchError);
                return StatusCode(500, new { message = ResponseMessages.ProductAllocationsFetchError, error = ex.Message });
            }
        }

        // [HttpPost("calculate-product-allocations")]
        // public async Task<IActionResult> CalculateProductAllocations(
        //     [FromBody] AllocationResultDTO allocationResult,
        //     [FromQuery] double targetAmount,
        //     [FromQuery] int investmentHorizon)
        // {
        //     try
        //     {
        //         _logger.LogInformation("Sub-allocation Data Received: {Data}", JsonSerializer.Serialize(allocationResult));
        //         _logger.LogInformation("Target Amount: {Amount}", targetAmount);
        //         _logger.LogInformation("Investment Horizon: {Horizon}", investmentHorizon);

        //         if (allocationResult == null || allocationResult.Assets == null)
        //         {
        //             return BadRequest(ResponseMessages.AllocationResultNull);
        //         }

        //         if (targetAmount <= 0)
        //         {
        //             return BadRequest(ResponseMessages.TargetAmountInvalid);
        //         }

        //         if (investmentHorizon <= 0)
        //         {
        //             return BadRequest(ResponseMessages.HorizonInvalid);
        //         }

        //         var subAllocationResult = allocationResult.Assets.ToDictionary(
        //             asset => asset.Key,
        //             asset => asset.Value.SubAssets
        //         );

        //         var result = await _productAllocationService.CalculateProductAllocations(
        //             subAllocationResult,
        //             targetAmount,
        //             investmentHorizon
        //         );

        //         if (result == null || result.Count == 0)
        //         {
        //             return NotFound(ResponseMessages.NoProductAllocationsFound);
        //         }

        //         return Ok(result);
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, ResponseMessages.ProductAllocationCalculationError);
        //         return StatusCode(500, new { message = ResponseMessages.ProductAllocationCalculationError, error = ex.Message });
        //     }
        // }

                [HttpPost("calculate-product-allocations")]
        public async Task<IActionResult> CalculateProductAllocations(
            [FromBody] AllocationResultDTO allocationResult,
            [FromQuery] double targetAmount,
            [FromQuery] int investmentHorizon)
        {
            try
            {
                _logger.LogInformation("Sub-allocation Data Received: {Data}", JsonSerializer.Serialize(allocationResult));
                _logger.LogInformation("Target Amount: {Amount}", targetAmount);
                _logger.LogInformation("Investment Horizon: {Horizon}", investmentHorizon);
        
                if (allocationResult == null || allocationResult.Assets == null)
                {
                    return BadRequest(new { message = "Allocation result cannot be null." });
                }
        
                foreach (var asset in allocationResult.Assets)
                {
                    foreach (var subAsset in asset.Value.SubAssets)
                    {
                        if (subAsset.Value < 0 || subAsset.Value > 100)
                        {
                            _logger.LogError("Invalid PercentageSplit detected: {Value} for sub-asset {SubAsset} in asset {Asset}",
                                subAsset.Value, subAsset.Key, asset.Key);
                            return BadRequest(new { message = "Percentage split must be between 0 and 100." });
                        }
                    }
                }
        
                var subAllocationResult = allocationResult.Assets.ToDictionary(
                    asset => asset.Key,
                    asset => asset.Value.SubAssets
                );
        
                var result = await _productAllocationService.CalculateProductAllocations(
                    subAllocationResult,
                    targetAmount,
                    investmentHorizon
                );
        
                if (result == null || result.Count == 0)
                {
                    return NotFound(new { message = "No product allocations found." });
                }
        
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while calculating product allocations.");
                return StatusCode(500, new { message = "Error occurred while calculating product allocations.", error = ex.Message });
            }
        }
    }
}
