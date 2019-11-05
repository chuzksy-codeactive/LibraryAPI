using System;
using System.Collections.Generic;
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
    [Route ("api/authors/{authorId}/books")]
    public class BooksController : Controller
    {
        private readonly ILibraryRepository _libraryRepo;
        private readonly ILogger _logger;

        public BooksController (ILibraryRepository libraryRepo, ILogger logger)
        {
            _libraryRepo = libraryRepo;
            _logger = logger;
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

            if (book.Description == book.Title)
            {
                ModelState.AddModelError (nameof (BookForCreationDto),
                    "The provided description should be different from the title.");
            }

            if (!_libraryRepo.AuthorExists (authorId))
            {
                return NotFound ();
            }

            if (!ModelState.IsValid)
            {
                // return 422
                return new UnprocessableEntityObjectResult (ModelState);
            }

            var bookEntity = Mapper.Map<Book> (book);
            _libraryRepo.AddBookForAuthor (authorId, bookEntity);

            if (!_libraryRepo.Save ())
            {
                throw new Exception ($"Creating a book for author {authorId} failed on save");
            }
            var bookToReturn = Mapper.Map<BookDto> (bookEntity);
            return CreatedAtRoute ("GetBookForAuthor", new
            {
                authorId = bookToReturn.AuthorId, id = bookToReturn.Id
            }, bookToReturn);
        }

        [HttpDelete ("{id}")]
        public IActionResult DeleteBookForAuthor (Guid authorId, Guid id)
        {
            if (!_libraryRepo.AuthorExists (authorId))
            {
                return NotFound ();
            }

            var bookForAuthorFromRepo = _libraryRepo.GetBookForAuthor (authorId, id);

            if (bookForAuthorFromRepo == null)
            {
                return NotFound ();
            }

            _libraryRepo.DeleteBook (bookForAuthorFromRepo);

            if (!_libraryRepo.Save ())
            {
                throw new Exception ($"Deleting book {id} for author {authorId} failed on save");
            }

            _logger.LogInformation (100, $"Bood {id} for author {authorId} was deleted");

            return NoContent ();
        }

        [HttpPut ("{id}")]
        public IActionResult UpdateBookForAuthor (Guid authorId, Guid id, [FromBody] BookForUpdateDto book)
        {
            if (book == null)
            {
                return BadRequest ();
            }

            if (book.Description == book.Title)
            {
                ModelState.AddModelError (nameof (BookForUpdateDto),
                    "The provided description should be different from the title.");
            }

            if (!ModelState.IsValid)
            {
                return new UnprocessableEntityObjectResult (ModelState);
            }

            if (!_libraryRepo.AuthorExists (authorId))
            {
                return NotFound ();
            }

            var bookForAuthorFromRepo = _libraryRepo.GetBookForAuthor (authorId, id);

            if (bookForAuthorFromRepo == null)
            {
                var bookToAdd = Mapper.Map<Book> (book);
                bookToAdd.Id = id;

                _libraryRepo.AddBookForAuthor (authorId, bookToAdd);

                if (!_libraryRepo.Save ())
                {
                    throw new Exception ($"Creating book {bookToAdd.Id} for author {authorId} failed on save");
                }

                var bookToReturn = Mapper.Map<BookDto> (bookToAdd);
                return CreatedAtRoute ("GetBookForAuthor", new
                {
                    authorId = authorId, id = bookToReturn.Id
                }, bookToReturn);
            }

            Mapper.Map (book, bookForAuthorFromRepo);

            _libraryRepo.UpdateBookForAuthor (bookForAuthorFromRepo);

            if (!_libraryRepo.Save ())
            {
                throw new Exception ($"Updating book {id} for author {authorId} failed on save");
            }

            return NoContent ();
        }

        [HttpPatch ("{id}")]
        public IActionResult PartiallyUpdateBookForAuthor (Guid authorId, Guid id, [FromBody] JsonPatchDocument<BookForUpdateDto> pathDoc)
        {
            if (pathDoc == null)
            {
                return BadRequest ();
            }

            if (!_libraryRepo.AuthorExists (authorId))
            {
                return NotFound ();
            }

            var bookForAuthorFromRepo = _libraryRepo.GetBookForAuthor (authorId, id);

            if (bookForAuthorFromRepo == null)
            {
                var bookDto = new BookForUpdateDto ();
                pathDoc.ApplyTo (bookDto, ModelState);

                if (bookDto.Description == bookDto.Title)
                {
                    ModelState.AddModelError (nameof (BookForUpdateDto),
                        "The provided description should be different from the title.");
                }

                TryValidateModel (bookDto);

                if (!ModelState.IsValid)
                {
                    return new UnprocessableEntityObjectResult (ModelState);
                }

                var bookToAdd = Mapper.Map<Book> (bookDto);
                bookToAdd.Id = id;

                _libraryRepo.AddBookForAuthor (authorId, bookToAdd);

                if (!_libraryRepo.Save ())
                {
                    throw new Exception ($"Upserting book {id} for author {authorId} failed on save");
                }

                var bookToReturn = Mapper.Map<BookDto> (bookToAdd);

                return CreatedAtRoute ("GetBookForAuthor", new
                {
                    authorId = authorId, id = bookToReturn.Id
                }, bookToReturn);
            }

            var bookToPatch = Mapper.Map<BookForUpdateDto> (bookForAuthorFromRepo);
            pathDoc.ApplyTo (bookToPatch, ModelState);

            if (bookToPatch.Description == bookToPatch.Title)
            {
                ModelState.AddModelError (nameof (BookForUpdateDto),
                    "The provided description should be different from the title.");
            }

            TryValidateModel (bookToPatch);

            if (!ModelState.IsValid)
            {
                return new UnprocessableEntityObjectResult (ModelState);
            }

            // add validation
            Mapper.Map (bookToPatch, bookForAuthorFromRepo);
            _libraryRepo.UpdateBookForAuthor (bookForAuthorFromRepo);

            if (!_libraryRepo.Save ())
            {
                throw new Exception ($"Pathing book {id} for author {authorId} failed on save");
            }

            return NoContent ();
        }
    }
}