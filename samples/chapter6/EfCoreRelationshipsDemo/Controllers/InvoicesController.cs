using EfCoreRelationshipsDemo.Data;
using EfCoreRelationshipsDemo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EfCoreRelationshipsDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoicesController : ControllerBase
    {
        private readonly SampleDbContext _context;

        public InvoicesController(SampleDbContext context)
        {
            _context = context;
        }

        // GET: api/Invoices
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Invoice>>> GetInvoices(int page = 1, int pageSize = 10,
            InvoiceStatus? status = null)
        {
            if (_context.Invoices == null)
            {
                return NotFound();
            }

            return await _context.Invoices
                .Include(x => x.InvoiceItems)
                .Where(x => status == null || x.Status == status)
                .OrderByDescending(x => x.InvoiceDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                //.AsSplitQuery()
                .ToListAsync();
        }

        // GET: api/Invoices/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Invoice>> GetInvoice(Guid id)
        {
            if (_context.Invoices == null)
            {
                return NotFound();
            }

            //var invoice = await _context.Invoices.FindAsync(id);
            var invoice = await _context.Invoices.Include(x => x.InvoiceItems).SingleOrDefaultAsync(x => x.Id == id);

            if (invoice == null)
            {
                return NotFound();
            }

            return invoice;
        }

        // PUT: api/Invoices/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutInvoice(Guid id, Invoice invoice)
        {
            if (id != invoice.Id)
            {
                return BadRequest();
            }

            _context.Entry(invoice).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InvoiceExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Invoices
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Invoice>> PostInvoice(Invoice invoice)
        {
            if (_context.Invoices == null)
            {
                return Problem("Entity set 'InvoiceDbContext.Invoices'  is null.");
            }

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetInvoice", new { id = invoice.Id }, invoice);
        }

        // DELETE: api/Invoices/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInvoice(Guid id)
        {
            if (_context.Invoices == null)
            {
                return NotFound();
            }

            //await _context.Invoices.Where(x => x.Id == id).ExecuteDeleteAsync();
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }
            _context.Invoices.Remove(invoice);
            return NoContent();
        }

        private bool InvoiceExists(Guid id)
        {
            return (_context.Invoices?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
