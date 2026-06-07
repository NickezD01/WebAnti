using AntiPhisher.Application.Request.Team;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Validation
{
    public class CreateTeamRequestValidator : AbstractValidator<CreateTeamRequest>
    {
        public CreateTeamRequestValidator()
        {
            RuleFor(x => x.TeamName)
                .NotEmpty().WithMessage("Tên nhóm không được để trống.")
                .MaximumLength(100).WithMessage("Tên nhóm quá dài.");
        }
    }
}
