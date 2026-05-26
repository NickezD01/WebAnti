using AntiPhisher.Application.Request.SubscriptionPlan;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Validation
{
    public class SubPlanValidator : AbstractValidator<CreateSubscriptionPlanRequest>
    {
        public SubPlanValidator()
        {
            RuleFor(p => (int)p.Name)
        .InclusiveBetween(0, 2).WithMessage("Role must be between 0 and 2.");
        }
    }
}
