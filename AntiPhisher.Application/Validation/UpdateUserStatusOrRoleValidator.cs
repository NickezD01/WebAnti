using AntiPhisher.Application.Request.User;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Validation
{
    public class UpdateUserStatusOrRoleValidator : AbstractValidator<UpdateUserStatusOrRoleRequest>
    {
        public UpdateUserStatusOrRoleValidator()
        {
            // Kiểm tra UserId bắt buộc phải lớn hơn 0
            RuleFor(x => x.UserId)
                 .NotEmpty().WithMessage("UserId is required.")
                 .GreaterThan(0).WithMessage("UserId must be greater than 0.");

            // 2. 🔒 VÁ LỖ HỔNG BẢO MẬT: Chỉ chấp nhận RoleId thuộc tập hợp [1, 2, 3]
            RuleFor(x => x.RoleId)
                .NotEmpty().WithMessage("RoleId is required.")
                .Must(roleId => roleId == 1 || roleId == 2 || roleId == 3)
                .WithMessage("RoleId is invalid. Only allowed values are: 1 (Admin), 2 (Manager), 3 (User).");

            // 3. Chỉ chấp nhận Status thuộc tập hợp hợp lệ
            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status is required.")
                .Must(s => s == "Active" || s == "Banned" || s == "Unverified")
                .WithMessage("Status is invalid. Only allowed values are: Active, Banned, Unverified.");
        }
    }
}
