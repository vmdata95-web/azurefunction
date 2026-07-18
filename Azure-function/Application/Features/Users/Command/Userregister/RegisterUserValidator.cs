using FluentValidation;

namespace Application.Features.Users.Command.Userregister
{
    public class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
    {
        private readonly string[] blockedDomains =
        {
            "gmail.com",
            "yahoo.com",
            "hotmail.com",
            "outlook.com",
            "icloud.com"
        };

        public RegisterUserValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Name is required");


            //RuleFor(x => x.Email)
            //    .NotEmpty()
            //    .WithMessage("Email is required")
            //    .EmailAddress()
            //    .WithMessage("Invalid email format")
            //    .Must(IsBusinessEmail)
            //    .WithMessage("Personal email is not allowed. Please use your company email.");


            RuleFor(x => x.RegisterFrom)
                .NotNull()
                .WithMessage("RegisterFrom is required")
                .Must(value => value == 1 || value == 2 || value == 3)
                .WithMessage("RegisterFrom must be 1, 2, or 3 only");

            RuleFor(x => x.IpAddress)
                .MaximumLength(50)
                .When(x => !string.IsNullOrEmpty(x.IpAddress))
                .WithMessage("IpAddress cannot exceed 50 characters");
        }


        private bool IsBusinessEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            var domain = email.Split('@').Last().ToLower();

            return !blockedDomains.Contains(domain);
        }
    }
}