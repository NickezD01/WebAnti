using AntiPhisher.Application.Request.UserAccount;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Validation
{
    public class RegisterValidator : AbstractValidator<UserRegisterRequest>
    {
        public RegisterValidator()
        {
            RuleFor(user => user.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email is required.");
            RuleFor(user => user.Password)
                 .NotEmpty().WithMessage("Password is required.")
                 .MinimumLength(7).WithMessage("Password must be more than 6 characters.");
            RuleFor(user => user.FullName)
                .NotEmpty().WithMessage("FullName is required.");
            RuleFor(user => user.PhoneNumber)
                .NotEmpty().WithMessage("PhoneNumber is required.");
            RuleFor(user => user.IsAgreedToTerms)
                .Equal(true).WithMessage("You must agree to the terms.");
        }
    }
}
