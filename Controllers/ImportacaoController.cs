using LarSaoVicente.Services;
using Microsoft.AspNetCore.Mvc;

namespace LarSaoVicente.Controllers
{
    public class ImportacaoController : Controller
    {
        private readonly IWebHostEnvironment _ambiente;

        public ImportacaoController(IWebHostEnvironment ambiente)
        {
            _ambiente = ambiente;
        }

        public IActionResult Simular()
        {
            var pastaPlanilhas = Path.Combine(_ambiente.ContentRootPath, "Dados", "Planilhas");
            var importador = new ImportadorPlanilhas(pastaPlanilhas);
            var resultado = importador.SimularImportacao();
            return View(resultado);
        }
    }
}