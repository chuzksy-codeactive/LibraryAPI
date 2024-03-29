using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers
{
    [Route ("api/authorcollections")]
    public class AuthorCollectionsController : Controller
    {
        private readonly ILibraryRepository _libraryRepo;

        public AuthorCollectionsController (ILibraryRepository libraryRepo)
        {
            _libraryRepo = libraryRepo;
        }

        [HttpPost ()]
        public IActionResult CreateAuthorCollection ([FromBody] IEnumerable<AuthorForCreationDto> authorCollection)
        {
            if (authorCollection == null)
            {
                return BadRequest ();
            }

            var authorEntities = Mapper.Map<IEnumerable<Author>> (authorCollection);

            foreach (var author in authorEntities)
            {
                _libraryRepo.AddAuthor (author);
            }

            if (!_libraryRepo.Save ())
            {
                throw new Exception ("Creating an author collection failed on save");
            }

            var authorCollectionToReturn = Mapper.Map<IEnumerable<AuthorDto>> (authorEntities);
            var idsAsString = string.Join (",",
                authorCollectionToReturn.Select (a => a.Id));

            return CreatedAtRoute ("GetAuthorCollection",
                new
                {
                    ids = idsAsString
                },
                authorCollectionToReturn);
        }

        [HttpGet ("{ids}", Name = "GetAuthorCollection")]
        public IActionResult GetAuthorCollection ([ModelBinder (BinderType = typeof (ArrayModelBinder))] IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                return BadRequest ();
            }

            var authorEntities = _libraryRepo.GetAuthors (ids);

            if (ids.Count () == authorEntities.Count ())
            {
                return NotFound ();
            }

            var authorsToReturn = Mapper.Map<AuthorDto> (authorEntities);
            return Ok (authorsToReturn);
        }
    }
}