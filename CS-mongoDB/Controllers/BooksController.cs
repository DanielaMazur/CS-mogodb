using CS_mongoDB.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CS_mongoDB.Controllers
{

     [ApiController]
     [Route("api/[controller]")]
     public class BooksController : ControllerBase
     {
          private readonly BooksService _booksService;

          public BooksController(BooksService booksService) =>
              _booksService = booksService;

          [HttpGet]
          public async Task<List<Book>> Get() => await _booksService.GetAsync();

          [HttpGet("decrypted")]
          public async Task<List<Book>> GetDecrypt() => await _booksService.GetDecryptedAsync();

          [HttpGet("{id:length(24)}")]
          public async Task<ActionResult<Book>> Get(string id)
          {
               var book = await _booksService.GetAsync(id);

               if (book is null)
               {
                    return NotFound();
               }


               return book;
          }

          [HttpPost]
          public async Task<IActionResult> Post(Book newBook)
          {
               await _booksService.CreateAsync(newBook);

               return CreatedAtAction(nameof(Get), new { id = newBook.Id }, newBook);
          }


          [HttpDelete("{id:length(24)}")]
          public async Task<IActionResult> Delete(string id)
          {
               var book = await _booksService.GetAsync(id);

               if (book is null)
               {
                    return NotFound();
               }

               await _booksService.RemoveAsync(book.Id);

               return NoContent();
          }
     }
}
