using FluentAssertions;
using NSubstitute;
using Project.Modules.Notifications.Application.Abstractions.Emails;
using Project.Modules.Notifications.Application.Emails.SendWelcomeEmail;
using Project.Modules.Notifications.Application.Emails.Templates.Welcome;
using Xunit;

namespace Project.Modules.Notifications.Application.Tests.Emails;

public class SendWelcomeEmailCommandHandlerTests
{
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly SendWelcomeEmailCommandHandler _handler;

    public SendWelcomeEmailCommandHandlerTests()
    {
        _handler = new SendWelcomeEmailCommandHandler(_emailService);
    }

    [Fact]
    public async Task Handle_Should_Succeed_WhenEmailSent()
    {
        _emailService.SendTemplateAsync(Arg.Any<WelcomeEmailTemplate>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(
            new SendWelcomeEmailCommand("a@b.com", "John", "Doe"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _emailService.Received(1).SendTemplateAsync(Arg.Any<WelcomeEmailTemplate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_WhenEmailNotSent()
    {
        _emailService.SendTemplateAsync(Arg.Any<WelcomeEmailTemplate>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(
            new SendWelcomeEmailCommand("a@b.com", "John", "Doe"), CancellationToken.None);

        result.IsFailed.Should().BeTrue();
    }
}
