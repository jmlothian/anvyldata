using System;
using System.Collections.Generic;
using System.Text;

namespace Anvyl.Data.Validation.Abstract
{
    public interface IValidationResponse
    {
        bool Passed { get; }
        string ErrorMessage { get; }
        string ErrorCode { get; }
        string FQN { get; }
    }
}
