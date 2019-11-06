using System;
using System.Collections.Generic;

using AutoMapper;

using Library.API.Entities;
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
        private readonly IUrlHelper _urlHelper;
        private readonly IPropertyMappingService _propertyMappingService;
        private readonly ITypeHelperService _typeHelperService;

        public AuthorsController (
            ILibraryRepository libraryRepo,
            IUrlHelper urlHelper,
            IPropertyMappingService propertyMappingService,
            ITypeHelperService typeHelperService)
        {
            _libraryRepo = libraryRepo;
            _urlHelper = urlHelper;
            _propertyMappingService = propertyMappingService;
            _typeHelperService = typeHelperService;
        }

        [HttpGet (Name = "GetAuthors")]
        public IActionResult GetAuthors (AuthorsResourceParameters authorsResourceParameters)
        {
            if (!_propertyMappingService.ValidMappingExistsFor<AuthorDto, Author> (authorsResourceParameters.OrderBy))
            {
                return BadRequest ();
            }

            if (!_typeHelperService.TypeHasProperties<AuthorDto> (authorsResourceParameters.Fields))
            {
                return BadRequest ();
            }
            var authorsFromRepo = _libraryRepo.GetAuthors (authorsResourceParameters);

            var previousPageLink = authorsFromRepo.HasPrevious ?
                CreateAuthorsResourceUri (authorsResourceParameters,
                    ResourceUriType.PreviousPage) : null;

            var nextPageLink = authorsFromRepo.HasNext ?
                CreateAuthorsResourceUri (authorsResourceParameters,
                    ResourceUriType.NextPage) : null;

            var paginationMetadata = new
            {
                totalCount = authorsFromRepo.TotalCount,
                pageSize = authorsFromRepo.PageSize,
                currentPage = authorsFromRepo.CurrentPage,
                totalPages = authorsFromRepo.TotalPages,
                previousPageLink = previousPageLink,
                nextPageLink = nextPageLink
            };

            Response.Headers.Add ("X-Pagination", Newtonsoft.Json.JsonConvert.SerializeObject (paginationMetadata));

            var authors = Mapper.Map<IEnumerable<AuthorDto>> (authorsFromRepo);

            return Ok (authors.ShapeData (authorsResourceParameters.Fields));
        }

        private string CreateAuthorsResourceUri (AuthorsResourceParameters authorsResourceParameters, ResourceUriType type)
        {
            switch (type)
            {
            case ResourceUriType.PreviousPage:
                return _urlHelper.Link ("GetAuthors",
                    new
                    {
                        fields = authorsResourceParameters.Fields,
                            orderBy = authorsResourceParameters.OrderBy,
                            searchQuery = authorsResourceParameters.SearchQuery,
                            genre = authorsResourceParameters.Genre,
                            pageNumber = authorsResourceParameters.PageNumber - 1,
                            pageSize = authorsResourceParameters.PageSize
                    });
            case ResourceUriType.NextPage:
                return _urlHelper.Link ("GetAuthors",
                    new
                    {
                        fields = authorsResourceParameters.Fields,
                            orderBy = authorsResourceParameters.OrderBy,
                            searchQuery = authorsResourceParameters.SearchQuery,
                            genre = authorsResourceParameters.Genre,
                            pageNumber = authorsResourceParameters.PageNumber + 1,
                            pageSize = authorsResourceParameters.PageSize
                    });

            default:
                return _urlHelper.Link ("GetAuthors",
                    new
                    {
                        fields = authorsResourceParameters.Fields,
                            orderBy = authorsResourceParameters.OrderBy,
                            searchQuery = authorsResourceParameters.SearchQuery,
                            genre = authorsResourceParameters.Genre,
                            pageNumber = authorsResourceParameters.PageNumber,
                            pageSize = authorsResourceParameters.PageSize
                    });
            }
        }

        [HttpGet ("{id}", Name = "GetAuthor")]
        public IActionResult GetAuthor (Guid id, [FromQuery] string fields)
        {
            if (!_typeHelperService.TypeHasProperties<AuthorDto> (fields))
            {
                return BadRequest ();
            }
            var authorFromRepo = _libraryRepo.GetAuthor (id);

            if (authorFromRepo == null) return NotFound ();

            var author = Mapper.Map<AuthorDto> (authorFromRepo);
            return Ok (author.ShapeData (fields));
        }

        [HttpPost ()]
        public IActionResult CreateAuthor ([FromBody] AuthorForCreationDto author)
        {
            if (author == null)
            {
                return BadRequest ();
            }

            var authorEntity = Mapper.Map<Author> (author);
            _libraryRepo.AddAuthor (authorEntity);

            if (!_libraryRepo.Save ())
            {
                throw new Exception ("Creating an Author failed on save");
            }

            var authorToReturn = Mapper.Map<AuthorDto> (authorEntity);
            return CreatedAtRoute ("GetAuthor", new
            {
                id = authorToReturn.Id
            }, authorToReturn);
        }

        [HttpDelete ("{id}")]
        public IActionResult DeleteAuthor (Guid id)
        {
            var authorFromRepo = _libraryRepo.GetAuthor (id);

            if (authorFromRepo == null)
            {
                return NotFound ();
            }

            _libraryRepo.DeleteAuthor (authorFromRepo);

            if (!_libraryRepo.Save ())
            {
                throw new Exception ($"Deleting author {id} failed on save");
            }

            return NoContent ();
        }
    }
}
