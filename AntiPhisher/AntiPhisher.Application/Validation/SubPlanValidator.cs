using AntiPhisher.Application.Request.SubscriptionPlan;
using FluentValidation;

namespace AntiPhisher.Application.Validation
{
    public class SubPlanValidator : AbstractValidator<CreateSubscriptionPlanRequest>
    {
        public SubPlanValidator()
        {
            // CHANGED: Name từ enum/int → string không được rỗng
            RuleFor(p => p.Name)
                .NotEmpty().WithMessage("Tên gói không được để trống.")
                .MaximumLength(200).WithMessage("Tên gói tối đa 200 ký tự.");

            RuleFor(p => p.MaxSlots)
                .GreaterThan(0).WithMessage("MaxSlots phải lớn hơn 0.");
        }
    }
}
