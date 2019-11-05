﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Library.API
{
    public class Startup
    {
        public static IConfiguration Configuration;

        public Startup (IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices (IServiceCollection services)
        {
            services.AddMvc (setupAction =>
            {
                setupAction.ReturnHttpNotAcceptable = true;
                setupAction.OutputFormatters.Add (new XmlDataContractSerializerOutputFormatter ());
                setupAction.InputFormatters.Add (new XmlDataContractSerializerInputFormatter ());
            });

            // register the DbContext on the container, getting the connection string from
            // appSettings (note: use this during development; in a production environment,
            // it's better to store the connection string in an environment variable)
            var connectionString = Configuration["connectionStrings:libraryDBConnectionString"];
            services.AddDbContext<LibraryContext> (o => o.UseSqlServer ("Data Source=.; Initial Catalog=LibraryDB; User Id=sa; Password=lillyr055y; Persist Security Info=True"));

            // register the repository
            services.AddScoped<ILibraryRepository, LibraryRepository> ();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure (IApplicationBuilder app, IHostingEnvironment env,
            ILoggerFactory loggerFactory, LibraryContext libraryContext)
        {
            if (env.IsDevelopment ())
            {
                app.UseDeveloperExceptionPage ();
            }
            else
            {
                app.UseExceptionHandler ();
            }

            libraryContext.EnsureSeedDataForContext ();

            AutoMapper.Mapper.Initialize (x =>
            {
                x.CreateMap<Author, AuthorDto> ()
                    .ForMember (dest => dest.Name, opt => opt.MapFrom (src =>
                        $"{src.FirstName} {src.LastName}"
                    ))
                    .ForMember (dest => dest.Age, opt => opt.MapFrom (src =>
                        src.DateOfBirth.GetCurrentAge ()
                    ));
                x.CreateMap<Book, BookDto> ();
                x.CreateMap<Book, BookForUpdateDto>();

                x.CreateMap<AuthorForCreationDto, Author> ();
                x.CreateMap<BookForCreationDto, Book> ();
                x.CreateMap<BookForUpdateDto, Book> ();
            });

            app.UseMvc ();
        }
    }
}