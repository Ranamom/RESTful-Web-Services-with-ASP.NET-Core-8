using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MiddlewareDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MiddlewareSamplesController : ControllerBase
{
    private readonly Random _random = new();

    private readonly ILogger<WeatherForecastController> _logger;

    public MiddlewareSamplesController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet("rate-limiting")]
    [EnableRateLimiting(policyName: "fixed")]
    public ActionResult RateLimitingDemo()
    {
        return Ok($"Hello {DateTime.Now.Ticks.ToString()}");
    }

    [HttpGet("request-timeout")]
    //[RequestTimeout(5000)]
    [RequestTimeout("ShortTimeoutPolicy")]
    public async Task<ActionResult> RequestTimeoutDemo()
    {
        var delay = _random.Next(1, 10);
        _logger.LogInformation($"Delaying for {delay} seconds");
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(delay), Request.HttpContext.RequestAborted);
        }
        catch
        {
            _logger.LogWarning("The request timed out");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "The request timed out");
        }
        return Ok($"Hello! The task is complete in {delay} seconds");
    }

    [HttpGet("request-timeout-short")]
    [RequestTimeout("ShortTimeoutPolicy")]
    public async Task<ActionResult> RequestTimeoutShortDemo()
    {
        var delay = _random.Next(1, 10);
        _logger.LogInformation($"Delaying for {delay} seconds");
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(delay), Request.HttpContext.RequestAborted);
        }
        catch
        {
            _logger.LogWarning("The request timed out");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "The request timed out");
        }
        return Ok($"Hello! The task is complete in {delay} seconds");
    }

    [HttpGet("request-timeout-long")]
    [RequestTimeout("LongTimeoutPolicy")]
    public async Task<ActionResult> RequestTimeoutLongDemo()
    {
        var delay = _random.Next(1, 10);
        _logger.LogInformation($"Delaying for {delay} seconds");
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(delay), Request.HttpContext.RequestAborted);
        }
        catch
        {
            _logger.LogWarning("The request timed out");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "The request timed out");
        }
        return Ok($"Hello! The task is complete in {delay} seconds");
    }
}
