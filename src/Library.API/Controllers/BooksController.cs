using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Library.API.Entities;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController: Controller
    {
        private ILibraryRepository libRepo;
        public BooksController(ILibraryRepository libRepo)
        {
            this.libRepo = libRepo;
        }
        [HttpGet]
        public IActionResult GetBooksForAuthor(Guid authorId)
        {
            if (!libRepo.AuthorExists(authorId))
                return NotFound();
            var bookEntities = libRepo.GetBooksForAuthor(authorId);
            var bookDtos = AutoMapper.Mapper.Map<IEnumerable<BookDTO>>(bookEntities);
            return Ok(bookDtos);
        }
        [HttpGet("{id}", Name = "GetBook")]
        public IActionResult GetBookForAuthor(Guid authorId, Guid id)
        {
            if (!libRepo.AuthorExists(authorId))
                return NotFound();

            var bookEntity = libRepo.GetBookForAuthor(authorId, id);
            if(bookEntity==null)
                return NotFound();

            var bookDto = AutoMapper.Mapper.Map<BookDTO>(bookEntity);
            return Ok(bookDto);
        }

        [HttpPost]
        public IActionResult CreateBookForAuthor(Guid authorId, [FromBody] BookForCreationDTO book)
        {
            if (book == null)
                return BadRequest();

            if (!libRepo.AuthorExists(authorId))
                return NotFound();
            var bookEntity = Mapper.Map<Book>(book);

            libRepo.AddBookForAuthor(authorId, bookEntity);

            if (!libRepo.Save())
               throw new Exception($"Creating a book for author {authorId} failed on save.");
            

            var bookToReturn = Mapper.Map<BookDTO>(bookEntity);

            return CreatedAtRoute("GetBook",
                new { authorId = authorId, id = bookToReturn.Id },
                bookToReturn);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteBookForAuthor(Guid authorId, Guid id)
        {
            if (!libRepo.AuthorExists(authorId))
            {
                return NotFound();
            }

            var bookForAuthorFromRepo = libRepo.GetBookForAuthor(authorId, id);
            if (bookForAuthorFromRepo == null)
            {
                return NotFound();
            }

            libRepo.DeleteBook(bookForAuthorFromRepo);

            if (!libRepo.Save())
            {
                throw new Exception($"Deleting book {id} for author {authorId} failed on save.");
            }

            return NoContent();
        }
    }
}
