using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Azure.Storage.Queues;
using APIContagem.Models;

namespace APIContagem.Controllers;

[ApiController]
[Route("[controller]")]
public class ContadorController : ControllerBase
{
    private static readonly Contador _CONTADOR = new Contador();
    private readonly ILogger<ContadorController> _logger;
    private readonly IConfiguration _configuration;

    public ContadorController(ILogger<ContadorController> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ResultadoContador), StatusCodes.Status202Accepted)]
    public ActionResult<ResultadoContador> Get()
    {
        int valorAtualContador;

        lock (_CONTADOR)
        {
            _CONTADOR.Incrementar();
            valorAtualContador = _CONTADOR.ValorAtual;
        }

        var resultado = new ResultadoContador()
        {
            ValorAtual = valorAtualContador,
            Producer = _CONTADOR.Local,
            Kernel = _CONTADOR.Kernel,
            Framework = _CONTADOR.Framework,
            Mensagem = _configuration["MensagemVariavel"]
        };
        var queueClient = new QueueClient(_configuration.GetConnectionString("AzureQueueStorage"),
            _configuration["AzureQueueStorage:Queue"]);
        queueClient.SendMessage(JsonSerializer.Serialize(resultado));

        _logger.LogInformation($"Contador - Valor atual: {valorAtualContador}");

        return Accepted(resultado);
    }
}