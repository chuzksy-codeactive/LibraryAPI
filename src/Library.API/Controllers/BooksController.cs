using System;
using System.Collections.Generic;
using AutoMapper;
using Library.API.Entities;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers
{
    [Route ("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        private readonly ILibraryRepository _libraryRepo;

        public BooksController (ILibraryRepository libraryRepo)
        {
            _libraryRepo = libraryRepo;
        }

        [HttpGet ()]
        public IActionResult GetBooksForAuthor (Guid authorId)
        {
            if (!_libraryRepo.AuthorExists (authorId))
            {
                return NotFound ();
            }

            var booksForAuthorFromRepo = _libraryRepo.GetBooksForAuthor (authorId);
            var booksForAuthor = Mapper.Map<IEnumerable<BookDto>> (booksForAuthorFromRepo);

            return Ok (booksForAuthor);
        }

        [HttpGet ("{id}", Name = "GetBookForAuthor")]
        public IActionResult GetBookForAuthor (Guid authorId, Guid id)
        {
            if (!_libraryRepo.AuthorExists (authorId))
            {
                return NotFound ();
            }

            var bookFromAuthorRepo = _libraryRepo.GetBookForAuthor (authorId, id);

            if (bookFromAuthorRepo == null)
            {
                return NotFound ();
            }

            var bookForAuthor = Mapper.Map<BookDto> (bookFromAuthorRepo);

            return Ok (bookForAuthor);
        }

        [HttpPost ()]
        public IActionResult CreateBookForAuthor (Guid authorId, [FromBody] BookForCreationDto book)
        {
            if (book == null)
            {
                return BadRequest ();
            }

            if (!_libraryRepo.AuthorExists (authorId))
            {
                return NotFound ();
            }

            var bookEntity = Mapper.Map<Book> (book);
            _libraryRepo.AddBookForAuthor (authorId, bookEntity);

            if (!_libraryRepo.Save ())
            {
                throw new Exception ($"Creating a book for author {authorId} failed on save");
            }
            var bookToReturn = Mapper.Map<BookDto>(bookEntity);
            return CreatedAtRoute ("GetBookForAuthor", new { authorId = bookToReturn.AuthorId, id = bookToReturn.Id }, bookToReturn);
        }
    }
}