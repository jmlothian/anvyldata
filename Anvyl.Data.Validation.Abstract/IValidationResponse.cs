using System;
using System.Collections.Generic;
using System.Text;

namespace Anvyl.Data.Validation.Abstract
{
    public interface IValidationResponse
    {
        bool Passed { get; set; }
        string ErrorMessage { get; set; }
        string ErrorCode { get; set; }
        string FQN { get; set; }
        List<IValidationResponse> ChildResponses { get; set; }
        string Path { get; set; }
    }
}
