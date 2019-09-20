using System.Collections.Generic;

namespace Scheduler.Api.Models
{
    public interface IHasValidation
    {
        void Validate(ICollection<ValidationError> errors);
    }
}