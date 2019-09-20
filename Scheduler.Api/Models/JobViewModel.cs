using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Scheduler.Api.Helpers;

namespace Scheduler.Api.Models
{
    public class JobViewModel : IHasValidation
    {
        [Required] public string JobName { get; set; }

        [Required] public string ProjectName { get; set; }

        [Required] public string Type { get; set; }

        public string Description { get; set; }

        public bool CreateTrigger { get; set; }

        public IDictionary<string, object> JobDataMap { get; set; }

        public void Validate(ICollection<ValidationError> errors) =>
            ModelValidator.ValidateObject(this, errors, nameof(JobViewModel));
    }
}
