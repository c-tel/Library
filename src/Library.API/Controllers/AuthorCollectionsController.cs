using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.API
{
    [Route("api/authorcollections")]
    public class AuthorCollectionsController: Controller
    {
        private ILibraryRepository libRepo;

        public AuthorCollectionsController(ILibraryRepository libRepo)
        {
            this.libRepo = libRepo;
        }


        [HttpPost]
        public IActionResult CreateAuthorCollection([FromBody] IEnumerable<AuthorForCreationDTO> authorCollection)
        {
            if (authorCollection == null)
                return BadRequest();
            var authorEntities = AutoMapper.Mapper.Map<IEnumerable<Author>>(authorCollection);

            foreach (var author in authorEntities)
            {
                libRepo.AddAuthor(author);
            }

            if (!libRepo.Save())
                throw new Exception("Creation of author failed on save");

            var authorsDtos = AutoMapper.Mapper.Map<IEnumerable<AuthorDTO>>(authorEntities);

            var keys = string.Join(", ", authorsDtos.Select(_ => _.Id));

            return CreatedAtRoute("GetAuthorCollection", new {ids = keys}, authorsDtos);
        }

        [HttpGet("({ids})", Name = "GetAuthorCollection")]
        public IActionResult GetAuthorCollection(
            [ModelBinder(BinderType = typeof(ArrayModelBinder))] IEnumerable<Guid> ids)
        {
            if (ids == null)
                return BadRequest();

            var authorEntities = libRepo.GetAuthors(ids);

            if (ids.Count() != authorEntities.Count())
                return NotFound();

            var authorsToReturn = AutoMapper.Mapper.Map<IEnumerable<AuthorDTO>>(authorEntities);
            return Ok(authorsToReturn);
        }
    }
}
