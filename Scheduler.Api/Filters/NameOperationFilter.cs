using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Scheduler.Api.Filters
{
    [ExcludeFromCodeCoverage]
    public class NameOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            operation.OperationId = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
                .Union(context.MethodInfo.GetCustomAttributes(true))
                .OfType<SwaggerOperationAttribute>()
                .Select(a => a.OperationId)
                .FirstOrDefault();
        }
    }

    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SwaggerOperationAttribute : Attribute
    {
        public SwaggerOperationAttribute(string operationId)
        {
            OperationId = operationId;
        }

        public string OperationId { get; }
    }

}
