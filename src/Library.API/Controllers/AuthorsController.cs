using System;
using System.Collections.Generic;
using AutoMapper;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers
{
    [Route ("api/[controller]")]
    public class AuthorsController : Controller
    {
        private readonly ILibraryRepository _libraryRepo;

        public AuthorsController (ILibraryRepository libraryRepo)
        {
            _libraryRepo = libraryRepo;
        }

        [HttpGet ()]
        public IActionResult GetAuthors ()
        {
            var authorsFromRepo = _libraryRepo.GetAuthors ();
            var authors = Mapper.Map<IEnumerable<AuthorDto>> (authorsFromRepo);

            return Ok (authors);
        }

        [HttpGet ("{id}")]
        public IActionResult GetAuthor (Guid id)
        {
            var authorFromRepo = _libraryRepo.GetAuthor (id);

            if (authorFromRepo == null) return NotFound ();

            var author = Mapper.Map<AuthorDto> (authorFromRepo);
            return Ok (author);
        }
    }
}