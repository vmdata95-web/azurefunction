using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using MediatR;

namespace Application.Features.Users.Command.Userregister
{
    public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, Guid>
    {
        private readonly IUserRepository _repository;
        private readonly IEventRepository _eventRepository;
        private readonly IAzureQueueService _azureQueueService;
        private readonly IEventRepository _get_event;

        public RegisterUserHandler(
            IEventRepository eventRepository,
            IUserRepository repository,
            IAzureQueueService azureQueueService,
            IEventRepository get_event)
        {
            _repository = repository;
            _eventRepository = eventRepository;
            _azureQueueService = azureQueueService;
            _get_event = get_event;
        }

        public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            // ✅ 1. Check user exists
            if (!request.RegisterFrom.HasValue)
                throw new BadRequestException("RegisterFrom is required");

            var registerFrom = (Registerfrom)request.RegisterFrom.Value;

            var existingUser = await _repository.GetByEmailAndRegisterFromAsync(
                request.Email,
                registerFrom);

            if (existingUser != null)
                throw new BadRequestException("Email already registered");

            // ✅ 2. Get ACTIVE event
            var activeEvent = await _get_event.GetActiveEventAsync();

            if (activeEvent == null)
                throw new BadRequestException("No active event found");

            // optional safety check (if you still want DB validation)
            var eventExists = await _eventRepository.EventExistsAsync(activeEvent.Id);
            if (!eventExists)
                throw new BadRequestException("Invalid Active Event");

            // ✅ 4. Create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Email = request.Email,
                Role = UserRole.User.ToString(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Designation = request.Designation,
                CompanyName = request.CompanyName,
                MobileNo = request.MobileNo,
                Country = request.Country,
                Number_Of_Employees = request.NumberOfEmployees,
                Registerfrom = request.RegisterFrom,
                IpAddress = request.IpAddress
            };

            await _repository.AddAsync(user);

            // ✅ 5. Prevent duplicate registration (same user + active event)
            var alreadyRegistered = await _repository.IsUserAlreadyRegistered(user.Id, activeEvent.Id);
            if (alreadyRegistered)
                throw new BadRequestException("User already registered for active event");

            // ✅ 6. Create user_event mapping
            var userEvent = new UserEvent
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                EventId = activeEvent.Id,
                RegisteredAt = DateTime.UtcNow,
                IsCheckedIn = false
            };

            await _eventRepository.AddUserEventAsync(userEvent);

            // ✅ 7. Queue welcome email via Azure Storage Queue
            var welcomeEmailBody = BuildWelcomeEmailBody(user.Name);

            await _azureQueueService.EnqueueAsync(new EmailQueueMessage
            {
                UserId = user.Id,
                Email = user.Email,
                Subject = "You're Registered for Salesforce Asia AI Summit 2026!",
                Body = welcomeEmailBody,
                IsHtml = true,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            // ✅ 8. Save all changes
            await _repository.SaveChangesAsync();

            return user.Id;
        }


        private static string BuildWelcomeEmailBody(string customerName)
        {
            return $@"<!DOCTYPE html>
<html lang=""en"">

<head>
     <meta charset=""UTF-8"">
     <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
     <title>Thank You for Registering - Salesforce Asia AI Summit 2026</title>
</head>

<body
     style=""margin:0; padding:0; background-color:#f1f6fb; font-family:'Helvetica Neue', Helvetica, Arial, sans-serif;"">

     <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f1f6fb;"">
          <tr>
               <td align=""center"" style=""padding:24px 12px;"">


                    <table role=""presentation"" width=""600"" cellpadding=""0"" cellspacing=""0""
                         style=""max-width:600px; width:100%; background-color:#ffffff; border-radius:12px; overflow:hidden; box-shadow:0 2px 12px rgba(0,0,0,0.06);"">

                         <!-- Banner -->
                         <tr>
                              <td style=""padding:0; line-height:0;"">
                                   <img src=""https://vpcpublicstorage.blob.core.windows.net/vpcdoc/Email-image/2218_Email-Banner-Thank-You.png""
                                        alt=""Salesforce Asia AI Summit 2026"" width=""600""
                                        style=""display:block; width:100%; height:auto; border:0;"">
                              </td>
                         </tr>

                         <!-- Greeting -->
                         <tr>
                              <td style=""padding:32px 40px 0 40px;"">                                  

                                   <p style=""margin:0 0 16px; font-size:16px; line-height:1.6; color:#444444;"">
                                        Thank you for registering for the
                                        <strong>Salesforce Asia AI Summit 2026</strong>.
                                        Your registration has been successfully confirmed.
                                   </p>

                                   <p style=""margin:0 0 16px; font-size:16px; line-height:1.6; color:#444444;"">
                                        We're excited to have you join us for an inspiring experience featuring AI
                                        thought
                                        leaders, customer success stories, and the latest innovations from Salesforce.
                                   </p>

                                   <p style=""margin:0 0 16px; font-size:16px; line-height:1.6; color:#444444;"">
                                        You will receive the event joining link and further details on your registered
                                        email
                                        address closer to the event date.
                                   </p>
                                   <p style=""margin:0 0 28px 0; font-size:16px; line-height:1.6; color:#2b3a4a;"">We look
                                        forward to seeing you at the summit!</p>
                              </td>
                         </tr>

                         <!-- Best Regards -->
                         <tr>
                              <td style=""padding:20px 40px 30px 40px;"">

                                   <p style=""margin:0; font-size:16px; line-height:1.5; color:#16325c;"">
                                        Best Regards,<br>
                                        <strong>Salesforce Asia AI Summit Team</strong>
                                   </p>
                              </td>
                         </tr>

                         <!-- Footer -->
                         <tr>
                              <td style=""background-color:#f8fafc; padding:20px 40px; border-top:1px solid #e8edf3;"">
                                   <p
                                        style=""margin:0; font-size:12px; color:#999999; text-align:center; line-height:1.6;"">
                                       You received this email because you registered at
                                        salesforceasiasummit.com.
                                        <br>                                        
                                        &copy; 2026 Salesforce Asia AI Summit &mdash; Value Prospect. All rights
                                        reserved.
                                   </p>
                              </td>
                         </tr>

                    </table>

               </td>
          </tr>
     </table>

</body>

</html>";
        }
    }
}