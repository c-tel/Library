using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Library.API.Controllers
{
    [Route("api/authors/{authorId}/books")]
    public class BooksController: Controller
    {
        private ILibraryRepository libRepo;
        private ILogger<BooksController> logger;
        public BooksController(ILibraryRepository libRepo, ILogger<BooksController> logger)
        {
            this.libRepo = libRepo;
            this.logger = logger;
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

            if (book.Description == book.Title)
            {
                ModelState.AddModelError(nameof(BookForCreationDTO),
                    "The provided description should be different from the title.");
            }

            if (!ModelState.IsValid)
            {
                return new UnprocessableEntityObjectResult(ModelState);
            }

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
            logger.LogInformation($"Deleted book {id} for author {authorId}.");
            return NoContent();
        }


        [HttpPut("{id}")]
        public IActionResult UpdateBook(Guid authorId, Guid id, [FromBody] BookForUpdateDTO book)
        {
            if (book == null)
                return BadRequest();
            if (!libRepo.AuthorExists(authorId))
                return NotFound();

            if (book.Description == book.Title)
            {
                ModelState.AddModelError(nameof(BookForUpdateDTO),
                    "The provided description should be different from the title.");
            }

            if (!ModelState.IsValid)
            {
                return new UnprocessableEntityObjectResult(ModelState);
            }

            var oldBook = libRepo.GetBookForAuthor(authorId, id);
            if (oldBook == null)
            {
                var newBook = Mapper.Map<Book>(book);
                newBook.Id = id;

                libRepo.AddBookForAuthor(authorId, newBook);

                if (!libRepo.Save())
                {
                    throw new Exception($"Updating book {id} for author {authorId} failed on save.");
                }

                var bookToReturn = Mapper.Map<BookDTO>(newBook);

                return CreatedAtRoute("GetBook",
                    new { authorId = authorId, id = bookToReturn.Id },
                    bookToReturn);
            }

            Mapper.Map(book, oldBook);
            libRepo.UpdateBookForAuthor(oldBook);

            if (!libRepo.Save())
            {
                throw new Exception($"Updating book {id} for author {authorId} failed on save.");
            }

            return NoContent();
        }

        [HttpPatch("{id}")]
        public IActionResult PartiallyUpdateBook(Guid authorId, Guid id, [FromBody] JsonPatchDocument<BookForUpdateDTO> doc)
        {
            if (doc == null)
                return BadRequest();

            if (!libRepo.AuthorExists(authorId))
            {
                return NotFound();
            }

            var bookForAuthorFromRepo = libRepo.GetBookForAuthor(authorId, id);
            if (bookForAuthorFromRepo == null)
            {
                var bookDto = new BookForUpdateDTO();
                doc.ApplyTo(bookDto, ModelState);
                TryValidateModel(bookDto);

                if (!ModelState.IsValid)
                {
                    return new UnprocessableEntityObjectResult(ModelState);
                }
                var newBook = Mapper.Map<Book>(bookDto);
                newBook.Id = id;
                libRepo.AddBookForAuthor(authorId, newBook);

                if (!libRepo.Save())
                {
                    throw new Exception($"Updating book {id} for author {authorId} failed on save.");
                }

                var bookToReturn = Mapper.Map<BookDTO>(newBook);

                return CreatedAtRoute("GetBook",
                    new { authorId = authorId, id = bookToReturn.Id },
                    bookToReturn);
            }

            var bookToUpdate = Mapper.Map<BookForUpdateDTO>(bookForAuthorFromRepo);

            doc.ApplyTo(bookToUpdate, ModelState);

            TryValidateModel(bookToUpdate);
            if (!ModelState.IsValid)
            {
                return new UnprocessableEntityObjectResult(ModelState);
            }
            Mapper.Map(bookToUpdate, bookForAuthorFromRepo);
            libRepo.UpdateBookForAuthor(bookForAuthorFromRepo);
            if (!libRepo.Save())
            {
                throw new Exception($"Partially updating book {id} for author {authorId} failed on save.");
            }

            return NoContent();
        }
    }
}
