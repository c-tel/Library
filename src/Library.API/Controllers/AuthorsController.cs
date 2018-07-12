using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers
{
    [Route("api/authors")]
    public class AuthorsController: Controller
    {
        private ILibraryRepository libRepo;
        public AuthorsController(ILibraryRepository libRepo)
        {
            this.libRepo = libRepo;
        }
           
        public IActionResult GetAuthors()
        {
//          throw new Exception();
            var res = AutoMapper.Mapper.Map<IEnumerable<AuthorDTO>>(libRepo.GetAuthors());
            return Ok(res);
        }

        [HttpGet("{id}", Name = "GetAuthor")]
        public IActionResult GetAuthor(Guid id)
        {
            var author = libRepo.GetAuthor(id);
            return author != null ? (IActionResult) Ok(AutoMapper.Mapper.Map<AuthorDTO>(author)) : NotFound();
        }

        [HttpPost]
        public IActionResult CreateAuthor([FromBody] AuthorForCreationDTO author)
        {
            if (author == null)
                return BadRequest();

            var authorEntity = AutoMapper.Mapper.Map<Author>(author);
            libRepo.AddAuthor(authorEntity);

            if (!libRepo.Save())
                throw new Exception("Creation of author failed on save");

            var authorDto = AutoMapper.Mapper.Map<AuthorDTO>(authorEntity);
            return CreatedAtRoute("GetAuthor", new {id = authorDto.Id}, authorDto);

        }

        [HttpDelete("{id}")]
        public IActionResult DeleteAuthor(Guid id)
        {
            var authorFromRepo = libRepo.GetAuthor(id);
            if (authorFromRepo == null)
            {
                return NotFound();
            }

            libRepo.DeleteAuthor(authorFromRepo);

            if (!libRepo.Save())
            {
                throw new Exception($"Deleting author {id} failed on save.");
            }

            return NoContent();
        }
    }
}
