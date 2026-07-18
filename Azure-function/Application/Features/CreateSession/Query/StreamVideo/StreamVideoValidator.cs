using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.CreateSession.Query.StreamVideo
{
    public class StreamVideoValidator : AbstractValidator<StreamVideoQuery>
    {
        public StreamVideoValidator()
        {
            RuleFor(x => x.FileName)
                .NotEmpty().WithMessage("Video FileName is required")
                .Must(p => !p.Contains("..")).WithMessage("Invalid FileName");

            RuleFor(x => x.FileName)
    .Must(fileName =>
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var ext = Path.GetExtension(fileName).ToLower();

        return ext == ".mp4"
            || ext == ".avi"
            || ext == ".mov"
            || ext == ".webm";
    })
    .WithMessage("Only video files allowed"); 
        }
    }
}
