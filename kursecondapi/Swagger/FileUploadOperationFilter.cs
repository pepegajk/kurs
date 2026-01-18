using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;
using System.Reflection;

namespace kursecondapi.Swagger;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Проверяем, есть ли параметры с [FromForm]
        var formParameters = context.MethodInfo.GetParameters()
            .Where(p => p.GetCustomAttributes(typeof(FromFormAttribute), false).Any())
            .ToList();

        if (formParameters.Any())
        {
            // Удаляем все параметры из operation, так как они будут в RequestBody
            operation.Parameters?.Clear();

            // Получаем тип DTO
            var dtoParameter = formParameters.FirstOrDefault();
            if (dtoParameter != null)
            {
                var dtoType = dtoParameter.ParameterType;
                var properties = new Dictionary<string, OpenApiSchema>();
                var required = new HashSet<string>();

                // Обрабатываем свойства DTO
                foreach (var prop in dtoType.GetProperties())
                {
                    OpenApiSchema schema;
                    
                    if (prop.PropertyType == typeof(IFormFile))
                    {
                        schema = new OpenApiSchema
                        {
                            Type = "string",
                            Format = "binary"
                        };
                    }
                    else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
                    {
                        schema = new OpenApiSchema
                        {
                            Type = "integer",
                            Format = "int32"
                        };
                    }
                    else
                    {
                        schema = new OpenApiSchema
                        {
                            Type = "string"
                        };
                    }

                    properties[prop.Name] = schema;
                    
                    // Проверяем, является ли свойство обязательным
                    var isNullable = Nullable.GetUnderlyingType(prop.PropertyType) != null;
                    if (!isNullable && prop.PropertyType != typeof(string) && prop.PropertyType != typeof(IFormFile))
                    {
                        required.Add(prop.Name);
                    }
                }

                operation.RequestBody = new OpenApiRequestBody
                {
                    Required = true,
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = properties,
                                Required = required
                            }
                        }
                    }
                };

                // Важно: НЕ удаляем Security Requirements - они должны остаться для авторизации
                // Security requirements уже установлены глобально в Program.cs
            }
        }
    }
}

