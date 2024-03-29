﻿using AspNetCoreRateLimit;

using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json.Serialization;

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
                })
                .AddJsonOptions (options =>
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver ();
                });

            services.AddSwaggerGen (c =>
            {
                c.SwaggerDoc ("v1", new OpenApiInfo
                {
                    Title = "Library API",
                        Version = "v1",
                        Contact = new OpenApiContact () { Name = "Onuchukwu Chika", Email = "chuzksy@yahoo.com" }
                });
            });

            // register the DbContext on the container, getting the connection string from
            // appSettings (note: use this during development; in a production environment,
            // it's better to store the connection string in an environment variable)
            var connectionString = Configuration["connectionStrings:libraryDBConnectionString"];
            services.AddDbContext<LibraryContext> (o => o.UseSqlServer (connectionString));

            // register the repository
            services.AddScoped<ILibraryRepository, LibraryRepository> ();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor> ();
            services.AddScoped<IUrlHelper> (implementationFactory =>
            {
                var actionContext = implementationFactory.GetService<IActionContextAccessor> ()
                    .ActionContext;
                return new UrlHelper (actionContext);
            });
            services.AddTransient<IPropertyMappingService, PropertyMappingService> ();
            services.AddTransient<ITypeHelperService, TypeHelperService> ();

            services.AddHttpCacheHeaders ((expirationModelOptions) =>
                {
                    expirationModelOptions.MaxAge = 600;
                },
                (validationModelOptions) =>
                {
                    validationModelOptions.AddMustRevalidate = true;
                });
            services.AddResponseCaching ();
            services.AddMemoryCache ();
            services.Configure<IpRateLimitOptions> ((options) =>
            {
                options.GeneralRules = new System.Collections.Generic.List<RateLimitRule> ()
                {
                    new RateLimitRule ()
                    {
                        Endpoint = "*",
                        Limit = 1000,
                        Period = "5m"
                    },
                    new RateLimitRule ()
                    {
                        Endpoint = "*",
                        Limit = 10,
                        Period = "10s"
                    }
                };
            });

            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore> ();
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore> ();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure (IApplicationBuilder app, IHostingEnvironment env,
            ILoggerFactory loggerFactory, LibraryContext libraryContext)
        {
            loggerFactory.AddConsole ();
            loggerFactory.AddDebug (LogLevel.Information);

            if (env.IsDevelopment ())
            {
                app.UseDeveloperExceptionPage ();
            }
            else
            {
                app.UseExceptionHandler (appBuilder =>
                {
                    appBuilder.Run (async context =>
                    {
                        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature> ();
                        if (exceptionHandlerFeature != null)
                        {
                            var logger = loggerFactory.CreateLogger ("Global exception logger");
                            logger.LogError (500,
                                exceptionHandlerFeature.Error,
                                exceptionHandlerFeature.Error.Message);
                        }

                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync ("An unexpected fault happened. Try again later.");

                    });
                });
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
                x.CreateMap<Book, BookForUpdateDto> ();

                x.CreateMap<AuthorForCreationDto, Author> ();
                x.CreateMap<BookForCreationDto, Book> ();
                x.CreateMap<BookForUpdateDto, Book> ();
            });

            app.UseIpRateLimiting ();
            app.UseResponseCaching ();
            app.UseHttpCacheHeaders ();
            app.UseMvc ();
            app.UseSwagger ();
            app.UseSwaggerUI (c =>
            {
                c.SwaggerEndpoint ("/swagger/v1/swagger.json", "Library API");
            });
        }
    }
}
