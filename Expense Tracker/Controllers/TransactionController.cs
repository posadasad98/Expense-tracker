using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Expense_Tracker.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace Expense_Tracker.Controllers
{
    public class TransactionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Transaction
        //public async Task<IActionResult> Index()
        //{
        //    var applicationDbContext = _context.Transactions.Include(t => t.Category);
        //    return View(await applicationDbContext.ToListAsync());

        //
        public async Task<IActionResult> Index(string? categoria, DateTime? date)
        {
            IQueryable<Transaction> query = _context.Transactions.Include(t => t.Category);
            if (!string.IsNullOrEmpty(categoria))
            {
                query = query.Where(t => t.Category.Title == categoria);
            }
            if (date != null)
            {
                query = query.Where(t => t.Date == date);
            }
            var transactions = await query.ToListAsync();
            return View(transactions);

        }

        public IActionResult AddOrEdit(int id = 0)
        {
            PopulateCategories();
            if (id == 0)
                return View(new Transaction());
            else
                return View(_context.Transactions.Find(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit([Bind("TransactionId,CategoryId,Amount,Note,Date")] Transaction transaction)
        {
            if (ModelState.IsValid)
            {
                if (transaction.TransactionId == 0)
                    _context.Add(transaction);
                else
                    _context.Update(transaction);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            PopulateCategories();
            return View(transaction);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Transactions == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Transactions'  is null.");
            }
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction != null)
            {
                _context.Transactions.Remove(transaction);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        [NonAction]
        public void PopulateCategories()
        {
            var CategoryCollection = _context.Categories.ToList();
            Category DefaultCategory = new Category() { CategoryId = 0, Title = "Choose a Category" };
            CategoryCollection.Insert(0, DefaultCategory);
            ViewBag.Categories = CategoryCollection;
        }

        public ActionResult GenerarPdf(string? tipo, int totalIngresos, int totalEgresos)
        {
            IQueryable<Transaction> transactions;
            if (tipo == "Income")
            {
                transactions = _context.Transactions.Include(t => t.Category).Where(t => t.Category.Type == "Income");
                
            }
            else if (tipo == "Expense")
            {
                transactions = _context.Transactions.Include(t => t.Category).Where(t => t.Category.Type == "Expense");
            }
            else if (tipo == null)
            {
                transactions = _context.Transactions.Include(t => t.Category);
            }
            else
            {
                return BadRequest("Tipo de transaccion no valido ");
            }

            string rutaTempPdf = Path.GetTempFileName() + ".pdf";

            using (PdfDocument pdfDocument = new PdfDocument(new PdfWriter(rutaTempPdf)))
            {
               
                using (Document document = new Document(pdfDocument))
                {
                    document.Add(new Paragraph("Transacciones Personales de Alexis Posadas"));

                    iText.Layout.Element.Table table = new iText.Layout.Element.Table(3);
                    table.SetWidth(UnitValue.CreatePercentValue(100));

                    table.AddHeaderCell("Categoria");
                    table.AddHeaderCell("Monto");
                    table.AddHeaderCell("Fecha");

                    foreach (var transaction in transactions.ToList())
                    {
                        table.AddCell(transaction.Category.Title);
                        table.AddCell(transaction.Amount.ToString());
                        table.AddCell(transaction.Date.ToString());

                        if (transaction.Category.Type == "Income")
                        {
                            
                             totalIngresos = totalIngresos + transaction.Amount;
                        }
                        else if(transaction.Category.Type == "Expense")
                        {
                            
                            totalEgresos = totalEgresos + transaction.Amount;
                        }

                        
                    }
                    if (tipo == "Balance")
                    {
                        var balance = totalIngresos - totalEgresos;
                        document.Add(new Paragraph($"Total de Ingresos: {totalIngresos}"));
                        document.Add(new Paragraph($"Total de Egresos: -{totalEgresos}"));
                        document.Add(new Paragraph($"Balance Total: {balance}"));
                    }
                    else
                    {
                        var total = tipo == "Income" ? totalIngresos : -totalEgresos;
                        document.Add(new Paragraph($"Total de {tipo}: {total}"));
                    }
                        document.Add(table);
                }
            }

            // Leer el archivo PDF como un arreglo de bytes
            byte[] fileBytes = System.IO.File.ReadAllBytes(rutaTempPdf);

            // Eliminar el archivo temporal
            System.IO.File.Delete(rutaTempPdf);

            // Descargar el archivo PDF
            return new FileStreamResult(new MemoryStream(fileBytes), "application/pdf")
            {
                FileDownloadName = "Estado.pdf"
            };
        }
    }
}
