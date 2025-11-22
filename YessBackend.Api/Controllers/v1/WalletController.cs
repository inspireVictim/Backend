using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YessBackend.Application.DTOs.Wallet;
using YessBackend.Application.Services;
using AutoMapper;
using System.Security.Claims;

namespace YessBackend.Api.Controllers.v1;

/// <summary>
/// Контроллер кошелька
/// Соответствует /api/v1/wallet из Python API
/// </summary>
[ApiController]
[Route("api/v1/wallet")]
[Tags("Wallet")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly IMapper _mapper;
    private readonly ILogger<WalletController> _logger;

    public WalletController(
        IWalletService walletService,
        IMapper mapper,
        ILogger<WalletController> logger)
    {
        _walletService = walletService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Получить информацию о кошельке
    /// GET /api/v1/wallet
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(WalletResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WalletResponseDto>> GetWallet()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "Неверный токен" });
            }

            var wallet = await _walletService.GetWalletByUserIdAsync(userId.Value);
            if (wallet == null)
            {
                return NotFound(new { error = "Кошелек не найден" });
            }

            var response = _mapper.Map<WalletResponseDto>(wallet);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения кошелька");
            return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Получить баланс
    /// GET /api/v1/wallet/balance
    /// </summary>
    [HttpGet("balance")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetBalance()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "Неверный токен" });
            }

            var balance = await _walletService.GetBalanceAsync(userId.Value);
            var yescoinBalance = await _walletService.GetYescoinBalanceAsync(userId.Value);

            return Ok(new
            {
                balance = balance,
                yescoin_balance = yescoinBalance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения баланса");
            return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Получить историю транзакций
    /// GET /api/v1/wallet/transactions
    /// </summary>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(List<TransactionResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TransactionResponseDto>>> GetTransactions(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { error = "Неверный токен" });
            }

            var transactions = await _walletService.GetUserTransactionsAsync(userId.Value, limit, offset);
            var response = transactions.Select(t => _mapper.Map<TransactionResponseDto>(t)).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения транзакций");
            return StatusCode(500, new { error = "Внутренняя ошибка сервера" });
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("user_id")?.Value 
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        
        return null;
    }
}
